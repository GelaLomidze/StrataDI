using System;
using System.Reflection;

namespace StrataDI
{
    internal sealed class InjectionMetadata
    {
        public InjectionMetadata(
            FieldInfo[] fields,
            PropertyInfo[] properties,
            MethodInjectionMetadata[] methods)
        {
            Fields = fields;
            Properties = properties;
            Methods = methods;
        }

        public FieldInfo[] Fields { get; }
        public PropertyInfo[] Properties { get; }
        public MethodInjectionMetadata[] Methods { get; }
    }

    internal sealed class MethodInjectionMetadata
    {
        public MethodInjectionMetadata(
            MethodInfo method,
            Type[] parameterTypes)
        {
            Method = method;
            ParameterTypes = parameterTypes;
        }

        public MethodInfo Method { get; }
        public Type[] ParameterTypes { get; }
    }

    internal sealed class ConstructorInjectionMetadata
    {
        public ConstructorInjectionMetadata(
            ConstructorInfo constructor,
            Type[] parameterTypes)
        {
            Constructor = constructor;
            ParameterTypes = parameterTypes;
        }

        public ConstructorInfo Constructor { get; }
        public Type[] ParameterTypes { get; }
    }
}