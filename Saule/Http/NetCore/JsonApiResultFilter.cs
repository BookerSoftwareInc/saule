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
                ProcessResult(context.HttpContext, objectResult, _config);
            }

            await next();
        }

        /// <summary>
        /// Shared with <see cref="JsonApiAttribute"/>, which forces the same behavior for one
        /// action using its own static <see cref="JsonApiConfiguration"/> instead of the one
        /// registered globally via <c>ConfigureJsonApi</c> - same as net47's split between
        /// <c>PreprocessingDelegatingHandler</c> and <c>JsonApiAttribute</c>.
        /// </summary>
        internal static void ProcessResult(HttpContext httpContext, ObjectResult objectResult, JsonApiConfiguration config)
        {
            var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
            if (statusCode >= 400 && statusCode < 500)
            {
                // probably malformed request or not found - same short-circuit as net47's JsonApiProcessor
                return;
            }

            var request = httpContext.GetOrCreateShimRequestMessage();
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
        }
    }
}
