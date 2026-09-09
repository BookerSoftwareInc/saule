using System.Collections.Generic;
using System.Net.Http;
using Saule.Queries;
using Saule.Resources;
using Saule.Serialization;

namespace Saule.Http
{
    /// <summary>
    /// The actual filter/sort/pagination/resource-resolution preprocessing logic, shared verbatim by
    /// the net47 <c>PreprocessingDelegatingHandler</c> and the net10.0 <c>JsonApiResultFilter</c>
    /// (Standard-2.0-Migration-Plan.md Section 7.1) - extracted because this logic itself has no
    /// <c>System.Web.Http</c> dependency at all (only <see cref="HttpRequestMessage"/>, which is
    /// portable), even though it used to live inside the net47-only
    /// <c>PreprocessingDelegatingHandler.cs</c>.
    /// </summary>
    internal static class JsonApiRequestPreprocessor
    {
        internal static PreprocessResult PreprocessRequest(
            object content,
            HttpRequestMessage request,
            JsonApiConfiguration config)
        {
            var jsonApi = new JsonApiSerializer();
            jsonApi.JsonConverters.AddRange(config.JsonConverters);

            PrepareQueryContext(jsonApi, request, config);

            ApiResource resource = null;
            bool isKnownError = IsKnownFrameworkError(content);
            IApiResourceProvider resourceProvider = null;

            if (!isKnownError)
            {
                resourceProvider = config.ApiResourceProviderFactory.Create(request);
                if (resourceProvider == null)
                {
                    content = new JsonApiException(
                        ErrorType.Server,
                        "ApiResourceProviderFactory returned null but it should always return an instance of IApiResourceProvider.")
                    {
                        HelpLink = "https://github.com/joukevandermaas/saule/wiki"
                    };
                    isKnownError = true;
                }
                else
                {
                    resource = resourceProvider.Resolve(content);
                }
            }

            if (resource == null && content != null && !isKnownError)
            {
                content = new JsonApiException(
                    ErrorType.Server,
                    "You must add a [ReturnsResourceAttribute] to action methods.")
                {
                    HelpLink = "https://github.com/joukevandermaas/saule/wiki"
                };
            }

            if (!isKnownError && jsonApi.QueryContext?.Pagination?.PerPage > jsonApi.QueryContext?.Pagination?.PageSizeLimit)
            {
                content = new JsonApiException(ErrorType.Client, "Page size exceeds page size limit for queries.");
            }

            PrepareUrlPathBuilder(jsonApi, request, config);

            return jsonApi.PreprocessContent(content, request.RequestUri, config, resourceProvider);
        }

        private static bool IsKnownFrameworkError(object content)
        {
#if NETFRAMEWORK
            return content is System.Web.Http.HttpError || content is IEnumerable<System.Web.Http.HttpError>;
#elif NET10_0
            return content is Microsoft.AspNetCore.Mvc.ProblemDetails || content is IEnumerable<Microsoft.AspNetCore.Mvc.ProblemDetails>;
#else
            return false;
#endif
        }

        private static void PrepareUrlPathBuilder(
            JsonApiSerializer jsonApiSerializer,
            HttpRequestMessage request,
            JsonApiConfiguration config)
        {
            if (config.UrlPathBuilder != null)
            {
                jsonApiSerializer.UrlPathBuilder = config.UrlPathBuilder;
            }
#if NETFRAMEWORK
            else if (!request.Properties.ContainsKey(Constants.PropertyNames.WebApiRequestContext))
            {
                jsonApiSerializer.UrlPathBuilder = new DefaultUrlPathBuilder();
            }
            else
            {
                var requestContext = request.Properties[Constants.PropertyNames.WebApiRequestContext]
                    as System.Web.Http.Controllers.HttpRequestContext;
                var routeTemplate = requestContext?.RouteData.Route.RouteTemplate;
                var virtualPathRoot = requestContext?.VirtualPathRoot ?? "/";

                jsonApiSerializer.UrlPathBuilder = new DefaultUrlPathBuilder(
                    virtualPathRoot, routeTemplate);
            }
#else
            else
            {
                jsonApiSerializer.UrlPathBuilder = new DefaultUrlPathBuilder();
            }
#endif
        }

        private static void PrepareQueryContext(
            JsonApiSerializer jsonApiSerializer,
            HttpRequestMessage request,
            JsonApiConfiguration config)
        {
            if (!request.Properties.ContainsKey(Constants.PropertyNames.QueryContext))
            {
                return;
            }

            var queryContext = (QueryContext)request.Properties[Constants.PropertyNames.QueryContext];

            if (queryContext.Filter != null)
            {
                queryContext.Filter.QueryFilters = config.QueryFilterExpressions;
            }

            jsonApiSerializer.QueryContext = queryContext;
        }
    }
}
