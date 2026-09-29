using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Saule;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.JsonApiMediaTypeFormatter</c>'s
    /// write/serialize half (its read/deserialize half is <see cref="JsonApiInputFormatter"/>).
    /// ASP.NET Core selects formatters by their declared supported media types
    /// automatically during content negotiation, so there is no per-request formatter-selection
    /// step to replicate here (Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    internal sealed class JsonApiOutputFormatter : TextOutputFormatter
    {
        private readonly JsonApiConfiguration _config;

        internal JsonApiOutputFormatter(JsonApiConfiguration config)
        {
            _config = config;
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(Constants.MediaType));
            SupportedEncodings.Add(Encoding.UTF8);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// PR review finding: when registered via FormatterPriority.AddFormatterToStart (the
        /// default), this formatter becomes MvcOptions.OutputFormatters[0]. ASP.NET Core's own
        /// content negotiation falls back to the first formatter whose base TextOutputFormatter
        /// logic returns true when a request carries no Accept header at all (a very common real
        /// case - many clients/tools send none), which the base implementation does unconditionally
        /// once SupportedMediaTypes is non-empty, regardless of what those media types actually are.
        /// Without this override, EVERY response in an app that calls ConfigureJsonApi() - including
        /// endpoints with no [ReturnsResourceAttribute] at all - gets hijacked into JSON:API
        /// processing the moment a client omits its Accept header, independent of the
        /// requiresMediaType gate already added to JsonApiResultFilter (that gate only stops the
        /// *filter* from stashing a PreprocessResult; it doesn't stop *this formatter* from still
        /// being selected and running its own fallback preprocessing). Only claim this result when
        /// a filter already decided to (PreprocessResult stashed) or the client's Accept header
        /// genuinely asks for application/vnd.api+json.
        /// </remarks>
        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context.HttpContext.Items.ContainsKey(Constants.PropertyNames.PreprocessResult))
            {
                return base.CanWriteResult(context);
            }

            // Same signal as JsonApiResultFilter.ShouldProcessAsJsonApi (not Accept-header-only -
            // that would wrongly reject a real [ReturnsResourceAttribute] action called with no
            // explicit Accept header, a common real case).
            var shimRequest = context.HttpContext.GetOrCreateShimRequestMessage();
            if (!JsonApiResultFilter.ShouldProcessAsJsonApi(context.HttpContext, shimRequest))
            {
                return false;
            }

            return base.CanWriteResult(context);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// PR review finding: TextOutputFormatter's base WriteResponseHeaders appends a "; charset="
        /// parameter to the Content-Type header once SupportedEncodings is non-empty - JSON:API's own
        /// spec forbids media type parameters on application/vnd.api+json, which is exactly why
        /// ReturnsResourceAttribute rejects a request Accept/Content-Type that carries one (406/415).
        /// Shipping a response that violates the same rule Saule enforces on requests is a real,
        /// ironic non-compliance bug - fixed by setting the header directly instead of deferring to
        /// the base charset-appending logic.
        /// </remarks>
        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            context.HttpContext.Response.ContentType = Constants.MediaType;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            PreprocessResult preprocessed;
            if (context.HttpContext.Items.TryGetValue(Constants.PropertyNames.PreprocessResult, out var stashed)
                && stashed is PreprocessResult stashedResult)
            {
                preprocessed = stashedResult;
            }
            else
            {
                // Backwards compatibility with setups that didn't go through JsonApiResultFilter -
                // same fallback net47's JsonApiMediaTypeFormatter.WriteToStreamAsync has.
                var request = context.HttpContext.GetOrCreateShimRequestMessage();
                preprocessed = JsonApiRequestPreprocessor.PreprocessRequest(context.Object, request, _config);
            }

            var json = JsonApiSerializer.Serialize(preprocessed);
            var response = context.HttpContext.Response;
            await response.WriteAsync(json.ToString(Formatting.None, _config.JsonConverters.ToArray()), selectedEncoding);
        }
    }
}
