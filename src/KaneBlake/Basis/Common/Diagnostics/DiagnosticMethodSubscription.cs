using AspectCore.Extensions.Reflection;
using KaneBlake.Basis.Common.Diagnostics.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KaneBlake.Basis.Common.Diagnostics
{
    public interface IParameterResolver
    {
        object Resolve(object value);
    }
    internal class DiagnosticMethodSubscription
    {
        private readonly IDiagnosticProcessor _DiagnosticProcessor;
        private readonly string _diagnosticName;
        private readonly IParameterResolver[] _parameterResolvers;
        private readonly MethodReflector _reflector;

        public DiagnosticMethodSubscription(IDiagnosticProcessor diagnosticProcessor, MethodInfo method,
            string diagnosticName)
        {
            _DiagnosticProcessor = diagnosticProcessor;
            _reflector = method.GetReflector();
            _diagnosticName = diagnosticName;
            _parameterResolvers = GetParameterResolvers(method).ToArray();
        }

        public void Invoke(string diagnosticName, object value)
        {
            if (_diagnosticName != diagnosticName)
            {
                return;
            }

            var args = new object[_parameterResolvers.Length];
            for (var i = 0; i < _parameterResolvers.Length; i++)
            {
                args[i] = _parameterResolvers[i].Resolve(value);
            }
            _reflector.Invoke(_DiagnosticProcessor, args);

        }

        private static IEnumerable<IParameterResolver> GetParameterResolvers(MethodInfo methodInfo)
        {
            foreach (var parameter in methodInfo.GetParameters())
            {
                var binder = parameter.GetCustomAttribute<ParameterBinder>();
                if (binder != null)
                {
                    if (binder is ObjectAttribute objectBinder)
                    {
                        if (objectBinder.TargetType == null)
                        {
                            objectBinder.TargetType = parameter.ParameterType;
                        }
                    }
                    if (binder is PropertyAttribute propertyBinder)
                    {
                        if (propertyBinder.Name == null)
                        {
                            propertyBinder.Name = parameter.Name;
                        }
                    }
                    yield return binder;
                }
                else
                {
                    yield return new NullParameterResolver();
                }
            }
        }



    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public abstract class ParameterBinder : Attribute, IParameterResolver
    {
        public abstract object Resolve(object value);
    }
    public class ObjectAttribute : ParameterBinder
    {
        public Type TargetType { get; set; }

        public override object Resolve(object value)
        {
            if (TargetType == null || value == null)
            {
                return value;
            }

            if (TargetType == value.GetType())
            {
                return value;
            }

            if (TargetType.IsInstanceOfType(value))
            {
                return value;
            }

            return null;
        }
    }

    public class PropertyAttribute : ParameterBinder
    {
        public string Name { get; set; }
        public PropertyReflector PropReflector { get; set; }

        public override object Resolve(object value)
        {
            if (value == null || Name == null)
            {
                return null;
            }
            if (PropReflector!=null)
            {
                return PropReflector.GetValue(value);
            }
            PropReflector = value.GetType().GetProperty(Name)?.GetReflector();
            if (PropReflector == null)
            {
                Name = null;
                return null;
            }
            return PropReflector?.GetValue(value);
        }
    }
    public class NullParameterResolver : IParameterResolver
    {
        public object Resolve(object value)
        {
            return null;
        }
    }
}