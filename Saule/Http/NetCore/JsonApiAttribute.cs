using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.JsonApiAttribute</c> - an
    /// optional attribute that opts one action into a JSON:API response using its own static
    /// <see cref="JsonApiConfiguration"/>, independent of whether <c>ConfigureJsonApi</c> was ever
    /// called (Standard-2.0-Migration-Plan.md Section 7.1).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class JsonApiAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Gets or sets JsonApiConfiguration parameters for Json Api serialization.
        /// </summary>
        public static JsonApiConfiguration JsonApiConfiguration { get; set; } = new JsonApiConfiguration();

        /// <inheritdoc/>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                throw context.Exception;
            }

            if (context.Result is ObjectResult objectResult)
            {
                JsonApiResultFilter.ProcessResult(context.HttpContext, objectResult, JsonApiConfiguration);
            }

            base.OnActionExecuted(context);
        }
    }
}
