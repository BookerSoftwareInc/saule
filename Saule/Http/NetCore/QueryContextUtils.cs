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
            var items = actionContext.HttpContext.Items;
            if (items.TryGetValue(Constants.PropertyNames.QueryContext, out var existing) && existing is QueryContext query)
            {
                return query;
            }

            var newQuery = new QueryContext();
            items[Constants.PropertyNames.QueryContext] = newQuery;
            return newQuery;
        }
    }
}
