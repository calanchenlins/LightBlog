using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.MVC
{
    /// <summary>
    /// Defines a serializable container for storing ModelState information.
    /// This information is stored as key/value pairs.
    /// </summary>
    public class SerializableModelError : Dictionary<string, string[]>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableModelError"/> class.
        /// </summary>
        public SerializableModelError()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="SerializableModelError"/>.
        /// </summary>
        /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
        public SerializableModelError(ModelStateDictionary modelState)
            : this()
        {

            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (modelState.IsValid)
            {
                return;
            }

            foreach (var keyModelStatePair in modelState)
            {
                var key = keyModelStatePair.Key;
                var errors = keyModelStatePair.Value.Errors;
                if (errors != null && errors.Count > 0)
                {
                    var errorMessages = errors.Select(error =>
                    {
                        return string.IsNullOrEmpty(error.ErrorMessage) ? "" : error.ErrorMessage;
                    }).ToArray();

                    Add(key, errorMessages);
                }
            }
        }
    }
}
