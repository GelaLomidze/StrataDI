# StrataDI Manual

**Version 0.2.0**

StrataDI is a lightweight hierarchical dependency injection library for Unity. It is designed around Unity's project and scene structure: an optional persistent project container can provide shared dependencies, while each scene can use its own child container and override project-level bindings when needed.

StrataDI keeps the runtime API intentionally small and focuses on existing-instance bindings, Unity-friendly component injection, and explicit constructor injection for plain C# classes.

## Requirements

- Unity 2022.3 or newer
- No external runtime dependencies

## Installation

Open Unity Package Manager:

`Window → Package Manager → + → Add package from git URL...`

For the `0.2.0` release, use:

```text
https://github.com/GelaLomidze/StrataDI.git#v0.2.0
```

You can also add it directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gelalomidze.stratadi": "https://github.com/GelaLomidze/StrataDI.git#v0.2.0"
  }
}
```

Using a version tag is recommended so the project stays pinned to a known StrataDI release.

## Core concepts

StrataDI has three main pieces:

- `DependencyContainer` stores dependency instances and resolves them by type.
- `DependencyContext` owns a scene-level container and performs scene/component injection.
- `ProjectDependencyContext` optionally owns a persistent project-level container used as the parent of scene containers.

The hierarchy is:

```text
ProjectDependencyContext
└── Project DependencyContainer
    └── DependencyContext
        └── Scene DependencyContainer
            └── Injected scene objects
```

Resolution always checks the current container first. If the requested type is not registered locally, StrataDI falls back to the parent container.

This allows a scene binding to override a project binding simply by registering the same service type in the scene container.

## Binding dependencies

### Bind as a service type

Use `Bind<T>()` when the dependency should be resolved through an interface or base type:

```csharp
container.Bind<IAudioService>(audioService);
```

### Bind by concrete runtime type

Use `BindInstance()` when the concrete runtime type should be used as the service key:

```csharp
container.BindInstance(audioService);
```

This is equivalent to binding the instance using `audioService.GetType()`.

### Bind with a runtime `Type`

```csharp
container.Bind(typeof(IAudioService), audioService);
```

The instance must be compatible with the supplied service type.

### Rebinding

A new local binding for the same service type replaces the previous local binding.

Parent bindings are not modified. They remain available as fallback for types that are not registered in the child container.

## Resolving dependencies

Resolve a registered dependency:

```csharp
IAudioService audio =
    container.Resolve<IAudioService>();
```

Or resolve by runtime type:

```csharp
object audio =
    container.Resolve(typeof(IAudioService));
```

If the dependency does not exist in the current container or any parent container, `Resolve` throws an `InvalidOperationException`.

For non-throwing resolution:

```csharp
if (container.TryResolve<IAudioService>(
        out IAudioService audio))
{
    audio.Play("click");
}
```

You can also use:

```csharp
if (container.TryResolve(
        typeof(IAudioService),
        out object audio))
{
    // Use resolved instance.
}
```

## Installers

`DependencyInstaller` is the standard way to declare bindings for a context.

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

Assign installers to the `DependencyContext` or `ProjectDependencyContext` in the Inspector.

Installers run when their context initializes, before that context performs injection.

## Component injection

StrataDI supports `[Inject]` on instance fields, properties, and methods.

Injection is reflection-based and supports public and non-public members.

### Field injection

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

### Property injection

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
}
```

Private setters are supported.

An injectable property must:

- have a setter
- not be an indexer

StrataDI throws an `InvalidOperationException` when an `[Inject]` property does not satisfy those requirements.

### Method injection

```csharp
using StrataDI;
using UnityEngine;

public sealed class Player : MonoBehaviour
{
    private IAudioService _audioService;
    private IInputService _inputService;

    [Inject]
    private void Construct(
        IAudioService audioService,
        IInputService inputService)
    {
        _audioService = audioService;
        _inputService = inputService;
    }
}
```

Every method parameter is resolved from the container.

## Injection order

For component injection, StrataDI processes members in this order:

```text
Fields
→ Properties
→ Methods
→ IInjectionCallback.OnInjected()
```

All required field, property, and method dependencies are resolved before StrataDI applies any of them to the target.

If one dependency cannot be resolved, the component is left uninjected and can remain pending for a later retry.

## Post-injection callback

