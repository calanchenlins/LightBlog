using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace K.AspNetCore.Extensions.Services.Module
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class AutoImplementationItemAttribute : Attribute
    {
        public AutoImplementationItemAttribute(Type abstractType, Type implementationType)
        {
            AbstractType = abstractType ?? throw new ArgumentNullException(nameof(abstractType));
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        }

        public Type AbstractType { get; }
        public Type ImplementationType { get; }
    }

    public interface IStoreAccessor
    {
        T GetState<T>(string name);

        void SetState<T>(string name, T stateValue);
    }

    public abstract class DefaultStoreAccessor : IStoreAccessor
    {
        private readonly IDictionary<string, object> _propertyValues;

        protected DefaultStoreAccessor(IDictionary<string, object> values)
        {
            _propertyValues = values ?? throw new ArgumentNullException(nameof(values));
        }

        public T GetState<T>(string name)
        {
            if (_propertyValues.TryGetValue(name, out object objectValue) && objectValue is JsonElement jsonElement)
            {
                objectValue = default(T);

                var propertyType = typeof(T);

                var jsonValue = jsonElement.GetString();

                var typeConverter = TypeDescriptor.GetConverter(propertyType);

                if (typeConverter.CanConvertFrom(typeof(string)))
                {
                    objectValue = (T)typeConverter.ConvertFromString(jsonValue);
                }
                else if (!(propertyType.IsInterface || propertyType.IsAbstract))
                {
                    try
                    {
                        objectValue = JsonSerializer.Deserialize<T>(jsonValue);
                    }
                    catch (Exception ex)
                    {
                        objectValue = ex.Message;
                    }
                }
                _propertyValues[name] = objectValue;

            }
            if (objectValue is T value)
            {
                return value;
            }
            return default;
        }

        public void SetState<T>(string name, T propertyValue)
        {
            _propertyValues[name] = propertyValue;
        }
    }
}
