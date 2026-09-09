using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Saule.Http
{
    /// <summary>
    /// Bridges ASP.NET Core's <see cref="HttpContext"/> to the portable <see cref="HttpRequestMessage"/>-shaped
    /// APIs (<c>IApiResourceProviderFactory.Create</c>, <c>ResourceDescriptor.AttachToRequest</c>) shared with
    /// net47, per Standard-2.0-Migration-Plan.md Section 7.1/7.2 - "something must construct an
    /// HttpRequestMessage from HttpContext.Request". One shim instance is created per request and cached on
    /// <see cref="HttpContext.Items"/> so every pipeline stage (middleware, filters, formatters) shares the same
    /// instance - its <see cref="HttpRequestMessage.Properties"/> bag is the same per-request state carrier
    /// net47's message-handler pipeline uses.
    /// </summary>
    internal static class HttpContextRequestMessageExtensions
    {
        private const string ShimItemsKey = "Saule_ShimHttpRequestMessage";

        internal static HttpRequestMessage GetOrCreateShimRequestMessage(this HttpContext context)
        {
            if (context.Items.TryGetValue(ShimItemsKey, out var existing) && existing is HttpRequestMessage shim)
            {
                return shim;
            }

            var request = context.Request;
            var message = new HttpRequestMessage(new HttpMethod(request.Method), new Uri(request.GetEncodedUrl()));

            if (request.Headers.TryGetValue("Accept", out var acceptValues))
            {
                foreach (var value in acceptValues)
                {
                    if (MediaTypeWithQualityHeaderValue.TryParse(value, out var accept))
                    {
                        message.Headers.Accept.Add(accept);
                    }
                }
            }

            context.Items[ShimItemsKey] = message;
            return message;
        }
    }
}
