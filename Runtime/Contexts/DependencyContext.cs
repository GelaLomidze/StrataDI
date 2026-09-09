using System;
using UnityEngine;

namespace StrataDI
{
    /// <summary>
    /// Scene-level dependency context.
    /// Creates a container whose parent is the optional
    /// project-level container.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class DependencyContext : MonoBehaviour
    {
        private static DependencyContext _instance;

        /// <summary>
        /// Returns the active scene dependency context.
        /// </summary>
        public static DependencyContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance =
                        FindAnyObjectByType<DependencyContext>(
                            FindObjectsInactive.Include);

                    if (_instance == null)
                    {
                        throw new InvalidOperationException(
                            "DependencyContext was not found in the scene. " +
                            "Create a GameObject with DependencyContext before " +
                            "using DependencyContext.Instance.");
                    }

                    _instance.InitializeContext();
                }

                return _instance;
            }
        }

        public DependencyContainer Container
        {
            get
            {
                InitializeContext();
                return _container;
            }
        }

        [SerializeField]
        private DependencyInstaller[] _installers =
            Array.Empty<DependencyInstaller>();

        private DependencyContainer _container;
        private DependencyInjector _injector;
        private bool _isInitialized;

        private void Awake()
        {
            if (_instance != null &&
                _instance != this)
            {
                Debug.LogError(
                    "StrataDI currently supports one active " +
                    "DependencyContext at a time. " +
                    $"Destroying duplicate context on '{gameObject.name}'.",
                    this);

                Destroy(gameObject);
                return;
            }

            _instance = this;
            InitializeContext();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void InitializeContext()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            ProjectDependencyContext.TryGetContainer(
                out DependencyContainer projectContainer);

            _container =
                new DependencyContainer(projectContainer);

            _injector =
                new DependencyInjector(_container);

            _container.Bind(this);

            InstallBindings();
            InjectSceneObjects();
        }

        private void InstallBindings()
        {
            foreach (DependencyInstaller installer in _installers)
            {
                if (installer == null)
                {
                    continue;
                }

                installer.InstallBindings(_container);
            }
        }

        private void InjectSceneObjects()
        {
            foreach (GameObject rootObject
                     in gameObject.scene.GetRootGameObjects())
            {
                _injector.InjectGameObject(rootObject);
            }

            _injector.RetryPendingObjects();
        }

        /// <summary>
        /// Instantiates a component prefab, binds all MonoBehaviours
        /// on the spawned hierarchy by concrete type,
        /// and injects the hierarchy.
        /// </summary>
        public new T Instantiate<T>(
            T prefab,
            Transform parent = null)
            where T : Component
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            T instance = UnityEngine.Object.Instantiate(prefab, parent);
            BindAndInjectGameObject(instance.gameObject);

            return instance;
        }

        /// <summary>
        /// Instantiates a component prefab, binds all MonoBehaviours
        /// on the spawned hierarchy by concrete type,
        /// and injects the hierarchy.
        /// </summary>
        public new T Instantiate<T>(
            T prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
            where T : Component
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            T instance =
                UnityEngine.Object.Instantiate(
                    prefab,
                    position,
                    rotation,
                    parent);

            BindAndInjectGameObject(
                instance.gameObject);

            return instance;
        }

        public void BindAndInjectGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            _injector.BindAndInjectGameObject(gameObject);
            _injector.RetryPendingObjects();
        }

        public void BindAndInjectComponent(Component component)
        {
            if (component == null)
            {
                return;
            }

            _container.BindInstance(component);

            _injector.TryInjectAndNotify(
                component as MonoBehaviour);

            _injector.RetryPendingObjects();
        }

        public void BindSpawnedComponents(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            _injector.BindGameObject(gameObject);
        }

        public void InjectGameObject(GameObject gameObject)
        {
            _injector.InjectGameObject(gameObject);
        }
    }
}