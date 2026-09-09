using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Saule.Queries.Fieldset;
using Saule.Queries.Including;
using Saule.Queries.Sorting;
using FilterContext = Saule.Queries.Filtering.FilterContext;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.AllowsQueryAttribute</c> - same
    /// name/namespace, same behavior, built against <see cref="ActionFilterAttribute"/> from
    /// <c>Microsoft.AspNetCore.Mvc.Filters</c> instead of <c>System.Web.Http.Filters</c>
    /// (Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class AllowsQueryAttribute : ActionFilterAttribute
    {
        /// <inheritdoc/>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var queryParams = context.HttpContext.Request.Query.ToNameValuePairs().ToList();
            var queryContext = QueryContextUtils.GetQueryContext(context);

            queryContext.Sort = new SortContext(queryParams);
            queryContext.Filter = new FilterContext(queryParams);
            queryContext.Fieldset = new FieldsetContext(queryParams);

            if (queryContext.Include == null)
            {
                queryContext.Include = new IncludeContext(queryParams);
            }
            else
            {
                queryContext.Include.SetIncludes(queryParams);
            }

            base.OnActionExecuting(context);
        }
    }
}
