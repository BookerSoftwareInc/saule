using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Saule;
using Saule.Queries;
using Saule.Queries.Including;
using Saule.Queries.Sorting;
using FilterContext = Saule.Queries.Filtering.FilterContext;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.HandlesQueryAttribute</c> - same
    /// intent (parse sort/filter/include but let the action handle them manually), same
    /// action-parameter injection, <see cref="ProblemDetails"/> instead of
    /// <c>System.Web.Http.HttpError</c> for the "shouldn't be combined with" error
    /// (Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    public class HandlesQueryAttribute : ActionFilterAttribute
    {
        /// <inheritdoc/>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (EnsureAttributeNotSpecified<DisableDefaultIncludedAttribute>(context)
                || EnsureAttributeNotSpecified<AllowsQueryAttribute>(context))
            {
                return;
            }

            var queryParams = context.HttpContext.Request.Query.ToNameValuePairs().ToList();
            var queryContext = QueryContextUtils.GetQueryContext(context);

            queryContext.IsHandledQuery = true;
            queryContext.Sort = new SortContext(queryParams);
            queryContext.Filter = new FilterContext(queryParams);

            if (queryContext.Include == null)
            {
                queryContext.Include = new IncludeContext(queryParams);
            }
            else
            {
                queryContext.Include.SetIncludes(queryParams);
            }

            // we validate if the action has a QueryContext parameter and, if so, pass it
            foreach (var parameter in context.ActionDescriptor.Parameters)
            {
                if (parameter.ParameterType == typeof(QueryContext))
                {
                    context.ActionArguments[parameter.Name] = queryContext;
                }
            }

            base.OnActionExecuting(context);
        }

        private bool EnsureAttributeNotSpecified<T>(ActionExecutingContext context)
            where T : class
        {
            // PR review finding: context.ActionDescriptor.FilterDescriptors includes controller-level
            // (class-attribute) filters merged in with action-level ones - net47's
            // HttpActionDescriptor.GetCustomAttributes<T>() only ever inspected the action/method
            // itself, so a controller-level [DisableDefaultIncluded] combined with an action-level
            // [HandlesQuery] was allowed there but would incorrectly throw here. Reflecting on the
            // action's MethodInfo directly restricts this to the same action-level-only scope.
            var isSpecifiedOnAction = (context.ActionDescriptor as ControllerActionDescriptor)?
                .MethodInfo.GetCustomAttributes(typeof(T), inherit: true).Any() ?? false;

            if (isSpecifiedOnAction)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Title = new JsonApiException(ErrorType.Server, $"{typeof(T).Name} shouldn't be used with {nameof(HandlesQueryAttribute)}").Message,
                    Status = 500,
                })
                {
                    StatusCode = 500,
                };
                return true;
            }

            return false;
        }
    }
}
