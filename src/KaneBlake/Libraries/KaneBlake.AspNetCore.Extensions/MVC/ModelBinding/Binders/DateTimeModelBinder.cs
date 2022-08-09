using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace K.AspNetCore.Extensions.MVC.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinder"/> for <see cref="DateTime"/> and nullable <see cref="DateTime"/> models.
    /// </summary>
    public class DateTimeModelBinder : IModelBinder
    {
        private readonly DateTimeStyles _supportedStyles;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DateTimeModelBinder"/>.
        /// </summary>
        /// <param name="supportedStyles">The <see cref="DateTimeStyles"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DateTimeModelBinder(DateTimeStyles supportedStyles, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _supportedStyles = supportedStyles;
            _logger = loggerFactory.CreateLogger<DateTimeModelBinder>();
        }

        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }


            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var modelState = bindingContext.ModelState;
            modelState.SetModelValue(modelName, valueProviderResult);

            var metadata = bindingContext.ModelMetadata;
            var type = metadata.UnderlyingOrModelType;
            try
            {
                if (type != typeof(DateTime))
                {
                    throw new NotSupportedException();
                }

                var value = valueProviderResult.FirstValue;

                object model = null;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (DateTime.TryParse(value, valueProviderResult.Culture, _supportedStyles, out var modelValue))
                    {
                        if (modelValue.Kind.Equals(DateTimeKind.Unspecified))
                        {
                            modelValue = modelValue.ToLocalTime();
                        }
                        model = modelValue;
                    }
                    else if (double.TryParse(value, out var unixTimeStamp))
                    {
                        model = DateTime.UnixEpoch.AddMilliseconds(unixTimeStamp);
                    }
                }

                // When converting value, a null model may indicate a failed conversion for an otherwise required
                // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
                // current bindingContext. If not, an error is logged.
                if (model == null && !metadata.IsReferenceOrNullableType)
                {
                    modelState.TryAddModelError(
                        modelName,
                        metadata.ModelBindingMessageProvider.ValueIsInvalidAccessor(
                            valueProviderResult.ToString()));
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Success(model);
                }
            }
            catch (Exception exception)
            {
                // Conversion failed.
                modelState.TryAddModelError(modelName, exception, metadata);
            }

            return Task.CompletedTask;
        }
    }
}
