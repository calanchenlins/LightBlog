using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace K.AspNetCore.Extensions.MVC.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for binding <see cref="DateTimeOffset" /> and nullable <see cref="DateTime"/> models.
    /// </summary>
    public class DateTimeOffsetModelBinderProvider : IModelBinderProvider
    {
        internal const DateTimeStyles SupportedStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;

        /// <inheritdoc />
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var modelType = context.Metadata.UnderlyingOrModelType;
            if (modelType == typeof(DateTimeOffset))
            {
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                return new DateTimeOffsetModelBinder(SupportedStyles, loggerFactory);
            }

            return null;
        }
    }
}
