using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Saule.Queries;
using Saule.Queries.Pagination;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.PaginatedAttribute</c> - same
    /// name/namespace/public properties (so reflection-based readers like Saule.Extended's
    /// RequestExtensions.cs keep working once it migrates - Standard-2.0-Migration-Plan.md
    /// Section 7.1), same validation logic, different filter base class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class PaginatedAttribute : ActionFilterAttribute
    {
        private int? _perPage;
        private int? _queryPageSizeLimit;
        private int _firstPageNumber = 0;

        /// <summary>
        /// Gets or sets the number of items to return per response.
        /// </summary>
        public int PerPage
        {
            get
            {
                return _perPage ?? Constants.QueryValues.ValueNotSpecified;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(PerPage), value, "Must have at least one item per page.");
                }

                if (_queryPageSizeLimit.HasValue && value > _queryPageSizeLimit)
                {
                    throw new ArgumentOutOfRangeException(nameof(PageSizeLimit), value, "Items per page cannot be larger than the page size limit.");
                }

                _perPage = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum page size to accept from a URL query string.
        /// </summary>
        public int PageSizeLimit
        {
            get
            {
                return _queryPageSizeLimit ?? Constants.QueryValues.ValueNotSpecified;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(PageSizeLimit), value, "Must have at least one item per page.");
                }

                if (_perPage.HasValue && value < _perPage)
                {
                    throw new ArgumentOutOfRangeException(nameof(PageSizeLimit), value, "PageSizeLimit cannot be smaller than page size.");
                }

                _queryPageSizeLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the first page number. Default value is 0.
        /// </summary>
        public int FirstPageNumber
        {
            get
            {
                return _firstPageNumber;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(FirstPageNumber), value, "The first page should be more or equal to 0.");
                }

                _firstPageNumber = value;
            }
        }

        /// <inheritdoc/>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var paginationContext = new PaginationContext(
                context.HttpContext.Request.Query.ToNameValuePairs(),
                _perPage,
                _queryPageSizeLimit,
                _firstPageNumber);

            var query = QueryContextUtils.GetQueryContext(context);

            query.Pagination = paginationContext;
            base.OnActionExecuting(context);
        }
    }
}
