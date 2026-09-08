using System;
using System.Collections.Generic;
using System.Reflection;

namespace StrataDI
{
    /// <summary>
    /// Stores dependency instances and resolves them by type.
    /// If a dependency is not registered locally, resolution falls back to the parent container.
    /// </summary>
    public sealed class DependencyContainer
    {
        private readonly Dictionary<Type, object> _instances = new();
        private readonly DependencyContainer _parent;

        public DependencyContainer(DependencyContainer parent = null)
        {
            _parent = parent;
        }

        /// <summary>
        /// Parent container used as a fallback during resolution.
        /// </summary>
        public DependencyContainer Parent => _parent;

        /// <summary>
        /// Creates a plain C# object using constructor injection.
        /// Constructor dependencies must already be registered
        /// in this container or one of its parents.
        /// </summary>
        public T Create<T>() where T : class
        {
            return (T)Create(typeof(T));
        }

        /// <summary>
        /// Creates a plain C# object using constructor injection.
        /// Constructor dependencies must already be registered
        /// in this container or one of its parents.
        /// </summary>
        public object Create(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsClass)
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName} cannot be created because " +
                    "constructor injection only supports classes.");
            }

            if (type.IsAbstract)
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName} cannot be created because it is abstract.");
            }

            ConstructorInfo constructor = GetInjectionConstructor(type);

            ParameterInfo[] parameters = constructor.GetParameters();
            object[] arguments = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                Type dependencyType = parameters[i].ParameterType;

                if (!TryResolve(dependencyType, out object dependency))
                {
                    throw new InvalidOperationException(
                        $"Cannot create {type.FullName}. " +
                        $"Constructor dependency {dependencyType.FullName} " +
                        "is not registered in this dependency container " +
                        "or any parent container.");
                }

                arguments[i] = dependency;
            }

            return constructor.Invoke(arguments);
        }

        private static ConstructorInfo GetInjectionConstructor(Type type)
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
                $"Mark the constructor StrataDI should use with " +
                $"[{nameof(InjectAttribute)}].");
        }

        /// <summary>
        /// Binds an instance as <typeparamref name="T"/>.
        /// Use this overload to bind an implementation to an interface or base type.
        /// </summary>
        public void Bind<T>(T instance)
        {
            Bind(typeof(T), instance);
        }

        /// <summary>
        /// Binds an instance to the specified service type.
        /// Existing bindings for the same type are replaced.
        /// </summary>
        public void Bind(Type type, object instance)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!type.IsInstanceOfType(instance))
            {
                throw new ArgumentException(
                    $"Instance of type {instance.GetType().FullName} cannot be bound as {type.FullName}.",
                    nameof(instance));
            }

            _instances[type] = instance;
        }

        /// <summary>
        /// Binds an instance using its concrete runtime type.
        /// </summary>
        public void BindInstance(object instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Bind(instance.GetType(), instance);
        }

        /// <summary>
        /// Resolves a dependency by type.
        /// </summary>
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        /// <summary>
        /// Resolves a dependency by type.
        /// </summary>
        public object Resolve(Type type)
        {
            if (TryResolve(type, out object instance))
            {
                return instance;
            }

            throw new InvalidOperationException(
                $"Type {type.FullName} is not registered in this dependency container " +
                "or any parent container.");
        }

        /// <summary>
        /// Attempts to resolve a dependency without throwing when it is missing.
        /// </summary>
        public bool TryResolve<T>(out T result)
        {
            if (TryResolve(typeof(T), out object instance))
            {
                result = (T)instance;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Attempts to resolve a dependency without throwing when it is missing.
        /// </summary>
        public bool TryResolve(Type type, out object instance)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_instances.TryGetValue(type, out instance))
            {
                return true;
            }

            if (_parent != null)
            {
                return _parent.TryResolve(type, out instance);
            }

            instance = null;
            return false;
        }
    }
}
