using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Saule.Http
{
    /// <summary>
    /// Processes JSON API responses to enable filtering, pagination and sorting.
    /// </summary>
    public class PreprocessingDelegatingHandler : DelegatingHandler
    {
        private readonly JsonApiConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreprocessingDelegatingHandler"/> class.
        /// </summary>
        /// <param name="config">The configuration parameters for JSON API serialization.</param>
        public PreprocessingDelegatingHandler(JsonApiConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// The actual preprocessing logic now lives in the portable <see cref="JsonApiRequestPreprocessor"/>
        /// (Standard-2.0-Migration-Plan.md Section 7.1) - shared verbatim with the net10.0
        /// <c>JsonApiResultFilter</c> rather than duplicated.
        /// </summary>
        internal static PreprocessResult PreprocessRequest(
            object content,
            HttpRequestMessage request,
            JsonApiConfiguration config)
        {
            return JsonApiRequestPreprocessor.PreprocessRequest(content, request, config);
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var result = await base.SendAsync(request, cancellationToken);

            JsonApiProcessor.ProcessRequest(request, result, _config, requiresMediaType: true);

            return result;
        }
    }
}
