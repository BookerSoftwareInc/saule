using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
            return _query.Keys.Any(k => string.Equals(k.ToPascalCase(), prefix, System.StringComparison.OrdinalIgnoreCase));
        }

        public ValueProviderResult GetValue(string key)
        {
            var match = _query.FirstOrDefault(p => string.Equals(p.Key.ToPascalCase(), key, System.StringComparison.OrdinalIgnoreCase));

            if (match.Key == null)
            {
                return ValueProviderResult.None;
            }

            return new ValueProviderResult(match.Value.ToString(), _culture);
        }
    }
}
