using System;
using System.Collections.Generic;
using System.Linq;
#if NETFRAMEWORK
using System.Web.Http;
#elif NET10_0
using Microsoft.AspNetCore.Mvc;
#endif

namespace Saule.Serialization
{
    internal class ApiError
    {
        private readonly JsonApiException _exception;

        public ApiError(Exception ex)
        {
            Title = ex.Message;
            Detail = ex.ToString();
            Code = ex.GetType().FullName;
            Links = ex.HelpLink != null
                ? new Dictionary<string, string> { ["about"] = ex.HelpLink }
                : null;

            _exception = ex as JsonApiException;
        }

#if NETFRAMEWORK
        internal ApiError(HttpError ex)
        {
            Title = GetRecursiveExceptionMessage(ex);
            Detail = ex.StackTrace;
            Code = ex.ExceptionType;
        }
#elif NET10_0
        internal ApiError(ProblemDetails problemDetails)
        {
            Title = problemDetails.Title ?? problemDetails.Detail;
            Detail = problemDetails.Detail;
            Code = problemDetails.Type ?? problemDetails.Status?.ToString();
        }
#endif

        public string Title { get; }

        public string Detail { get; }

        public string Code { get; }

        public Dictionary<string, string> Links { get; }

        public static bool IsClientError(List<ApiError> errors)
        {
            return errors.Any(IsClientError);
        }

        public static bool IsClientError(ApiError error)
        {
            return error._exception != null && error._exception.ErrorType == ErrorType.Client;
        }

#if NETFRAMEWORK
        private static string GetRecursiveExceptionMessage(HttpError ex)
        {
            var msg = !string.IsNullOrEmpty(ex.ExceptionMessage) ? ex.ExceptionMessage : ex.Message;
            msg = msg?.EnsureEndsWith(".");
            if (ex.InnerException != null)
            {
                msg += ' ' + GetRecursiveExceptionMessage(ex.InnerException);
            }

            return msg;
        }
#endif
    }
}