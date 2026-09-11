# StrataDI

**Lightweight hierarchical dependency injection for Unity.**

StrataDI is a small dependency injection library designed around Unity's project and scene structure. It provides project-level and scene-level containers, parent fallback, installer-based bindings, reflection-based component injection, and explicit constructor injection for plain C# classes.

StrataDI is intended for Unity projects that want dependency injection without adopting a large dependency-management framework.

## Features

- Lightweight instance-based dependency container
- Parent → child container hierarchy
- Optional persistent project-level dependencies
- Scene-level dependency contexts
- `[Inject]` field injection
- `[Inject]` property injection, including private setters
- `[Inject]` method injection
- Explicit `[Inject]` constructor injection for plain C# classes
- Installer-based bindings
- Post-injection callback via `IInjectionCallback`
- Runtime GameObject/prefab binding and injection helpers
- Reflection metadata caching
- Reduced temporary allocations through reusable buffers and pooled argument arrays
- Managed-code stripping protection for `[Inject]` members
- No external runtime dependencies

## Requirements

- Unity 2022.3 or newer

## Installation

### Unity Package Manager

Open:

`Window → Package Manager → + → Add package from git URL...`

For the stable `0.2.0` release, use:

```text
https://github.com/GelaLomidze/StrataDI.git#v0.2.0
```

You can also add the package directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gelalomidze.stratadi": "https://github.com/GelaLomidze/StrataDI.git#v0.2.0"
  }
}
```

Using a release tag is recommended so your project stays on a known StrataDI version.

## Quick start

### 1. Create a dependency

```csharp
public interface IAudioService
{
    void Play(string id);
}
```

```csharp
using UnityEngine;

public sealed class AudioService : MonoBehaviour, IAudioService
{
    public void Play(string id)
    {
        Debug.Log($"Play: {id}");
    }
}
```

### 2. Bind it with an installer

```csharp
using StrataDI;
using UnityEngine;

public sealed class GameInstaller : DependencyInstaller
{
    [SerializeField]
    private AudioService _audioService;

    public override void InstallBindings(
        DependencyContainer container)
    {
        container.Bind<IAudioService>(_audioService);
    }
}
```

Add `DependencyContext` to a GameObject in the scene and assign `GameInstaller` to its installer list.

### 3. Inject it into a component

#### Field injection

```csharp
using StrataDI;
using UnityEngine;

public sealed class Player : MonoBehaviour
{
    [Inject]
    private IAudioService _audioService;

    private void Start()
    {
        _audioService.Play("spawn");
    }
}
```

#### Property injection

```csharp
using StrataDI;
using UnityEngine;

public sealed class Player : MonoBehaviour
{
    [Inject]
    public IAudioService AudioService
    {
        get;
        private set;
    }

    private void Start()
    {
        AudioService.Play("spawn");
    }
}
```

#### Method injection

```csharp
using StrataDI;
using UnityEngine;

public sealed class Player : MonoBehaviour
{
    private IAudioService _audioService;

    [Inject]
    private void Construct(
        IAudioService audioService)
    {
        _audioService = audioService;
    }
}
```

### 4. Optional post-injection callback

```csharp
using StrataDI;
using UnityEngine;

public sealed class Player :
    MonoBehaviour,
    IInjectionCallback
{
    [Inject]
    private IAudioService _audioService;

    public void OnInjected()
    {
        // Fields, properties, and methods have already been injected.
    }
}
```

For component injection, StrataDI applies injection in this order:

```text
Fields
→ Properties
→ Methods
→ IInjectionCallback.OnInjected()
```

StrataDI resolves all required dependencies before applying them to the target. If a dependency is not available yet, the component can remain pending and be retried later when context APIs trigger another retry.

## Constructor injection

`DependencyContainer.Create<T>()` can create plain C# classes through constructor injection.

Constructor injection is explicit: the target class must have exactly one constructor marked with `[Inject]`.

```csharp
using StrataDI;

public sealed class SaveService
{
    private readonly IStorage _storage;

    [Inject]
    public SaveService(
        IStorage storage)
    {
        _storage = storage;
    }
}
```

Create it from a container:

```csharp
SaveService saveService =
    container.Create<SaveService>();
```

Private constructors are also supported:

```csharp
public sealed class SaveService
{
    private readonly IStorage _storage;

