using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Saule.Queries;

namespace Saule.Http
{
    /// <summary>
    /// PR review finding (#13): <see cref="HandlesQueryAttribute"/> injects a <see cref="QueryContext"/>
    /// into action arguments during <c>OnActionExecuting</c> (an order-0 action filter), but
    /// <c>QueryContext</c> is a plain complex type with no attribute of its own, so
    /// <c>[ApiController]</c>'s <c>InferParameterBindingInfoConvention</c> infers <c>BindingSource.Body</c>
    /// for any action parameter of that type. That makes <c>[ApiController]</c>'s
    /// <c>UnsupportedContentTypeFilter</c>/<c>ModelStateInvalidFilter</c> (both ordered before any
    /// action filter) short-circuit the request before <see cref="HandlesQueryAttribute"/> ever runs,
    /// requiring a request body for what should be a query-driven GET. This convention gives every
    /// <c>QueryContext</c> parameter an explicit non-Body binding source instead, paired with
    /// <see cref="QueryContextModelBinder"/> to make binding itself a no-op.
    /// </summary>
    internal sealed class QueryContextParameterConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    foreach (var parameter in action.Parameters)
                    {
                        if (parameter.ParameterType != typeof(QueryContext))
                        {
                            continue;
                        }

                        parameter.BindingInfo = new BindingInfo
                        {
                            BindingSource = BindingSource.Custom,
                            BinderType = typeof(QueryContextModelBinder),
                        };
                    }
                }
            }
        }
    }
}
