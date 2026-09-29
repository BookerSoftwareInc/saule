using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Saule.Http
{
    /// <summary>
    /// No-op binder for <see cref="Saule.Queries.QueryContext"/> action parameters - <see cref="HandlesQueryAttribute"/>
    /// populates the real value directly into <c>ActionExecutingContext.ActionArguments</c> before the
    /// action runs, so this binder only needs to succeed without requiring a request body or any
    /// particular Content-Type (paired with <see cref="QueryContextParameterConvention"/>, which gives
    /// the parameter a non-Body binding source so <c>[ApiController]</c>'s automatic body-required
    /// checks never see it).
    /// </summary>
    internal sealed class QueryContextModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }
    }
}
