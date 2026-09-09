using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Saule.Serialization;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.JsonApiMediaTypeFormatter</c>'s
    /// read/deserialize half (Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    internal sealed class JsonApiInputFormatter : TextInputFormatter
    {
        private readonly JsonApiConfiguration _config;

        internal JsonApiInputFormatter(JsonApiConfiguration config)
        {
            _config = config;
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(Constants.MediaType));
            SupportedEncodings.Add(System.Text.Encoding.UTF8);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, System.Text.Encoding encoding)
        {
            // PR review finding: a raw `new StreamReader(context.HttpContext.Request.Body, encoding)`
            // disposes Request.Body when the reader is disposed - ASP.NET Core owns that stream's
            // lifecycle, and other things (logging, downstream middleware) may need it afterward.
            // context.ReaderFactory is the framework's own factory for this exact purpose (respects
            // any pooled/custom reader configuration too) and does not dispose the underlying stream.
            var reader = context.ReaderFactory(context.HttpContext.Request.Body, encoding);
            try
            {
                var json = JToken.Parse(await reader.ReadToEndAsync());
                var result = new ResourceDeserializer(json, context.ModelType, _config.PropertyNameConverter).Deserialize();
                return await InputFormatterResult.SuccessAsync(result);
            }
            catch (JsonApiException ex)
            {
                // PR review finding: ModelState + FailureAsync() only produces an automatic 400 under
                // the [ApiController] convention - a plain ControllerBase (a real migration path, not
                // just a smoke-test convenience) would silently invoke the action with a null/default
                // parameter instead, unlike net47's unconditional HttpResponseException(400). Throwing
                // BadHttpRequestException forces 400 regardless of [ApiController].
                throw new BadHttpRequestException(ex.Message);
            }
            catch (JsonReaderException)
            {
                throw new BadHttpRequestException("Request content is not valid JSON.");
            }
        }
    }
}
