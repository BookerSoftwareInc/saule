using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.JsonApiQueryValueProviderFactory</c>
    /// (Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    public class JsonApiQueryValueProviderFactory : IValueProviderFactory
    {
        /// <inheritdoc/>
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            context.ValueProviders.Add(new JsonApiQueryValueProvider(context.ActionContext.HttpContext.Request.Query, CultureInfo.InvariantCulture));
            return Task.CompletedTask;
        }
    }
}
