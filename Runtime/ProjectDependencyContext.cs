using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrataDI
{
    /// <summary>
    /// Optional persistent project-level dependency context.
    /// Its container becomes the parent of scene-level dependency containers.
    /// </summary>
    [DefaultExecutionOrder(-20000)]
    public sealed class ProjectDependencyContext : MonoBehaviour
    {
        private const string ResourcesPath =
            "StrataDI/ProjectDependencyContext";

        private static ProjectDependencyContext _instance;
        private static bool _hasTriedLoadingPrefab;

        /// <summary>
        /// Returns the project context when configured; otherwise returns null.
        /// </summary>
        public static ProjectDependencyContext Instance
        {
            get
            {
                TryGetOrCreate(out ProjectDependencyContext context);
                return context;
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

        [Header("Concrete project-level dependencies")]
        [SerializeField] private MonoBehaviour[] _projectDependencies =
            Array.Empty<MonoBehaviour>();

        [Header("Project-level installers")]
        [SerializeField] private DependencyInstaller[] _installers =
            Array.Empty<DependencyInstaller>();

        [Header("Scene injection")]
        [Tooltip(
            "When a loaded scene has no active DependencyContext, project-level " +
            "dependencies are injected after scene loading and before Start.")]
        [SerializeField] private bool _injectScenesWithoutDependencyContext = true;

        private DependencyContainer _container;
        private DependencyInjector _injector;
        private bool _isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _hasTriedLoadingPrefab = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Project context is optional. If the prefab does not exist,
            // the application continues normally.
            TryGetOrCreate(out _);
        }

        /// <summary>
        /// Tries to return the project dependency container.
        /// </summary>
        public static bool TryGetContainer(out DependencyContainer container)
        {
            if (TryGetOrCreate(out ProjectDependencyContext context))
            {
                container = context.Container;
                return true;
            }

            container = null;
            return false;
        }

        private static bool TryGetOrCreate(
            out ProjectDependencyContext context)
        {
            if (_instance != null)
            {
                context = _instance;
                return true;
            }

            // A project context may be placed directly in a scene.
            _instance = FindAnyObjectByType<ProjectDependencyContext>(
                FindObjectsInactive.Include);

            if (_instance != null)
            {
                _instance.InitializeContext();
                context = _instance;
                return true;
            }

            if (_hasTriedLoadingPrefab)
            {
                context = null;
                return false;
            }

            _hasTriedLoadingPrefab = true;

            ProjectDependencyContext prefab =
                Resources.Load<ProjectDependencyContext>(ResourcesPath);

            if (prefab == null)
            {
                context = null;
                return false;
            }

            ProjectDependencyContext createdContext =
                Instantiate(prefab);

            // Awake normally assigns _instance; retain this fallback for
            // unusual initialization states.
            if (_instance == null)
            {
                _instance = createdContext;
            }

            _instance.InitializeContext();

            context = _instance;
            return true;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
            InitializeContext();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void InitializeContext()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            _container = new DependencyContainer();
            _injector = new DependencyInjector(_container);

            _container.Bind(this);

            BindProjectDependencies();
            InstallBindings();

            _injector.InjectGameObject(gameObject);
            _injector.RetryPendingObjects();
        }

        private void BindProjectDependencies()
        {
            foreach (MonoBehaviour dependency in _projectDependencies)
            {
                if (dependency == null)
                {
                    continue;
                }

                _container.BindInstance(dependency);
            }
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

        private void OnSceneLoaded(
            Scene scene,
            LoadSceneMode _)
        {
            if (!_injectScenesWithoutDependencyContext)
            {
                return;
            }

            if (HasActiveDependencyContext(scene))
            {
                return;
            }

            DependencyInjector sceneInjector =
                new DependencyInjector(_container);

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                sceneInjector.InjectGameObject(rootObject);
            }

            sceneInjector.RetryPendingObjects();
        }

        private static bool HasActiveDependencyContext(Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                DependencyContext[] contexts =
                    rootObject.GetComponentsInChildren<DependencyContext>(true);

                foreach (DependencyContext context in contexts)
                {
                    if (context != null && context.isActiveAndEnabled)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
