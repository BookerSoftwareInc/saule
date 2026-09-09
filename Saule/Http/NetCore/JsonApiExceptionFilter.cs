using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Saule.Http
{
    /// <summary>
    /// On net47, an unhandled action-method exception is turned into an <c>HttpError</c> by classic
    /// ASP.NET Web API itself, which then flows through the same response pipeline as any other
    /// result - <c>JsonApiSerializer.GetAsError</c>'s <c>HttpError</c> branch is exactly why that
    /// produces a proper JSON:API errors document automatically. ASP.NET Core has no equivalent:
    /// an unhandled exception bypasses result filters/formatters entirely and surfaces as the
    /// developer exception page (or a bare 500) unless something converts it into a result first.
    /// This filter is that something - registered by <c>MvcBuilderExtensions.ConfigureJsonApi</c>,
    /// it turns the exception into a <see cref="ProblemDetails"/>-wrapped <see cref="ObjectResult"/>,
    /// which then flows through the normal result-filter pipeline (<see cref="JsonApiResultFilter"/>)
    /// exactly like any other result - <c>JsonApiSerializer.GetAsError</c>'s <c>NET10_0</c>
    /// <see cref="ProblemDetails"/> branch (Standard-2.0-Migration-Plan.md Section 3.2/7.1) is what
    /// then converts it into a real JSON:API errors document.
    /// </summary>
    internal sealed class JsonApiExceptionFilter : IExceptionFilter
    {
        private readonly JsonApiConfiguration _config;

        internal JsonApiExceptionFilter(JsonApiConfiguration config)
        {
            _config = config;
        }

        /// <inheritdoc/>
        public void OnException(ExceptionContext context)
        {
            var problemDetails = new ProblemDetails
            {
                Title = context.Exception.Message,
                Detail = context.Exception.ToString(),
                Status = StatusCodes.Status500InternalServerError,
            };

            var objectResult = new ObjectResult(problemDetails)
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };

            // A result substituted by an exception filter bypasses the normal result-filter pipeline
            // entirely in ASP.NET Core - JsonApiResultFilter never runs for it (confirmed: without
            // this direct call, the built-in [ApiController] problem-details formatter served this
            // response as application/problem+json instead). Process it directly here instead of
            // relying on the filter pipeline. requiresMediaType: true, same as the normal
            // globally-registered path - only convert this into a JSON:API errors document if the
            // action that threw was actually a JSON:API one ([ReturnsResourceAttribute] already ran
            // before the action body, so its descriptor is still attached even though it threw).
            JsonApiResultFilter.ProcessResult(context.HttpContext, objectResult, _config, requiresMediaType: true);

            context.Result = objectResult;
            context.ExceptionHandled = true;
        }
    }
}
