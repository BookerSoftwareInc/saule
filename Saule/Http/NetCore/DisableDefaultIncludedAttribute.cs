using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Saule.Queries.Including;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.DisableDefaultIncludedAttribute</c>
    /// (Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class DisableDefaultIncludedAttribute : ActionFilterAttribute
    {
        /// <inheritdoc/>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var queryContext = QueryContextUtils.GetQueryContext(context);

            if (queryContext.Include == null)
            {
                queryContext.Include = new IncludeContext();
            }

            queryContext.Include.DisableDefaultIncluded = true;

            base.OnActionExecuting(context);
        }
    }
}
