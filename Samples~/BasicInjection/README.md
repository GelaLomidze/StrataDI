# Basic Injection Sample

1. Create a GameObject named `Scene Dependencies`.
2. Add `DependencyContext`.
3. Add `GameInstaller` to the same GameObject.
4. Create a GameObject with `GameClock` and assign it to the installer's field.
5. Add the installer to the `DependencyContext` installer list.
6. Add `ClockDisplay` to any GameObject in the scene.
7. Enter Play Mode. `ClockDisplay.OnInjected()` logs after its dependency is injected.
