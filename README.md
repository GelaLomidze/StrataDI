# StrataDI

**Lightweight hierarchical dependency injection for Unity.**

StrataDI provides a small instance-based DI container designed around Unity's project and scene structure. A scene container can inherit from an optional persistent project container, so dependencies resolve locally first and fall back to the parent when needed.

## Features

- Lightweight instance-based dependency container
- Parent → child container hierarchy
- Optional persistent project-level dependencies
- Scene-level dependency contexts
- `[Inject]` field injection
- `[Inject]` method injection
- Installer-based bindings
- Post-injection callback via `IInjectionCallback`
- Runtime GameObject/prefab injection helpers
- No external runtime dependencies

## Requirements

- Unity 2022.3 or newer

## Installation

### Git URL

After this repository is published, add its Git URL through Unity Package Manager:

`Window → Package Manager → + → Add package from git URL...`

Then enter your repository URL, for example:

```text
https://github.com/GelaLomidze/StrataDI.git
```

You can also add it directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gelalomidze.stratadi": "https://github.com/GelaLomidze/StrataDI.git"
  }
}
```

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
    [SerializeField] private AudioService _audioService;

    public override void InstallBindings(DependencyContainer container)
    {
        container.Bind<IAudioService>(_audioService);
    }
}
```

Add `DependencyContext` to a GameObject in the scene and assign `GameInstaller` to its installer list.

### 3. Inject it

Field injection:

```csharp
using StrataDI;
using UnityEngine;

public sealed class Player : MonoBehaviour
{
    [Inject] private IAudioService _audioService;

    private void Start()
    {
        _audioService.Play("spawn");
    }
}
```

Method injection:

```csharp
using StrataDI;
using UnityEngine;

public sealed class Player : MonoBehaviour
{
    private IAudioService _audioService;

    [Inject]
    private void Construct(IAudioService audioService)
    {
        _audioService = audioService;
    }
}
```

### 4. Optional post-injection callback

```csharp
using StrataDI;
using UnityEngine;

public sealed class Player : MonoBehaviour, IInjectionCallback
{
    [Inject] private IAudioService _audioService;

    public void OnInjected()
    {
        // All [Inject] fields and methods are resolved before this callback.
    }
}
```

## Project and scene hierarchy

```text
ProjectDependencyContext
└── Project DependencyContainer
    └── DependencyContext
        └── Scene DependencyContainer
            └── Injected scene objects
```

When a dependency is requested, the scene container checks itself first. If it cannot resolve the type, it falls back to the project container.

This means a scene can override a project binding simply by registering the same service type locally.

## Project-wide dependencies

Project-wide dependencies are optional.

To enable them, create:

```text
Assets/Resources/StrataDI/ProjectDependencyContext.prefab
```

Add `ProjectDependencyContext` to the prefab and configure project dependencies/installers. The context is created before scene loading and persists with `DontDestroyOnLoad`.

If no project context prefab exists, a scene `DependencyContext` works as a standalone container.

## Runtime-instantiated objects

Objects created after initial scene injection need to be injected explicitly. Prefer:

```csharp
Enemy enemy = DependencyContext.Instance.Instantiate(enemyPrefab, parent);
```

or:

```csharp
GameObject instance = Instantiate(prefab);
DependencyContext.Instance.BindAndInjectGameObject(instance);
```

`BindAndInjectGameObject` binds MonoBehaviours on the spawned hierarchy by their concrete types and then performs injection.

## Binding APIs

Bind a concrete type:

```csharp
container.Bind(audioService);
```

Bind an implementation as an interface/base type:

```csharp
container.Bind<IAudioService>(audioService);
```

Resolve directly when needed:

```csharp
IAudioService audio = container.Resolve<IAudioService>();
```

Or safely:

```csharp
if (container.TryResolve<IAudioService>(out IAudioService audio))
{
    audio.Play("click");
}
```

## Injection timing

Do not rely on injected dependencies inside `Awake`.

Scene injection is intended to make dependencies available before normal gameplay initialization such as `Start`. The project context can also inject a scene after it has loaded when that scene has no active `DependencyContext`.

## Design goals

StrataDI intentionally focuses on a small API and predictable Unity integration. Version `0.1.x` does **not** include:

- transient/scoped/singleton lifetime registration
- automatic constructor injection
- property injection
- factories or automatic object construction
- multiple active additive-scene dependency contexts

Those features should only be added when they solve a concrete Unity workflow rather than to imitate larger DI frameworks.

## License

StrataDI is available under the MIT License. See [LICENSE.md](LICENSE.md).
