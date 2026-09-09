using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Saule.Serialization;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.PreprocessingDelegatingHandler</c>.
    /// <para>
    /// Not literal ASP.NET Core middleware, deliberately - by the time an <c>app.Use(...)</c>
    /// middleware's <c>await next()</c> returns, MVC has already executed the result and written
    /// the response body (unlike net47's message-handler pipeline, where the actual
    /// <c>MediaTypeFormatter.WriteToStreamAsync</c> call happens even later, at the hosting layer,
    /// after every message handler - including <c>PreprocessingDelegatingHandler</c> - has already
    /// returned). An <see cref="IAsyncResultFilter"/> is the correct translation: it runs after the
    /// action executes but before the result (and therefore the output formatter) executes - see
    /// Standard-2.0-Migration-Plan.md Section 7.1's "correction" note.
    /// </para>
    /// </summary>
    internal sealed class JsonApiResultFilter : IAsyncResultFilter
    {
        private readonly JsonApiConfiguration _config;

        internal JsonApiResultFilter(JsonApiConfiguration config)
        {
            _config = config;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult objectResult)
            {
                // requiresMediaType: true - this filter is registered globally (MvcBuilderExtensions.cs),
                // exactly like net47's PreprocessingDelegatingHandler/JsonApiProcessor.ProcessRequest(...,
                // requiresMediaType: true). Without this gate, every ObjectResult in the whole app -
                // including endpoints that were never meant to be JSON:API - gets forced through this
                // pipeline and fails with "You must add a [ReturnsResourceAttribute]" the moment a
                // client's Accept header doesn't ask for application/vnd.api+json.
                ProcessResult(context.HttpContext, objectResult, _config, requiresMediaType: true);
            }

            await next();
        }

        /// <summary>
        /// Shared with <see cref="JsonApiAttribute"/>, which forces the same behavior for one
        /// action using its own static <see cref="JsonApiConfiguration"/> instead of the one
        /// registered globally via <c>ConfigureJsonApi</c> - same as net47's split between
        /// <c>PreprocessingDelegatingHandler</c> and <c>JsonApiAttribute</c>.
        /// </summary>
        /// <param name="httpContext">The current request's <see cref="HttpContext"/>.</param>
        /// <param name="objectResult">The action's result, to be reshaped into a JSON:API document.</param>
        /// <param name="config">The <see cref="JsonApiConfiguration"/> to preprocess/serialize with.</param>
        /// <param name="requiresMediaType">
        /// Mirrors net47's <c>JsonApiProcessor.ProcessRequest</c> parameter of the same name - true
        /// for the globally-registered filter (only process requests that actually asked for
        /// application/vnd.api+json), false for <see cref="JsonApiAttribute"/>'s explicit per-action
        /// opt-in (net47's own <c>JsonApiAttribute</c> forces the format unconditionally too).
        /// </param>
        internal static void ProcessResult(HttpContext httpContext, ObjectResult objectResult, JsonApiConfiguration config, bool requiresMediaType)
        {
            var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
            if (statusCode >= 400 && statusCode < 500)
            {
                // probably malformed request or not found - same short-circuit as net47's JsonApiProcessor
                return;
            }

            var request = httpContext.GetOrCreateShimRequestMessage();

            if (requiresMediaType && !ShouldProcessAsJsonApi(httpContext, request))
            {
                return;
            }


            var preprocessed = JsonApiRequestPreprocessor.PreprocessRequest(objectResult.Value, request, config);

            if (preprocessed.ErrorContent != null)
            {
                objectResult.StatusCode = ApiError.IsClientError(preprocessed.ErrorContent)
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status500InternalServerError;
            }

            httpContext.Items[Constants.PropertyNames.PreprocessResult] = preprocessed;

            // Force the JSON:API formatter regardless of the action's declared/negotiated content types,
            // matching net47's ConfigureJsonApi wiring (the formatter is the only one that understands
            // PreprocessResult).
            objectResult.ContentTypes.Clear();
            objectResult.ContentTypes.Add(Constants.MediaType);

            // ObjectResult.Formatters (scoped to this one result) takes priority over the globally
            // registered MvcOptions.OutputFormatters during content negotiation. Attaching the
            // formatter here - not just relying on global registration - is what makes JsonApiAttribute
            // self-sufficient on its own (net47's own JsonApiAttribute swaps in its own formatter
            // instance directly for exactly this reason, independent of whether ConfigureJsonApi was
            // ever called); it's a no-op for the globally-registered path since that formatter is
            // already present in MvcOptions.OutputFormatters.
            if (objectResult.Formatters.Count == 0)
            {
                objectResult.Formatters.Add(new JsonApiOutputFormatter(config));
            }
        }

        /// <summary>
        /// Shared by <see cref="ProcessResult"/> (via the <c>requiresMediaType</c> gate) and
        /// <see cref="JsonApiOutputFormatter.CanWriteResult"/>. An Accept-header-only check is too
        /// strict: a plain client call to a real <c>[ReturnsResourceAttribute]</c> action with no
        /// explicit <c>Accept</c> header (a very common case) would otherwise be wrongly treated as
        /// "not JSON:API". The reliable signal is whether <c>ReturnsResourceAttribute</c> actually
        /// ran for this action (it always runs before the action body, so it's already set even if
        /// the action later throws - see <see cref="JsonApiExceptionFilter"/>) - Accept-header
        /// matching is only the fallback for actions with no such attribute at all.
        /// </summary>
        internal static bool ShouldProcessAsJsonApi(HttpContext httpContext, System.Net.Http.HttpRequestMessage shimRequest)
        {
            if (shimRequest.Properties.ContainsKey(Constants.PropertyNames.ResourceDescriptor))
            {
                return true;
            }

            var accept = httpContext.Request.GetTypedHeaders().Accept;
            return accept != null && accept.Any(a => a.MediaType == Constants.MediaType);
        }
    }
}
