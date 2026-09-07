# StrataDI Manual

StrataDI is a small hierarchical dependency injection library for Unity.

## Resolution model

A scene `DependencyContext` creates a local `DependencyContainer`. If an optional
`ProjectDependencyContext` exists, its container becomes the scene container's parent.
Resolution always checks the local container first and then walks to the parent.

## Injection timing

StrataDI injects scene objects during context initialization. A persistent project context
can also inject scenes that do not contain a `DependencyContext` when Unity reports that
the scene has loaded. Design injected components so they require dependencies in `Start`
or later, not in `Awake`.

## Project context prefab

To use project-wide dependencies, create this prefab in your game project:

`Assets/Resources/StrataDI/ProjectDependencyContext.prefab`

Add `ProjectDependencyContext` and configure project dependencies/installers in the Inspector.
The prefab is optional; scenes can use `DependencyContext` without it.

## Current limitations

- Registered values are existing instances; StrataDI does not create transient/scoped/singleton lifetimes.
- Constructor and property injection are not supported.
- Only one active `DependencyContext` is currently supported at a time; multiple additive scene contexts are not yet supported.
- Runtime objects created with regular `Object.Instantiate` are not automatically injected. Use `DependencyContext.Instantiate` or `BindAndInjectGameObject`.