    [Inject]
    private SaveService(
        IStorage storage)
    {
        _storage = storage;
    }
}
```

Constructor dependencies must already be registered in the current container or one of its parents.

StrataDI does not recursively construct missing dependencies and does not automatically register objects created through `Create<T>()`.

## Project and scene hierarchy

```text
ProjectDependencyContext
└── Project DependencyContainer
    └── DependencyContext
        └── Scene DependencyContainer
            └── Injected scene objects
```

When a dependency is requested, the scene container checks itself first. If the type is not registered locally, resolution falls back to the parent project container.

This allows a scene to override a project-level binding by registering the same service type locally.

## Project-wide dependencies

Project-wide dependencies are optional.

To enable them, create:

```text
Assets/Resources/StrataDI/ProjectDependencyContext.prefab
```

Add `ProjectDependencyContext` to the prefab and configure project dependencies and installers in the Inspector.

The project context is created before scene loading and persists through `DontDestroyOnLoad`.

If no project context prefab exists, a scene `DependencyContext` works as a standalone container.

A project context can also inject a loaded scene that does not contain an active `DependencyContext`.

## Runtime-instantiated objects

Objects created after initial scene injection must be injected explicitly.

Prefer:

```csharp
Enemy enemy =
    DependencyContext.Instance.Instantiate(
        enemyPrefab,
        parent);
```

or:

```csharp
GameObject instance =
    Instantiate(prefab);

DependencyContext.Instance
    .BindAndInjectGameObject(instance);
```

`BindAndInjectGameObject` traverses the hierarchy once, binds its `MonoBehaviour` components by concrete runtime type, then injects those components.

You can also bind and inject a single component:

```csharp
DependencyContext.Instance
    .BindAndInjectComponent(component);
```

## Binding APIs

Bind a concrete type:

```csharp
container.Bind(audioService);
```

Bind an implementation as an interface or base type:

```csharp
container.Bind<IAudioService>(
    audioService);
```

Bind an object using its concrete runtime type:

```csharp
container.BindInstance(audioService);
```

Resolve directly:

```csharp
IAudioService audio =
    container.Resolve<IAudioService>();
```

Resolve safely:

```csharp
if (container.TryResolve<IAudioService>(
        out IAudioService audio))
{
    audio.Play("click");
}
```

A local binding replaces any previous local binding for the same service type. Parent bindings remain available as fallback when a service is not registered locally.

## IL2CPP and managed stripping

StrataDI discovers injectable members through reflection.

`InjectAttribute` derives from Unity's `PreserveAttribute`, so constructors, fields, properties, and methods marked with `[Inject]` are protected from managed-code stripping.

This is also why constructor injection requires an explicit `[Inject]` constructor rather than relying on automatic public-constructor discovery.

StrataDI `0.2.0` has been verified with a Windows IL2CPP player using High managed stripping for:

- private field injection
- property injection
- private method injection
- private constructor injection

## Injection timing

Do not rely on injected component dependencies inside `Awake`.

`DependencyContext` initializes early and performs scene injection before normal gameplay initialization such as `Start`, but Unity may invoke a component's own `Awake` before StrataDI injects it.

Use `Start`, `IInjectionCallback.OnInjected()`, or another post-injection path when initialization depends on injected values.

## Current limitations

StrataDI intentionally keeps its API small. Version `0.2.0` does not currently provide:

- transient/scoped/singleton lifetime registrations
- factories
- recursive automatic object-graph construction
- automatic registration of objects created through `Create<T>()`
- multiple bindings or collection injection for the same service type
- automatic injection for objects instantiated through regular `Object.Instantiate`
- multiple simultaneously active scene `DependencyContext` scopes
- automatic disposal or unbinding lifecycle
- thread-safe container access
- source-generated injection

StrataDI currently supports one active scene `DependencyContext` at a time.

## Design goals

StrataDI focuses on:

- a small public API
- predictable Unity integration
- project → scene dependency hierarchy
- support for serialized `MonoBehaviour` workflows
- explicit behavior that is easy to inspect and own
- low runtime overhead without introducing a large framework

Advanced DI features should be added only when they solve a concrete Unity workflow.

## License

StrataDI is available under the MIT License. See [LICENSE.md](LICENSE.md).
