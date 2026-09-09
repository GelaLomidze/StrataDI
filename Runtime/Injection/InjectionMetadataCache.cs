using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StrataDI
{
    internal static class InjectionMetadataCache
    {
        private static readonly Dictionary<Type, InjectionMetadata>
            _injectionMetadata = new();

        private static readonly Dictionary<Type, ConstructorInjectionMetadata>
            _constructorMetadata = new();

        public static InjectionMetadata GetInjectionMetadata(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_injectionMetadata.TryGetValue(type, out InjectionMetadata metadata))
            {
                return metadata;
            }

            metadata = BuildInjectionMetadata(type);
            _injectionMetadata[type] = metadata;

            return metadata;
        }

        public static ConstructorInjectionMetadata GetConstructorMetadata(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_constructorMetadata.TryGetValue(
                    type,
                    out ConstructorInjectionMetadata metadata))
            {
                return metadata;
            }

            metadata = BuildConstructorMetadata(type);
            _constructorMetadata[type] = metadata;

            return metadata;
        }

        private static InjectionMetadata BuildInjectionMetadata(Type targetType)
        {
            List<FieldInfo> fields = new();
            List<PropertyInfo> properties = new();
            List<MethodInjectionMetadata> methods = new();

            foreach (Type type in EnumerateTypeHierarchy(targetType))
            {
                CollectFields(type, fields);
                CollectProperties(type, properties);
                CollectMethods(type, methods);
            }

            return new InjectionMetadata(
                fields.ToArray(),
                properties.ToArray(),
                methods.ToArray());
        }

        private static void CollectFields(
            Type type,
            List<FieldInfo> result)
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            foreach (FieldInfo field in fields)
            {
                if (field.GetCustomAttribute<InjectAttribute>() != null)
                {
                    result.Add(field);
                }
            }
        }

        private static void CollectProperties(
            Type type,
            List<PropertyInfo> result)
        {
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            foreach (PropertyInfo property in properties)
            {
                if (property.GetCustomAttribute<InjectAttribute>() == null)
                {
                    continue;
                }

                ValidateInjectableProperty(property);
                result.Add(property);
            }
        }

        private static void CollectMethods(
            Type type,
            List<MethodInjectionMetadata> result)
        {
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in methods)
            {
                if (method.GetCustomAttribute<InjectAttribute>() == null)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                Type[] parameterTypes = new Type[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    parameterTypes[i] = parameters[i].ParameterType;
                }

                result.Add(
                    new MethodInjectionMetadata(
                        method,
                        parameterTypes));
            }
        }

        private static ConstructorInjectionMetadata BuildConstructorMetadata(
            Type type)
        {
            ConstructorInfo constructor = FindInjectionConstructor(type);
            ParameterInfo[] parameters = constructor.GetParameters();

            Type[] parameterTypes = new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }

            return new ConstructorInjectionMetadata(
                constructor,
                parameterTypes);
        }

        private static ConstructorInfo FindInjectionConstructor(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            ConstructorInfo injectConstructor = null;

            foreach (ConstructorInfo constructor in constructors)
            {
                if (constructor.GetCustomAttribute<InjectAttribute>() == null)
                {
                    continue;
                }

                if (injectConstructor != null)
                {
                    throw new InvalidOperationException(
                        $"Type {type.FullName} has multiple constructors marked " +
                        $"with [{nameof(InjectAttribute)}]. " +
                        "Only one injection constructor is allowed.");
                }

                injectConstructor = constructor;
            }

            if (injectConstructor != null)
            {
                return injectConstructor;
            }

            ConstructorInfo[] publicConstructors = type.GetConstructors(
                BindingFlags.Instance |
                BindingFlags.Public);

            if (publicConstructors.Length == 1)
            {
                return publicConstructors[0];
            }

            if (publicConstructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName} does not have a public constructor. " +
                    $"Mark one constructor with [{nameof(InjectAttribute)}].");
            }

            throw new InvalidOperationException(
                $"Type {type.FullName} has multiple public constructors. " +
                "Mark the constructor StrataDI should use with " +
                $"[{nameof(InjectAttribute)}].");
        }

        private static void ValidateInjectableProperty(PropertyInfo property)
        {
            if (property.GetIndexParameters().Length > 0)
            {
                throw new InvalidOperationException(
                    $"Property {property.DeclaringType?.FullName}.{property.Name} " +
                    "cannot be injected because indexer properties are not supported.");
            }

            if (property.GetSetMethod(true) == null)
            {
                throw new InvalidOperationException(
                    $"Property {property.DeclaringType?.FullName}.{property.Name} " +
                    "cannot be injected because it does not have a setter.");
            }
        }

        private static IEnumerable<Type> EnumerateTypeHierarchy(Type targetType)
        {
            Stack<Type> hierarchy = new();
            Type currentType = targetType;

            while (currentType != null &&
                   currentType != typeof(MonoBehaviour))
            {
                hierarchy.Push(currentType);
                currentType = currentType.BaseType;
            }

            while (hierarchy.Count > 0)
            {
                yield return hierarchy.Pop();
            }
        }
    }
}