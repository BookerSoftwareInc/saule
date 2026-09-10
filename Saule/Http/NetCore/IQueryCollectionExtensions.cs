using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Saule.Http
{
    /// <summary>
    /// Flattens ASP.NET Core's <see cref="IQueryCollection"/> into the same
    /// <c>IEnumerable&lt;KeyValuePair&lt;string,string&gt;&gt;</c> shape net47's
    /// <c>HttpRequestMessage.GetQueryNameValuePairs()</c> produces (one pair per value, so a
    /// repeated key like <c>sort=a&amp;sort=b</c> yields two pairs) - the portable
    /// <c>Saule.Queries.*</c> context constructors (<c>SortContext</c>, <c>FilterContext</c>, etc.)
    /// take that shape unchanged on both TFMs.
    /// </summary>
    internal static class IQueryCollectionExtensions
    {
        internal static IEnumerable<KeyValuePair<string, string>> ToNameValuePairs(this IQueryCollection query)
        {
            foreach (var key in query.Keys)
            {
                foreach (var value in query[key])
                {
                    // PR review finding: the shared Saule.Queries.* contexts expect the net47
                    // UriExtensions.ParseQueryNameValuePairs() normalization (page[number] ->
                    // page.number) - without it, bracketed keys never match Constants.QueryNames'
                    // dotted form and pagination/filter/fields lookups silently no-op.
                    yield return new KeyValuePair<string, string>(
                        key.Replace("[", ".").Replace("]", string.Empty),
                        value);
                }
            }
        }
    }
}
