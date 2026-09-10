using System.Linq;
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
            // PR review finding: this filter is registered globally (MvcBuilderExtensions.cs), so it
            // used to run - and unconditionally set context.Result/ExceptionHandled - for every
            // unhandled exception in the whole app, not just JSON:API actions. Merely calling
            // ConfigureJsonApi() was silently swallowing exceptions from ordinary, non-JSON:API
            // endpoints and replacing the app's own exception handling (middleware, other filters,
            // the dev exception page) with a bare 500. Only handle it here if the action was actually
            // JSON:API - either explicitly opted in via [JsonApi]/[ReturnsResourceAttribute], or the
            // client asked for it via Accept header (ShouldProcessAsJsonApi's own fallback).
            var shimRequest = context.HttpContext.GetOrCreateShimRequestMessage();
            var isExplicitJsonApi = context.Filters.OfType<JsonApiAttribute>().Any();

            if (!isExplicitJsonApi && !JsonApiResultFilter.ShouldProcessAsJsonApi(context.HttpContext, shimRequest))
            {
                return;
            }

            // PR review finding: unconditionally exposing the exception (message, stack trace, inner
            // exception messages) to the client is an information-disclosure risk in production -
            // exception messages commonly carry SQL, paths, or other internal details, and Title was
            // still leaking the raw message even after Detail was gated. On net47 this was never an
            // issue because Web API's own IncludeErrorDetailPolicy (local-only by default) stripped
            // HttpError's detail before it ever reached Saule, falling back to a generic message
            // (classic Web API's "An error has occurred."); net10.0 has no equivalent built-in gate,
            // so this filter applies its own policy instead - JsonApiConfiguration.
            // IncludeExceptionDetailInErrors, defaulting to false, gates both Title and Detail.
            // PR review finding: BadHttpRequestException (e.g. JsonApiInputFormatter's malformed-body
            // 400) already carries its own client-error StatusCode - forcing every exception to 500
            // here discarded that and turned a client error into a server error. Its Message is
            // always a client-safe, purpose-written string (never raw exception detail), so it is
            // shown regardless of IncludeExceptionDetailInErrors, same as net47's
            // HttpResponseException(400) was never subject to IncludeErrorDetailPolicy either.
            var badRequest = context.Exception as BadHttpRequestException;
            var statusCode = badRequest?.StatusCode ?? StatusCodes.Status500InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Title = badRequest != null
                    ? badRequest.Message
                    : (_config.IncludeExceptionDetailInErrors ? context.Exception.Message : "An error has occurred."),
                Detail = _config.IncludeExceptionDetailInErrors ? context.Exception.ToString() : null,
                Status = statusCode,
            };

            var objectResult = new ObjectResult(problemDetails)
            {
                StatusCode = statusCode,
            };

            // A result substituted by an exception filter bypasses the normal result-filter pipeline
            // entirely in ASP.NET Core - JsonApiResultFilter never runs for it (confirmed: without
            // this direct call, the built-in [ApiController] problem-details formatter served this
            // response as application/problem+json instead). Process it directly here instead of
            // relying on the filter pipeline. requiresMediaType: false - we've already established
            // above (isExplicitJsonApi || ShouldProcessAsJsonApi) that this request is JSON:API.
            JsonApiResultFilter.ProcessResult(context.HttpContext, objectResult, _config, requiresMediaType: false);

            context.Result = objectResult;
            context.ExceptionHandled = true;
        }
    }
}
