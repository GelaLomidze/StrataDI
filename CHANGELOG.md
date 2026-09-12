# Changelog

All notable changes to StrataDI will be documented in this file.

The format is based on Keep a Changelog and this project follows Semantic Versioning.

## [0.2.0] - 2026-09-12

### Added

- `[Inject]` property injection, including properties with private setters.
- Constructor injection for plain C# classes through `DependencyContainer.Create<T>()` and `Create(Type)`.
- Support for private constructors marked with `[Inject]`.
- Reflection metadata caching for injectable fields, properties, methods, and constructors.
- IL2CPP managed-code stripping protection by making `InjectAttribute` derive from `PreserveAttribute`.
- Automated coverage for constructor injection, property injection, IL2CPP preservation, runtime hardening, and performance optimizations.

### Changed

- Constructor injection now requires exactly one constructor marked with `[Inject]`.
- Injection order is now fields → properties → methods → `IInjectionCallback.OnInjected()`.
- Runtime injection resolves dependencies once before applying them to the target.
- Runtime hierarchy binding and injection now reuse component and dependency buffers.
- Reflection invocation argument arrays are pooled by exact length to reduce temporary allocations.
- Runtime source files are organized into `Core`, `Contexts`, and `Injection` folders.
- Runtime tests are organized into `Container`, `Injection`, and `Performance` folders.

### Fixed

- Prevented inherited `[Inject]` methods from being discovered more than once when overridden.
- Prevented nested `RetryPendingObjects()` calls from corrupting pending-injection iteration.
- Improved pending-object tracking with constant-time lookup.
- Removed avoidable allocations from pending retries and hierarchy traversal.
- Preserved `[Inject]` members in IL2CPP builds with aggressive managed stripping.

## [0.1.0] - 2026-09-08

### Added

- Hierarchical `DependencyContainer` with parent fallback.
- Scene-level `DependencyContext`.
- Optional persistent `ProjectDependencyContext`.
- `[Inject]` field injection.
- `[Inject]` method injection.
- `DependencyInstaller` binding workflow.
- `IInjectionCallback.OnInjected()` post-injection callback.
- Runtime prefab instantiation and injection helpers.
- Unity Package Manager metadata.
- Basic sample and runtime container tests.
