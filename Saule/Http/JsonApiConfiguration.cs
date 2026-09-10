using System.Collections.Generic;
using Newtonsoft.Json;
using Saule.Resources;
using Saule.Serialization;

namespace Saule.Http
{
    /// <summary>
    /// Contains settings to influence the Json Api serialization process.
    /// </summary>
    public class JsonApiConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonApiConfiguration"/> class.
        /// </summary>
        public JsonApiConfiguration()
        {
        }

        /// <summary>
        /// Gets or sets the PropertyNameConverter which determines
        /// the format of json properties and model properties
        /// </summary>
        public IPropertyNameConverter PropertyNameConverter { get; set; } = new DefaultPropertyNameConverter();

        /// <summary>
        /// Gets or sets the UrlPathBuilder which determines how to generate urls for links.
        /// </summary>
        public IUrlPathBuilder UrlPathBuilder { get; set; } = null;

        /// <summary>
        /// Gets the JsonConverters to manipulate the serialization process.
        /// </summary>
        public List<JsonConverter> JsonConverters { get; } = new List<JsonConverter>();

        /// <summary>
        /// Gets the expressions that are used to evaluate filter queries on a per-type basis.
        /// </summary>
        public QueryFilterExpressionCollection QueryFilterExpressions { get; } = new QueryFilterExpressionCollection();

        /// <summary>
        /// Gets or sets the factory that creates request specific ApiResourceProvider
        /// </summary>
        public IApiResourceProviderFactory ApiResourceProviderFactory { get; set; } = new DefaultApiResourceProviderFactory();

        /// <summary>
        /// Gets or sets a value indicating whether an unhandled exception's full details (message,
        /// stack trace) are included in the JSON:API error response's <c>detail</c> member. Defaults
        /// to <c>false</c> - the response only ever gets a generic message unless a consumer
        /// explicitly opts in (e.g. for their own non-production environments). Deliberately not
        /// tied to <c>IHostEnvironment.IsDevelopment()</c>: that reads an ambient, easy-to-misconfigure
        /// host setting, whereas this is a plain, testable setting the consumer controls directly.
        /// </summary>
        public bool IncludeExceptionDetailInErrors { get; set; } = false;
    }
}