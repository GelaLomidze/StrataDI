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
        private readonly HashSet<MonoBehaviour> _pendingLookup = new();
        private readonly HashSet<MonoBehaviour> _completedObjects = new();

        private readonly Stack<List<MonoBehaviour>> _componentBufferPool = new();
        private readonly Stack<List<object>> _dependencyBufferPool = new();

        private readonly ObjectArrayPool _argumentPool = new();

        public DependencyInjector(DependencyContainer container)
        {
            _container = container ??
                throw new ArgumentNullException(nameof(container));
        }

        public void InjectGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            List<MonoBehaviour> components = RentComponentBuffer();

            try
            {
                gameObject.GetComponentsInChildren(
                    true,
                    components);

                InjectComponents(components);
            }
            finally
            {
                ReturnComponentBuffer(components);
            }
        }

        public void BindGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            List<MonoBehaviour> components = RentComponentBuffer();

            try
            {
                gameObject.GetComponentsInChildren(
                    true,
                    components);

                BindComponents(components);
            }
            finally
            {
                ReturnComponentBuffer(components);
            }
        }

        public void BindAndInjectGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            List<MonoBehaviour> components = RentComponentBuffer();

            try
            {
                gameObject.GetComponentsInChildren(
                    true,
                    components);

                BindComponents(components);
                InjectComponents(components);
            }
            finally
            {
                ReturnComponentBuffer(components);
            }
        }

        public void TryInjectAndNotify(MonoBehaviour target)
        {
            if (target == null ||
                _completedObjects.Contains(target))
            {
                return;
            }

            InjectionMetadata metadata =
                InjectionMetadataCache.GetInjectionMetadata(
                    target.GetType());

            List<object> dependencies =
                RentDependencyBuffer();

            try
            {
                if (!TryResolveAllDependencies(
                        metadata,
                        dependencies))
                {
                    AddPendingObject(target);
                    return;
                }

                InjectResolvedDependencies(
                    target,
                    metadata,
                    dependencies);

                if (_pendingLookup.Remove(target))
                {
                    _pendingObjects.Remove(target);
                }

                _completedObjects.Add(target);

                if (target is IInjectionCallback callback)
                {
                    callback.OnInjected();
                }
            }
            finally
            {
                ReturnDependencyBuffer(dependencies);
            }
        }

        public void RetryPendingObjects()
        {
            for (int i = _pendingObjects.Count - 1; i >= 0; i--)
            {
                MonoBehaviour pendingObject =
                    _pendingObjects[i];

                if (pendingObject == null)
                {
                    _pendingLookup.Remove(pendingObject);
                    _pendingObjects.RemoveAt(i);
                    continue;
                }

                TryInjectAndNotify(pendingObject);
            }
        }

        private void BindComponents(
            List<MonoBehaviour> components)
        {
            foreach (MonoBehaviour component in components)
            {
                if (component == null)
                {
                    continue;
                }

                _container.BindInstance(component);
            }
        }

        private void InjectComponents(
            List<MonoBehaviour> components)
        {
            foreach (MonoBehaviour component in components)
            {
                TryInjectAndNotify(component);
            }
        }

        private bool TryResolveAllDependencies(
            InjectionMetadata metadata,
            List<object> dependencies)
        {
            foreach (FieldInfo field in metadata.Fields)
            {
                if (!_container.TryResolve(
                        field.FieldType,
                        out object dependency))
                {
                    return false;
                }

                dependencies.Add(dependency);
            }

            foreach (PropertyInfo property in metadata.Properties)
            {
                if (!_container.TryResolve(
                        property.PropertyType,
                        out object dependency))
                {
                    return false;
                }

                dependencies.Add(dependency);
            }

            foreach (MethodInjectionMetadata method in metadata.Methods)
            {
                foreach (Type parameterType in method.ParameterTypes)
                {
                    if (!_container.TryResolve(
                            parameterType,
                            out object dependency))
                    {
                        return false;
                    }

                    dependencies.Add(dependency);
                }
            }

            return true;
        }

        private void InjectResolvedDependencies(
            object target,
            InjectionMetadata metadata,
            List<object> dependencies)
        {
            int dependencyIndex = 0;

            foreach (FieldInfo field in metadata.Fields)
            {
                field.SetValue(
                    target,
                    dependencies[dependencyIndex++]);
            }

            foreach (PropertyInfo property in metadata.Properties)
            {
                property.SetValue(
                    target,
                    dependencies[dependencyIndex++]);
            }

            foreach (MethodInjectionMetadata method in metadata.Methods)
            {
                int parameterCount =
                    method.ParameterTypes.Length;

                object[] arguments =
                    _argumentPool.Rent(parameterCount);

                try
                {
                    for (int i = 0; i < parameterCount; i++)
                    {
                        arguments[i] =
                            dependencies[dependencyIndex++];
                    }

                    method.Method.Invoke(
                        target,
                        arguments);
                }
                finally
                {
                    _argumentPool.Return(arguments);
                }
            }
        }

        private void AddPendingObject(MonoBehaviour target)
        {
            if (_pendingLookup.Add(target))
            {
                _pendingObjects.Add(target);
            }
        }

        private List<object> RentDependencyBuffer()
        {
            if (_dependencyBufferPool.Count > 0)
            {
                return _dependencyBufferPool.Pop();
            }

            return new List<object>();
        }

        private void ReturnDependencyBuffer(List<object> buffer)
        {
            buffer.Clear();
            _dependencyBufferPool.Push(buffer);
        }

        private List<MonoBehaviour> RentComponentBuffer()
        {
            if (_componentBufferPool.Count > 0)
            {
                return _componentBufferPool.Pop();
            }

            return new List<MonoBehaviour>();
        }

        private void ReturnComponentBuffer(
            List<MonoBehaviour> buffer)
        {
            buffer.Clear();
            _componentBufferPool.Push(buffer);
        }
    }
}