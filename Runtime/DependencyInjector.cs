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

            InjectionMetadata metadata = InjectionMetadataCache.GetInjectionMetadata(target.GetType());

            if (!CanResolveAllDependencies(metadata))
            {
                AddPendingObject(target);
                return;
            }

            InjectFields(target, metadata);
            InjectProperties(target, metadata);
            InjectMethods(target, metadata);

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

        private bool CanResolveAllDependencies(InjectionMetadata metadata)
        {
            foreach (FieldInfo field in metadata.Fields)
            {
                if (!_container.TryResolve(field.FieldType, out _))
                {
                    return false;
                }
            }

            foreach (PropertyInfo property in metadata.Properties)
            {
                if (!_container.TryResolve(property.PropertyType, out _))
                {
                    return false;
                }
            }

            foreach (MethodInjectionMetadata method in metadata.Methods)
            {
                foreach (Type parameterType in method.ParameterTypes)
                {
                    if (!_container.TryResolve(parameterType, out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void InjectFields(
            object target,
            InjectionMetadata metadata)
        {
            foreach (FieldInfo field in metadata.Fields)
            {
                object dependency = _container.Resolve(field.FieldType);
                field.SetValue(target, dependency);
            }
        }

        private void InjectProperties(
            object target,
            InjectionMetadata metadata)
        {
            foreach (PropertyInfo property in metadata.Properties)
            {
                object dependency = _container.Resolve(property.PropertyType);
                property.SetValue(target, dependency);
            }
        }

        private void InjectMethods(
            object target,
            InjectionMetadata metadata)
        {
            foreach (MethodInjectionMetadata method in metadata.Methods)
            {
                Type[] parameterTypes = method.ParameterTypes;
                object[] dependencies = new object[parameterTypes.Length];

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    dependencies[i] =
                        _container.Resolve(parameterTypes[i]);
                }

                method.Method.Invoke(target, dependencies);
            }
        }

        private void AddPendingObject(MonoBehaviour target)
        {
            if (!_pendingObjects.Contains(target))
            {
                _pendingObjects.Add(target);
            }
        }
    }
}
