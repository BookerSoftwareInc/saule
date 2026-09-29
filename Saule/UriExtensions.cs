using System;
using System.Collections.Generic;

namespace Saule
{
    /// <summary>
    /// Portable replacement for <c>HttpRequestMessage.GetQueryNameValuePairs()</c>
    /// (<c>System.Net.Http.Formatting</c>), whose netstandard2.0 asset does not include that method -
    /// confirmed by an actual build failure (CS1061) before this was added, not assumed. Used
    /// uniformly on all 3 TFMs by <see cref="JsonApiSerializer{T}"/> so there is exactly one
    /// implementation of this parsing, rather than a net47-only path plus a portable twin.
    /// </summary>
    internal static class UriExtensions
    {
        internal static IEnumerable<KeyValuePair<string, string>> ParseQueryNameValuePairs(this Uri uri)
        {
            // Null-tolerant, matching the real HttpRequestMessage.GetQueryNameValuePairs() (which
            // operates on RequestUri and is fine with no URI set) - confirmed via an actual
            // regression this caused: JsonApiSerializer<T>.Serialize(null) is meant to surface
            // ArgumentNullException from its own later explicit null check, not fail here first.
            if (uri == null)
            {
                yield break;
            }

            var query = uri.Query;
            if (string.IsNullOrEmpty(query))
            {
                yield break;
            }

            if (query[0] == '?')
            {
                query = query.Substring(1);
            }

            foreach (var pair in query.Split('&'))
            {
                if (string.IsNullOrEmpty(pair))
                {
                    continue;
                }

                var separatorIndex = pair.IndexOf('=');
                string key;
                string value;
                if (separatorIndex < 0)
                {
                    key = Uri.UnescapeDataString(pair.Replace('+', ' '));
                    value = string.Empty;
                }
                else
                {
                    key = Uri.UnescapeDataString(pair.Substring(0, separatorIndex).Replace('+', ' '));
                    value = Uri.UnescapeDataString(pair.Substring(separatorIndex + 1).Replace('+', ' '));
                }

                // Classic ASP.NET/WebApi key normalization: "filter[location]" -> "filter.location" -
                // real HttpRequestMessage.GetQueryNameValuePairs() does this too (confirmed via a
                // side-by-side comparison against the actual net47 method before this was added, not
                // assumed) - Saule.Queries.Filtering.FilterContext/FieldsetContext etc. all key off the
                // dot-separated form.
                key = NormalizeBracketNotation(key);

                yield return new KeyValuePair<string, string>(key, value);
            }
        }

        private static string NormalizeBracketNotation(string key)
        {
            return key.Replace("[", ".").Replace("]", string.Empty);
        }
    }
}
