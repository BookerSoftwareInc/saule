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
