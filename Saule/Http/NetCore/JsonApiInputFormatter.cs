using System.IO;
using System.Threading.Tasks;
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
            using (var reader = new StreamReader(context.HttpContext.Request.Body, encoding))
            {
                try
                {
                    var json = JToken.Parse(await reader.ReadToEndAsync());
                    var result = new ResourceDeserializer(json, context.ModelType, _config.PropertyNameConverter).Deserialize();
                    return await InputFormatterResult.SuccessAsync(result);
                }
                catch (JsonApiException ex)
                {
                    context.ModelState.TryAddModelError(context.ModelName, ex.Message);
                    return await InputFormatterResult.FailureAsync();
                }
                catch (JsonReaderException)
                {
                    context.ModelState.TryAddModelError(context.ModelName, "Request content is not valid JSON.");
                    return await InputFormatterResult.FailureAsync();
                }
            }
        }
    }
}
