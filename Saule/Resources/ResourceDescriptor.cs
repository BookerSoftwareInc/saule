using System.Net.Http;

namespace Saule.Resources
{
    /// <summary>
    /// Attaches/reads the <see cref="ApiResource"/> descriptor associated with a request. Portable so that both
    /// the net47 (<c>System.Web.Http</c>) and net10.0 (ASP.NET Core) <c>ReturnsResourceAttribute</c> implementations,
    /// and the portable <see cref="JsonApiSerializer{T}"/>, share exactly one implementation.
    /// </summary>
    internal static class ResourceDescriptor
    {
        /// <summary>
        /// Attaches the resource descriptor to the request so downstream pipeline stages can resolve it.
        /// </summary>
        /// <param name="request">The current request.</param>
        /// <param name="resource">The resource type for this request.</param>
        internal static void AttachToRequest(HttpRequestMessage request, ApiResource resource)
        {
            request.Properties.Add(Constants.PropertyNames.ResourceDescriptor, resource);
        }
    }
}
