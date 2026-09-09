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

        /// <summary>
        /// The deployment's virtual path root (<see cref="HttpRequest.PathBase"/>) and the matched
        /// endpoint's route template - net47's <c>DefaultUrlPathBuilder(virtualPathRoot, template)</c>
        /// needs both to build correct canonical links under a real deployment (e.g. PathBase=/crm).
        /// Stashed here, not read directly from HttpContext at the point they're needed, because
        /// JsonApiRequestPreprocessor.PrepareUrlPathBuilder only has the portable
        /// <see cref="HttpRequestMessage"/> shim to work with, by design (Standard-2.0-Migration-Plan.md
        /// Section 7.1) - it must stay HttpContext-agnostic to remain shared with net47.
        /// </summary>
        internal const string PathBasePropertyKey = "Saule_NetCore_PathBase";
        internal const string RouteTemplatePropertyKey = "Saule_NetCore_RouteTemplate";

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
                foreach (var raw in acceptValues)
                {
                    // A single Accept header line is often comma-separated (e.g.
                    // "application/vnd.api+json, text/html") - TryParse only understands one media
                    // type at a time, so the whole line must be split before parsing each entry, or
                    // it silently fails to parse and the value is dropped.
                    foreach (var value in raw.Split(','))
                    {
                        if (MediaTypeWithQualityHeaderValue.TryParse(value.Trim(), out var accept))
                        {
                            message.Headers.Accept.Add(accept);
                        }
                    }
                }
            }

            message.Properties[PathBasePropertyKey] = request.PathBase.Value;
            message.Properties[RouteTemplatePropertyKey] =
                (context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern?.RawText;

            context.Items[ShimItemsKey] = message;
            return message;
        }
    }
}
