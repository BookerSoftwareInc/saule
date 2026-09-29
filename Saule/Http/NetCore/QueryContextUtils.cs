using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Saule.Queries;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.QueryContextUtils</c> - same
    /// get-or-create-per-request semantics, <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/>
    /// instead of <c>HttpRequestMessage.Properties</c> (Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    internal static class QueryContextUtils
    {
        internal static QueryContext GetQueryContext(ActionContext actionContext)
        {
            // PR review finding: JsonApiRequestPreprocessor.PrepareQueryContext (shared/portable code)
            // reads the QueryContext exclusively from the shim HttpRequestMessage.Properties, not from
            // HttpContext.Items - without also mirroring it there, [AllowsQuery]/[HandlesQuery]/
            // [Paginated]/[DisableDefaultIncluded]'s state never reaches serialization on net10.0.
            var request = actionContext.HttpContext.GetOrCreateShimRequestMessage();
            var items = actionContext.HttpContext.Items;
            if (items.TryGetValue(Constants.PropertyNames.QueryContext, out var existing) && existing is QueryContext query)
            {
                request.Properties[Constants.PropertyNames.QueryContext] = query;
                return query;
            }

            var newQuery = new QueryContext();
            items[Constants.PropertyNames.QueryContext] = newQuery;
            request.Properties[Constants.PropertyNames.QueryContext] = newQuery;
            return newQuery;
        }
    }
}
