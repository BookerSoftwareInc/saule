using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Saule;
using Saule.Resources;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.ReturnsResourceAttribute</c> -
    /// same name/namespace, same media-type-parameter validation, same
    /// <c>ResourceDescriptor.AttachToRequest</c> call as net47 (via the shimmed
    /// <see cref="System.Net.Http.HttpRequestMessage"/> - Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class ReturnsResourceAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReturnsResourceAttribute"/> class.
        /// </summary>
        /// <param name="resourceType">The type of the resource this controller action returns.</param>
        public ReturnsResourceAttribute(Type resourceType)
        {
            if (!resourceType.IsSubclassOf(typeof(ApiResource)))
            {
                throw new ArgumentException("Resource types must inherit from Saule.ApiResource");
            }

            Resource = resourceType.CreateInstance<ApiResource>();
        }

        /// <summary>
        /// Gets the type of the resource this controller action returns.
        /// </summary>
        public ApiResource Resource { get; }

        /// <inheritdoc/>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;

            // request.Headers.Accept holds raw, potentially comma-separated header strings - a
            // single line can carry multiple media types, which a naive per-value TryParse would
            // miss. GetTypedHeaders().Accept is the framework's own already-split/parsed list.
            var accept = (request.GetTypedHeaders().Accept ?? Enumerable.Empty<MediaTypeHeaderValue>())
                .Where(a => a.MediaType == Constants.MediaType)
                .ToList();
            if (accept.Count > 0 && accept.All(a => a.Parameters.Any()))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status406NotAcceptable);
                return;
            }

            if (MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) && contentType.Parameters.Any())
            {
                context.Result = new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);
                return;
            }

            ResourceDescriptor.AttachToRequest(context.HttpContext.GetOrCreateShimRequestMessage(), Resource);
            base.OnActionExecuting(context);
        }
    }
}
