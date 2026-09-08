using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StrataDI
{
    internal sealed class DependencyInjector
    {
        private readonly DependencyContainer _container;
        private readonly List<MonoBehaviour> _pendingObjects = new();
        private readonly HashSet<object> _completedObjects = new();

        public DependencyInjector(DependencyContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public void InjectGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            MonoBehaviour[] components =
                gameObject.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour component in components)
            {
                TryInjectAndNotify(component);
            }
        }

        public void TryInjectAndNotify(MonoBehaviour target)
        {
            if (target == null || _completedObjects.Contains(target))
            {
                return;
            }

            if (!CanResolveAllDependencies(target))
            {
                AddPendingObject(target);
                return;
            }

            InjectFields(target);
            InjectProperties(target);
            InjectMethods(target);

            _pendingObjects.Remove(target);
            _completedObjects.Add(target);

            if (target is IInjectionCallback callback)
            {
                callback.OnInjected();
            }
        }

        public void RetryPendingObjects()
        {
            if (_pendingObjects.Count == 0)
            {
                return;
            }

            MonoBehaviour[] pendingObjects = _pendingObjects.ToArray();

            foreach (MonoBehaviour pendingObject in pendingObjects)
            {
                if (pendingObject == null)
                {
                    _pendingObjects.Remove(pendingObject);
                    continue;
                }

                TryInjectAndNotify(pendingObject);
            }
        }

        private bool CanResolveAllDependencies(object target)
        {
            foreach (FieldInfo field in GetInjectableFields(target.GetType()))
            {
                if (!_container.TryResolve(field.FieldType, out _))
                {
                    return false;
                }
            }

            foreach (PropertyInfo property in GetInjectableProperties(target.GetType()))
            {
                if (!_container.TryResolve(property.PropertyType, out _))
                {
                    return false;
                }
            }

            foreach (MethodInfo method in GetInjectableMethods(target.GetType()))
            {
                ParameterInfo[] parameters = method.GetParameters();

                foreach (ParameterInfo parameter in parameters)
                {
                    if (!_container.TryResolve(parameter.ParameterType, out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void InjectFields(object target)
        {
            foreach (FieldInfo field in GetInjectableFields(target.GetType()))
            {
                object dependency = _container.Resolve(field.FieldType);
                field.SetValue(target, dependency);
            }
        }

        private void InjectProperties(object target)
        {
            foreach (PropertyInfo property in GetInjectableProperties(target.GetType()))
            {
                object dependency = _container.Resolve(property.PropertyType);
                property.SetValue(target, dependency);
            }
        }

        private void InjectMethods(object target)
        {
            foreach (MethodInfo method in GetInjectableMethods(target.GetType()))
            {
                ParameterInfo[] parameters = method.GetParameters();
                object[] dependencies = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    dependencies[i] =
                        _container.Resolve(parameters[i].ParameterType);
                }

                method.Invoke(target, dependencies);
            }
        }

        private void AddPendingObject(MonoBehaviour target)
        {
            if (!_pendingObjects.Contains(target))
            {
                _pendingObjects.Add(target);
            }
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

        private static IEnumerable<FieldInfo> GetInjectableFields(Type targetType)
        {
            foreach (Type type in EnumerateTypeHierarchy(targetType))
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
                        yield return field;
                    }
                }
            }
        }

        private static IEnumerable<PropertyInfo> GetInjectableProperties(Type targetType)
        {
            foreach (Type type in EnumerateTypeHierarchy(targetType))
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
                    yield return property;
                }
            }
        }

        private static IEnumerable<MethodInfo> GetInjectableMethods(Type targetType)
        {
            foreach (Type type in EnumerateTypeHierarchy(targetType))
            {
                MethodInfo[] methods = type.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                foreach (MethodInfo method in methods)
                {
                    if (method.GetCustomAttribute<InjectAttribute>() != null)
                    {
                        yield return method;
                    }
                }
            }
        }

        private static IEnumerable<Type> EnumerateTypeHierarchy(Type targetType)
        {
            Stack<Type> hierarchy = new();
            Type currentType = targetType;

            while (currentType != null && currentType != typeof(MonoBehaviour))
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
