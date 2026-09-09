using Microsoft.Extensions.DependencyInjection;

namespace Saule.Http
{
    /// <summary>
    /// net10.0 (ASP.NET Core) equivalent of the net47 <c>Saule.Http.HttpConfigExtensions</c> - same
    /// method name (<c>ConfigureJsonApi</c>) and same <see cref="JsonApiConfiguration"/>/
    /// <see cref="FormatterPriority"/> parameters, called against <see cref="IMvcBuilder"/> instead
    /// of <c>HttpConfiguration</c> (the one place a consumer's <c>Program.cs</c>/<c>Startup.cs</c>
    /// must change when it migrates to net10.0 - Standard-2.0-Migration-Plan.md Section 7.1/7.2).
    /// </summary>
    public static class MvcBuilderExtensions
    {
        /// <summary>
        /// Sets up serialization and deserialization of Json Api resources.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/> that is used in the setup of the application.</param>
        public static IMvcBuilder ConfigureJsonApi(this IMvcBuilder builder)
        {
            return ConfigureJsonApi(builder, new JsonApiConfiguration());
        }

        /// <summary>
        /// Sets up serialization and deserialization of Json Api resources.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/> that is used in the setup of the application.</param>
        /// <param name="jsonApiConfiguration">JsonApiConfiguration parameters for Json Api serialization.</param>
        public static IMvcBuilder ConfigureJsonApi(this IMvcBuilder builder, JsonApiConfiguration jsonApiConfiguration)
        {
            return ConfigureJsonApi(builder, jsonApiConfiguration, false);
        }

        /// <summary>
        /// Sets up serialization and deserialization of Json Api resources.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/> that is used in the setup of the application.</param>
        /// <param name="jsonApiConfiguration">JsonApiConfiguration parameters for Json Api serialization.</param>
        /// <param name="overwriteOtherFormatters">
        /// If true, other formatters will be cleared. Otherwise, the JSON API formatter
        /// will be inserted at the start of the collection.
        /// </param>
        public static IMvcBuilder ConfigureJsonApi(
            this IMvcBuilder builder,
            JsonApiConfiguration jsonApiConfiguration,
            bool overwriteOtherFormatters)
        {
            return ConfigureJsonApi(
                builder,
                jsonApiConfiguration,
                overwriteOtherFormatters ? FormatterPriority.OverwriteOtherFormatters : FormatterPriority.AddFormatterToStart);
        }

        /// <summary>
        ///  Sets up serialization and deserialization of Json Api resources.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/> that is used in the setup of the application.</param>
        /// <param name="jsonApiConfiguration">JsonApiConfiguration parameters for Json Api serialization.</param>
        /// <param name="formatterPriority"> Determines the relative position of the JSON API formatter.</param>
        public static IMvcBuilder ConfigureJsonApi(
            this IMvcBuilder builder,
            JsonApiConfiguration jsonApiConfiguration,
            FormatterPriority formatterPriority)
        {
            JsonApiAttribute.JsonApiConfiguration = jsonApiConfiguration;

            builder.AddMvcOptions(options =>
            {
                options.Filters.Add(new JsonApiResultFilter(jsonApiConfiguration));
                options.ValueProviderFactories.Add(new JsonApiQueryValueProviderFactory());

                var outputFormatter = new JsonApiOutputFormatter(jsonApiConfiguration);
                var inputFormatter = new JsonApiInputFormatter(jsonApiConfiguration);

                if (formatterPriority == FormatterPriority.OverwriteOtherFormatters)
                {
                    options.OutputFormatters.Clear();
                    options.InputFormatters.Clear();
                    options.OutputFormatters.Add(outputFormatter);
                    options.InputFormatters.Add(inputFormatter);
                }
                else if (formatterPriority == FormatterPriority.AddFormatterToEnd)
                {
                    options.OutputFormatters.Add(outputFormatter);
                    options.InputFormatters.Add(inputFormatter);
                }
                else
                {
                    options.OutputFormatters.Insert(0, outputFormatter);
                    options.InputFormatters.Insert(0, inputFormatter);
                }
            });

            return builder;
        }
    }
}