Implement `IInjectionCallback` when a component needs initialization immediately after successful dependency injection:

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
        _audioService.Play("ready");
    }
}
```

`OnInjected()` runs only after all injectable fields, properties, and methods on that component have been successfully processed.

## Inheritance

StrataDI scans injectable members across the target's class hierarchy from base classes to derived classes.

Declared `[Inject]` members in base classes are therefore supported.

Attribute discovery is performed per declared member rather than through inherited attribute lookup. This prevents an overridden method from being discovered twice merely because its base declaration has `[Inject]`.

## Constructor injection

Constructor injection is available for plain C# classes through `DependencyContainer.Create<T>()` or `Create(Type)`.

The class must have **exactly one constructor explicitly marked with `[Inject]`**.

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

Create it with:

```csharp
SaveService saveService =
    container.Create<SaveService>();
```

Private constructors are supported:

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

The following rules apply:

- the target must be a non-abstract class
- exactly one constructor must have `[Inject]`
- every constructor dependency must already be registered in the current container or a parent
- missing constructor dependencies cause `Create` to throw
- zero `[Inject]` constructors cause `Create` to throw
- multiple `[Inject]` constructors cause `Create` to throw

StrataDI does not recursively construct missing constructor dependencies.

Objects created through `Create<T>()` are returned to the caller; they are not automatically registered in the container.

## Scene dependency context

Add `DependencyContext` to a GameObject in a scene.

The context:

1. looks for an optional `ProjectDependencyContext`
2. creates a scene `DependencyContainer` using the project container as its parent
3. binds the `DependencyContext` itself
4. runs its installers
5. injects `MonoBehaviour` components in the scene
6. retries pending objects after the initial scene pass

`DependencyContext` uses an early execution order, but injected values should still not be assumed to exist inside a target component's own `Awake`.

Use `Start`, `IInjectionCallback.OnInjected()`, or another post-injection path when initialization depends on injected members.

### Accessing the scene context

```csharp
DependencyContext context =
    DependencyContext.Instance;
```

The active context's container is available through:

```csharp
DependencyContainer container =
    DependencyContext.Instance.Container;
```

StrataDI `0.2.0` supports one active scene `DependencyContext` at a time. A duplicate active context is rejected.

## Project dependency context

Project-wide dependencies are optional.

To create a persistent project context automatically, create this prefab in the consuming Unity project:

```text
Assets/Resources/StrataDI/ProjectDependencyContext.prefab
```

Add `ProjectDependencyContext` to that prefab.

The project context:

- is initialized before scene loading
- persists with `DontDestroyOnLoad`
- owns a root `DependencyContainer`
- can bind concrete project dependencies assigned in the Inspector
- runs project-level installers
- becomes the parent of scene `DependencyContext` containers

The project context itself is also bound into its container.

### Project dependencies

The `ProjectDependencyContext` Inspector can contain concrete `MonoBehaviour` dependencies.

These are bound using their concrete runtime types.

Use project-level installers when dependencies need to be registered as interfaces or base types.

### Scenes without a `DependencyContext`

By default, `ProjectDependencyContext` can inject a loaded scene when that scene has no active `DependencyContext`.

This behavior can be disabled through the project context's scene-injection setting.

When a scene has its own active `DependencyContext`, scene injection is handled by that scene context instead.

## Runtime-instantiated objects

Unity objects created after the initial scene injection are not automatically injected when using regular `Object.Instantiate`.

### Preferred prefab helper

For component prefabs:

```csharp
Enemy enemy =
    DependencyContext.Instance.Instantiate(
        enemyPrefab,
        parent);
```

There is also a position/rotation overload:

```csharp
Enemy enemy =
    DependencyContext.Instance.Instantiate(
        enemyPrefab,
        position,
        rotation,
        parent);
```

These helpers instantiate the prefab, bind `MonoBehaviour` components in the spawned hierarchy by concrete runtime type, inject the hierarchy, and retry pending objects.

### Existing GameObject

For an already-created hierarchy:

```csharp
DependencyContext.Instance
    .BindAndInjectGameObject(gameObject);
```

This binds all `MonoBehaviour` components in the hierarchy by concrete runtime type before injecting them.

### Single component

```csharp
DependencyContext.Instance
    .BindAndInjectComponent(component);
```

This binds the component by its concrete runtime type, injects it, and retries pending objects.

### Separate binding and injection

The context also exposes:

```csharp
DependencyContext.Instance
    .BindSpawnedComponents(gameObject);

DependencyContext.Instance
    .InjectGameObject(gameObject);
