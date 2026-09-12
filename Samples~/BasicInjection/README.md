# Basic Injection Sample

This sample demonstrates the core StrataDI `0.2.0` workflow:

- installer-based bindings
- field injection
- property injection with a private setter
- method injection
- `IInjectionCallback.OnInjected()`
- explicit constructor injection for a plain C# class

## Scene setup

1. Create a GameObject named `Game Clock`.
2. Add `GameClock` to it.
3. Create another GameObject named `Scene Dependencies`.
4. Add `DependencyContext` to `Scene Dependencies`.
5. Add `GameInstaller` to the same GameObject.
6. Assign the `Game Clock` object's `GameClock` component to the installer's `_gameClock` field.
7. Add `GameInstaller` to the `DependencyContext` installer list.

The installer registers `GameClock` as `IGameClock`:

```csharp
container.Bind<IGameClock>(_gameClock);
```

## Component injection examples

Add any of these components to GameObjects in the scene.

### `ClockDisplay`

Demonstrates private field injection:

```csharp
[Inject]
private IGameClock _gameClock;
```

### `PropertyClockDisplay`

Demonstrates property injection with a private setter:

```csharp
[Inject]
public IGameClock GameClock
{
    get;
    private set;
}
```

### `MethodClockDisplay`

Demonstrates private method injection:

```csharp
[Inject]
private void Construct(
    IGameClock gameClock)
{
    _gameClock = gameClock;
}
```

All three examples implement `IInjectionCallback`. Their `OnInjected()` methods run only after their dependencies have been successfully injected.

For component injection, StrataDI processes:

```text
Fields
→ Properties
→ Methods
→ IInjectionCallback.OnInjected()
```

## Constructor injection example

Add `ConstructorInjectionExample` to a GameObject in the scene.

The component receives the active `DependencyContext` and then creates the plain C# `ClockReader` through the container:

```csharp
ClockReader reader =
    _context.Container.Create<ClockReader>();
```

`ClockReader` has exactly one constructor marked with `[Inject]`:

```csharp
[Inject]
private ClockReader(
    IGameClock gameClock)
{
    _gameClock = gameClock;
}
```

Private `[Inject]` constructors are supported.

Constructor dependencies must already be registered in the current container or one of its parent containers. StrataDI does not recursively construct missing dependencies.

## Expected result

Enter Play Mode.

Each example component that you added should write a message to the Console showing the current clock time, for example:

```text
Field injection: 0.15
Property injection: 0.15
Method injection: 0.15
Constructor injection: 0.15
```

Exact time values will vary.

## Injection timing

Do not depend on injected values inside `Awake`.

Use `Start`, `IInjectionCallback.OnInjected()`, or another post-injection path when initialization requires injected dependencies.
