# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

storybrew is an osu! storyboard editor: users write C# effect scripts, and the editor recompiles and re-renders them live whenever a script or asset file is saved. This checkout is a fork (`origin` = magazine0410, `upstream` = Damnae) that is being ported to run natively on Linux.

## Commands

`brewlib/` is a git submodule (a separate repository, also forked). Run `git submodule update --init` after cloning.

```bash
dotnet build editor/editor.csproj                  # builds brewlib, common and editor
./editor/bin/Debug/net10.0/StorybrewEditor         # run (Linux); logs go to editor/bin/Debug/net10.0/logs/
dotnet test test/test.csproj                       # all tests (MSTest)
dotnet test test/test.csproj --filter "FullyQualifiedName~CommandTest.TestBooleanCommandsInLoop"   # single test
```

- `TestBooleanCommandsInLoop` fails on the original upstream code too; it isn't a regression.
- The editor's target framework depends on the OS doing the build: `net10.0-windows` (with WinForms) on Windows, and plain `net10.0` elsewhere. Windows-only code is wrapped in `#if WINDOWS`. To check that the Windows version still compiles on Linux, build a copy of the repo with the Linux `TargetFramework` line in `editor/editor.csproj` changed to `net10.0-windows` (`EnableWindowsTargeting` is already set). Don't override `TargetFramework` on the command line; restore breaks.
- The `Build` configuration packages a Windows release zip (`Builder.cs`), and only on Windows.

## Projects

- **brewlib** (submodule, `BrewLib.*`): the engine. It has OpenTK 3 windowing and OpenGL rendering, BASS audio (through ManagedBass, with native `bass.dll`/`libbass.so` in the submodule root), the retained-mode UI widget toolkit and skinning, screen layers, input, and utilities (`PathHelper`, `ClipboardHelper`, `Native`).
- **common** (`StorybrewCommon.dll`): the public API that user scripts compile against. It contains `StoryboardObjectGenerator` (the base class for effects), `OsbSprite`/`OsbAnimation` and their commands, mapset/beatmap parsing, curves, 3D storyboarding, and subtitle/font generation. Changes to public types here can break users' existing scripts.
- **editor** (`StorybrewEditor`): the application. It covers the startup and main loop (`Program.cs`), screens (`ScreenLayers/`), project load/save/export (`Storyboarding/Project.cs`), script compilation and hot reload (`Scripting/`), and Windows/Linux helpers (`Util/`).
- **scripts**: the sample effects shipped with the editor, the same ones users see as "common scripts".
- **test**: MSTest tests for common's storyboard command logic.

## How scripts run

- `ScriptManager` watches `.cs` files with `FileSystemWatcher`.
- `ScriptCompiler` compiles them in memory with Roslyn. It references `Project.DefaultAssemblies`: the .NET reference assemblies (from `Project.GetRuntimeRefDirectory()`, which needs the .NET SDK installed), `System.Drawing.Common`, `OpenTK`, and `StorybrewCommon`.
- `ScriptContainer` loads each compiled assembly into a collectible `AssemblyLoadContext`, so a script can be unloaded and reloaded.
- Effects run on background threads (`AsyncActionQueue`), write their sprites into editor layers, and are re-rendered by `EditorOsbSprite`.
- Textures and samples are loaded lazily through `TextureContainerSeparate`/`AudioSampleContainer`. They are reloaded when the asset watcher sees a file change.
- Settings, logs, `cache/` and `scripts/` are resolved relative to the working directory. On Linux, `Program.Main` sets the working directory to the application folder.

## Linux port status

- **Done:** WinForms removed from the Linux build. File dialogs use kdialog or zenity (`editor/Util/LinuxDialogs.cs`), the clipboard uses wl-clipboard, xclip or xsel (`brewlib/Util/ClipboardHelper.cs`), native calls are guarded, and OpenTK library names are mapped (`OpenTkNativeLibraries`). Mapset files are found regardless of case and backslashes (`PathHelper.FindFileIgnoringCase`), and osu! is found in Wine installs (`OsuHelper`).
- **Remaining:** replace System.Drawing (GDI+), which throws on non-Windows. It is used for texture loading (`Texture2d`), UI text (`TextGenerator`), and subtitle/font generation plus `BitmapHelper` in common. The editor currently crashes at startup in `TextGenerator`. The ~560 CA1416 build warnings mark the call sites. `FontEffect`, `FontDescription.FontStyle` and `GetMapsetBitmap` expose System.Drawing types to user scripts.
- **OpenTK 3 on X11:** setting `window.Location` or `Size` to its current value hangs forever, because it waits for a ConfigureNotify event that never comes. Only assign these when the value changes.
- **Keep OpenTK 3:** don't upgrade to OpenTK 4. It moves the math types (`Vector2`, `Color4`, …) that every user script uses.

## Conventions

- Match the existing style: private fields and methods in camelCase, interfaces without an `I` prefix (e.g. `ClipboardBackend`, `FontEffect`), and `var` everywhere. `Nullable` is disabled.
- Commits that change brewlib need two steps: commit and push in `brewlib/` first, then commit the updated submodule reference in storybrew.
