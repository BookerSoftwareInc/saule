using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.JsonApiQueryValueProvider</c> -
    /// same PascalCase-conversion intent (converts JSON:API's kebab-case query keys so the model
    /// binder can bind them to action parameters), different base interface
    /// (<see cref="IValueProvider"/> vs. <c>NameValuePairsValueProvider</c> - see
    /// Standard-2.0-Migration-Plan.md Section 7.1). ASP.NET Core already hands us parsed
    /// <see cref="IQueryCollection"/> pairs, unlike net47's raw query string, so no manual parsing
    /// is needed here (compare to the portable UriExtensions.ParseQueryNameValuePairs()).
    /// </summary>
    internal sealed class JsonApiQueryValueProvider : IValueProvider
    {
        private readonly IQueryCollection _query;
        private readonly CultureInfo _culture;

        internal JsonApiQueryValueProvider(IQueryCollection query, CultureInfo culture)
        {
            _query = query;
            _culture = culture;
        }

        public bool ContainsPrefix(string prefix)
        {
            // NameValuePairsValueProvider (net47's base class) uses actual prefix matching, not
            // exact equality - needed for complex/nested model binding (e.g. a [FromUri] filter
            // object querying whether any key starts with "Filter."). Exact equality here would
            // make such binding silently fail to see any of its properties.
            return _query.Keys.Any(k => NormalizeKey(k).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        public ValueProviderResult GetValue(string key)
        {
            var matches = _query
                .Where(p => string.Equals(NormalizeKey(p.Key), key, StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => p.Value.ToArray())
                .Where(v => v != null)
                .ToArray();

            if (matches.Length == 0)
            {
                return ValueProviderResult.None;
            }

            // A repeated key (?sort=a&sort=b) must bind as multiple StringValues entries, not one
            // comma-joined string, or an array/list-bound parameter only ever sees one element.
            return new ValueProviderResult(new StringValues(matches), _culture);
        }

        // PR review finding: net47's provider gets bracket-normalized keys from
        // GetQueryNameValuePairs() (filter[location] -> filter.location) before ToPascalCase() ever
        // runs; IQueryCollection here still has raw bracket notation. ToPascalCase() alone treats
        // "[" and "]" as ordinary characters, not separators, so "filter[location]" became
        // "Filter[location]" - never matching a bound property path like "Filter.Location". Normalize
        // brackets to dots first, exactly like the portable UriExtensions.ParseQueryNameValuePairs().
        private static string NormalizeKey(string key)
        {
            return key.Replace("[", ".").Replace("]", string.Empty).ToPascalCase();
        }
    }
}
