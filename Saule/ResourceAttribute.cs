using System.Collections.Generic;

using Newtonsoft.Json;

namespace Saule
{
    /// <summary>
    /// Represents an attribute on a resource.
    /// </summary>
    public class ResourceAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        public ResourceAttribute(string name)
        {
            InternalName = name;
            Name = name.ToDashed();
            PropertyName = name.ToPascalCase();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="converters">List of converters to be used when serializing the value.</param>
        public ResourceAttribute(string name, IEnumerable<JsonConverter> converters)
            : this(name)
        {
            JsonConverters = converters;
        }

        /// <summary>
        /// Gets the name of the attribute as provided from the ApiResource definition.
        /// </summary>
        public string InternalName { get; }

        /// <summary>
        /// Gets the name of the attribute in dashed JSON API format.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the attribute in PascalCase.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets json Converters that can be used to serialize resource
        /// </summary>
        public IEnumerable<JsonConverter> JsonConverters { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return PropertyName;
        }
    }
}