```

These are separate operations. `InjectGameObject` alone does not automatically bind the hierarchy first.

## Pending injection

When StrataDI cannot resolve every dependency required by a component, that component is tracked as pending instead of being partially injected.

Context operations such as `BindAndInjectGameObject` and `BindAndInjectComponent` trigger another pending retry after adding runtime bindings.

A direct call to `DependencyContainer.Bind(...)` only changes the container. It does not itself trigger a pending retry.

This distinction matters when dependencies are registered after the context's initial injection pass.

## Runtime hierarchy binding behavior

`BindAndInjectGameObject` binds each `MonoBehaviour` in the hierarchy by its concrete runtime type.

Because a `DependencyContainer` stores one instance per service type, multiple components with the same concrete runtime type will replace the previous local binding for that type during binding.

Use an installer and an explicit service type when a different binding model is required.

## IL2CPP and managed stripping

StrataDI discovers injectable members through reflection.

`InjectAttribute` derives from Unity's `PreserveAttribute`, so constructors, fields, properties, and methods explicitly marked with `[Inject]` are preserved from managed-code stripping.

Constructor injection deliberately requires an explicit `[Inject]` constructor. StrataDI does not fall back to automatically selecting a lone public constructor, because an unmarked constructor may be removed by aggressive managed stripping.

For code that is discovered only through reflection outside StrataDI's `[Inject]` members, normal Unity preservation rules still apply.

## Performance behavior

StrataDI caches reflection metadata per target type.

Runtime injection also reuses internal buffers for hierarchy traversal and dependency collection, and pools reflection argument arrays by exact parameter count.

These are implementation details rather than public API guarantees, but they reduce repeated reflection work and avoid unnecessary temporary allocations during repeated injection operations.

StrataDI is designed for Unity's normal main-thread object lifecycle. Its container and injector are not thread-safe.

## API overview

| API | Purpose |
| --- | --- |
| `DependencyContainer.Bind<T>(instance)` | Bind an instance as a service type |
| `DependencyContainer.Bind(Type, object)` | Bind using a runtime service type |
| `DependencyContainer.BindInstance(object)` | Bind by concrete runtime type |
| `DependencyContainer.Resolve<T>()` | Resolve or throw |
| `DependencyContainer.Resolve(Type)` | Resolve by runtime type or throw |
| `DependencyContainer.TryResolve<T>()` | Try to resolve without throwing for a missing binding |
| `DependencyContainer.TryResolve(Type, out object)` | Runtime-type non-throwing resolution |
| `DependencyContainer.Create<T>()` | Create a plain C# class through explicit constructor injection |
| `DependencyContainer.Create(Type)` | Runtime-type constructor injection |
| `DependencyContext.Instance` | Access the active scene context |
| `DependencyContext.Container` | Access the scene container |
| `DependencyContext.Instantiate(...)` | Instantiate, bind, and inject a component prefab |
| `DependencyContext.BindAndInjectGameObject(...)` | Bind and inject a runtime hierarchy |
| `DependencyContext.BindAndInjectComponent(...)` | Bind and inject one component |
| `DependencyContext.BindSpawnedComponents(...)` | Bind hierarchy components without injecting |
| `DependencyContext.InjectGameObject(...)` | Inject an existing hierarchy |
| `ProjectDependencyContext.Instance` | Access the optional project context |
| `ProjectDependencyContext.Container` | Access the project container |
| `ProjectDependencyContext.TryGetContainer(...)` | Try to access the project container without requiring one |
| `DependencyInstaller.InstallBindings(...)` | Declare context bindings |
| `IInjectionCallback.OnInjected()` | Run logic after successful component injection |

## Current limitations

StrataDI `0.2.0` intentionally does not provide:

- transient/scoped/singleton lifetime registrations
- factories
- recursive automatic object-graph construction
- automatic registration of objects created through `Create<T>()`
- multiple bindings or collection injection for the same service type
- automatic injection for objects created with regular `Object.Instantiate`
- multiple simultaneously active scene `DependencyContext` scopes
- automatic unbinding or disposal lifecycle
- thread-safe container access
- source-generated injection

Reflection member order inside an individual field/property/method category should not be treated as a cross-runtime ordering contract. If initialization order matters, prefer one explicit injection method or `IInjectionCallback`.

## Design goals

StrataDI focuses on:

- a small API and mental model
- predictable Unity integration
- project → scene dependency hierarchy
- support for serialized `MonoBehaviour` workflows
- explicit constructor construction for plain C# classes
- behavior that is easy to inspect and own
- low runtime overhead without introducing a large DI framework

Advanced DI features should be added when they solve a concrete Unity workflow rather than simply to mirror larger dependency injection frameworks.

## License

StrataDI is available under the MIT License. See `LICENSE.md`.
