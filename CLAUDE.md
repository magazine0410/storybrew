# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

storybrew is an osu! storyboard editor: users write C# effect scripts, and the editor recompiles and re-renders them live whenever a script or asset file is saved. This checkout is a fork (`origin` = magazine0410, `upstream` = Damnae) that is a Linux-native version of storybrew. Windows support isn't a goal: the upstream version already runs on Windows.

## Commands

`brewlib/` is a git submodule (a separate repository, also forked). Run `git submodule update --init` after cloning.

```bash
dotnet build editor/editor.csproj                  # builds brewlib, common and editor
./editor/bin/Debug/net10.0/StorybrewEditor         # run (Linux); logs go to editor/bin/Debug/net10.0/logs/
dotnet test test/test.csproj                       # all tests (MSTest)
dotnet test test/test.csproj --filter "FullyQualifiedName~CommandTest.TestBooleanCommandsInLoop"   # single test
```

- `TestBooleanCommandsInLoop` fails on the original upstream code too; it isn't a regression.
- Windows code left over from upstream isn't maintained or tested: the `net10.0-windows` target with WinForms in `editor/editor.csproj` and the `#if WINDOWS` / `OperatingSystem.IsWindows()` branches in shared files. They're kept to limit conflicts with upstream, but the Windows build no longer compiles (e.g. `FormsClipboard` was removed). Don't spend effort keeping Windows working.
- Windows-only files were deleted: the self-updater (`Updater.cs`, `UpdateMenu.cs`), the release packager (`Builder.cs` and its `Build` post-build step), `editor/Util/Native.cs`, `FormsClipboard.cs`, `bass.dll`/`bass_fx.dll` and the `OpenTK.dll.config` files.
- Common scripts come from the repository's `scripts/` folder (`Project.cs` looks four levels up from `editor/bin/<configuration>/<framework>/`). Projects are created in `editor/bin/Debug/net10.0/projects/`, and relative paths in effects resolve from the project's folder.

## Projects

- **brewlib** (submodule, `BrewLib.*`): the engine. It has OpenTK 3 windowing and OpenGL rendering, BASS audio (through ManagedBass, with native `bass.dll`/`libbass.so` in the submodule root), the retained-mode UI widget toolkit and skinning, screen layers, input, and utilities (`PathHelper`, `ClipboardHelper`, `Native`).
- **common** (`StorybrewCommon.dll`): the public API that user scripts compile against. It contains `StoryboardObjectGenerator` (the base class for effects), `OsbSprite`/`OsbAnimation` and their commands, mapset/beatmap parsing, curves, 3D storyboarding, and subtitle/font generation. Changes to public types here can break users' existing scripts.
- **editor** (`StorybrewEditor`): the application. It covers the startup and main loop (`Program.cs`), screens (`ScreenLayers/`), project load/save/export (`Storyboarding/Project.cs`), script compilation and hot reload (`Scripting/`), and Windows/Linux helpers (`Util/`).
- **scripts**: the sample effects shipped with the editor, the same ones users see as "common scripts".
- **test**: MSTest tests for common's storyboard command logic.

## How scripts run

- `ScriptManager` watches `.cs` files with `FileSystemWatcher`.
- `ScriptCompiler` compiles them in memory with Roslyn. It references `Project.DefaultAssemblies`: the .NET reference assemblies (from `Project.GetRuntimeRefDirectory()`, which needs the .NET SDK installed), `SkiaSharp`, `OpenTK`, and `StorybrewCommon`.
- `ScriptContainer` loads each compiled assembly into a collectible `AssemblyLoadContext`, so a script can be unloaded and reloaded.
- Effects run on background threads (`AsyncActionQueue`), write their sprites into editor layers, and are re-rendered by `EditorOsbSprite`.
- Textures and samples are loaded lazily through `TextureContainerSeparate`/`AudioSampleContainer`. They are reloaded when the asset watcher sees a file change.
- A project only shows what its effects generate: a new project is black even if the map has a storyboard. The `ImportOsb` script shows an existing `.osb`.
- To run effects without the UI (e.g. a test program referencing `editor.csproj`), `Program`'s main thread id, `Settings` and `AudioManager` must be set and scheduled tasks pumped with `Program.RunScheduledTasks`. The effect queue is also only enabled by `Project.Draw`; enable `effectUpdateQueue` directly.
- Settings, logs, `cache/` and `scripts/` are resolved relative to the working directory. On Linux, `Program.Main` sets the working directory to the application folder.

## Linux port status

- **Platform code:** file dialogs use kdialog or zenity (`editor/Util/LinuxDialogs.cs`), the clipboard uses wl-clipboard, xclip or xsel (`brewlib/Util/ClipboardHelper.cs`), native calls are guarded, and OpenTK library names are mapped (`OpenTkNativeLibraries`). Mapset files are found regardless of case and backslashes (`PathHelper.FindFileIgnoringCase`), and osu! is found in Wine installs (`OsuHelper`).
- **Imaging is SkiaSharp**, replacing System.Drawing (GDI+), which throws on non-Windows. Don't add System.Drawing.Common back; only its cross-platform types (`Color`, `Rectangle`, … from System.Drawing.Primitives) are used.
  - Images decode through `BitmapLoader` as BGRA that isn't premultiplied (what GDI+ gave). `TextureOptions.WithPixels` converts to the premultiplication a texture wants before upload.
  - Text goes through `SkiaText` (brewlib): font sizes are points at 96 dpi like GDI+, and characters missing from a font are drawn with a system fallback font.
  - Script compatibility: `GetMapsetBitmap` returns `StorybrewCommon.Scripting.Bitmap` (`Width`, `Height`, `Size`, `GetPixel`), and `StorybrewCommon.Subtitles.FontStyle`/`WrapMode` keep the System.Drawing values, so scripts using `var`, `Bitmap` or those enums still compile. Custom `FontEffect` implementations must be rewritten for the SkiaSharp signature (`FontText.Draw` draws the text).
  - Font caches record `Renderer: SkiaSharp`, so textures generated with GDI+ are regenerated.
- **OpenTK 3 on X11:** setting `window.Location` or `Size` to its current value hangs forever, because it waits for a ConfigureNotify event that never comes. Only assign these when the value changes.
- **Keep OpenTK 3:** don't upgrade to OpenTK 4. It moves the math types (`Vector2`, `Color4`, …) that every user script uses.

## Merging upstream

Merge (don't rebase) `upstream/master`, in `brewlib/` first, then in storybrew, resolving the brewlib pointer conflict by pointing at the merged brewlib commit. Upstream changes to the deleted Windows-only files show up as modify/delete conflicts: resolve them by keeping the files deleted (`git rm <file>`). Port new Windows-only code to the Linux equivalents (SkiaSharp, `LinuxDialogs`, `ClipboardHelper`); new WinForms or System.Drawing.Common usage fails to compile, other Windows-only APIs raise CA1416 warnings, and new `DllImport`s need checking by hand.

## Conventions

- Match the existing style: private fields and methods in camelCase, interfaces without an `I` prefix (e.g. `ClipboardBackend`, `FontEffect`), and `var` everywhere. `Nullable` is disabled.
- Commits that change brewlib need two steps: commit and push in `brewlib/` first, then commit the updated submodule reference in storybrew.
