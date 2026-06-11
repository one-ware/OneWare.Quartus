# AGENTS.md

## Project Overview

`OneWare.Quartus` is a **plugin/extension for [OneWare Studio](https://github.com/one-ware/OneWare)**, an open-source IDE for FPGA/embedded development. This extension adds support for the **Intel/Altera Quartus toolchain**, enabling compilation and programming (download) of designs for Altera FPGAs from inside OneWare Studio.

The extension integrates with OneWare's `UniversalFpgaProjectSystem` by registering a **toolchain** (compile flow) and a **loader** (device programming flow), plus UI extensions and settings.

## Tech Stack

- **Language:** C# (`net10.0`, nullable enabled, implicit usings enabled)
- **UI:** Avalonia (XAML `.axaml` views + MVVM view models), compiled bindings enabled
- **DI:** `Microsoft.Extensions.DependencyInjection` via OneWare's module system
- **Tests:** xUnit (`Microsoft.NET.Test.Sdk`)
- **Plugin model:** `EnableDynamicLoading` — built as a dynamically loaded plugin assembly

### Key external dependencies (provided by the host IDE, do NOT bundle)
- `OneWare.Essentials` — services (`ILogger`, `ISettingsService`, `IWindowService`, `IChildProcessService`, `IEnvironmentService`, `IOutputService`, `IMainDockService`), helpers (`PlatformHelper`), enums, base classes.
- `OneWare.UniversalFpgaProjectSystem` — `UniversalFpgaProjectRoot`, `FpgaModel`, `FpgaService`, `IFpgaToolchain`, `IFpgaLoader`, `FpgaSettingsParser`.

Both are referenced with `Private="false"` and `ExcludeAssets="runtime;Native"` because the host IDE supplies them at runtime. **Never copy these assemblies into the output or bump them carelessly** — version compatibility with the host is tracked in `oneware-extension.json` (`minStudioVersion`).

## Repository Layout

```
src/OneWare.Quartus/
  QuartusModule.cs        # Plugin entry point: registers services, toolchain, loader, UI extensions, settings
  QuartusToolchain.cs     # IFpgaToolchain: writes .qsf, runs compile flow
  QuartusLoader.cs        # IFpgaLoader: programs the device (short-/long-term, quartus_pgm / quartus_cpf)
  Services/
    QuartusService.cs     # Runs `quartus_sh --flow compile`, streams output to OutputService
  Helper/                 # .qsf (Quartus Settings File) parsing/manipulation + settings control adapters
    QsfFile.cs, QsfHelper.cs, IQsfSetting.cs, QsfSettingComboBox.cs, QsfSettingSlider.cs
  ViewModels/             # Avalonia MVVM view models (settings + toolbar window extensions)
  Views/                  # Avalonia .axaml views + code-behind
  Assets/                 # Icons (AvaloniaResource)
tests/OneWare.Quartus.UnitTests/
  OneWareQuartusTests.cs  # xUnit tests
oneware-extension.json    # Extension manifest: versions + minStudioVersion mapping
```

## Build, Test & Run

Always use the .NET CLI from the repo root.

```bash
# Restore (may require workloads for Avalonia)
dotnet workload restore
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Build the publishable plugin (Release) — mirrors CI publish
dotnet build src/OneWare.Quartus/OneWare.Quartus.csproj -c Release -o publish
```

- CI (`.github/workflows/test.yml`) runs restore → build → test on every push/PR to `main`.
- CI (`.github/workflows/publish.yml`) builds Release on release/`workflow_dispatch`, zips the `publish` folder and attaches `compatibility.txt`.
- The `GenerateCompatibilityFile` MSBuild target emits `compatibility.txt` listing host-provided package versions — used by OneWare to validate compatibility. Keep it intact.

> Note: there is no host IDE in this repo, so the plugin cannot be "run" standalone here. Validate changes via `dotnet build` and `dotnet test`. Manual integration testing requires loading the built plugin into OneWare Studio with Quartus installed.

## How It Works (key flows)

- **Module init (`QuartusModule.Initialize`)**: registers `QuartusToolchain` and `QuartusLoader` on `FpgaService`, registers UI extensions by name (`CompileWindow_TopRightExtension`, `UniversalFpgaToolBar_DownloaderConfigurationExtension`), and registers a `Quartus_Path` settings entry under **Tools**. On valid path it exports `Quartus_Bin` / `Quartus_Bin64` to `IEnvironmentService` so the `quartus_*` executables are on PATH.
- **Compile (`QuartusToolchain.CompileAsync` → `QuartusService`)**: updates the project `.qsf` (family, device, top-level entity, included files), then runs `quartus_sh --flow compile <top>`.
- **Program (`QuartusLoader.DownloadAsync`)**: locates `.sof`/`.pof` in the output dir, supports short-term (volatile) and long-term programming, optionally converting via `quartus_cpf`, then runs `quartus_pgm`.
- **`.qsf` handling**: all Quartus Settings File reading/writing goes through `Helper/QsfHelper` + `QsfFile`. Reuse these helpers rather than parsing `.qsf` manually.
- **Per-FPGA settings keys** (read via `FpgaSettingsParser.LoadSettings`): e.g. `quartusToolchainFamily`, `quartusToolchainDevice`, `quartusProgrammerShortTermMode/Operation/Arguments`, `quartusProgrammerLongTermFormat/Mode/Operation/Arguments/CpfArguments`.

## Conventions

- **C# style**: nullable reference types enabled; use primary constructors for services/toolchains/loaders (see `QuartusToolchain`, `QuartusLoader`, `QuartusService`); resolve dependencies via constructor injection, not the service locator, except where the existing module code uses `serviceProvider.Resolve<T>()` / `ContainerLocator`.
- **Logging & errors**: wrap external/IO operations in try/catch and log via `ILogger` (`logger.Error(...)`, `logger.Warning(...)`). Compile/program failures should be reported, not thrown to the UI.
- **External tools**: invoke Quartus binaries (`quartus_sh`, `quartus_pgm`, `quartus_cpf`) through `IChildProcessService.ExecuteShellAsync`. Never hardcode absolute tool paths — rely on the PATH entries set from `Quartus_Path`.
- **MVVM**: view models go in `ViewModels/`, derive from OneWare base view-model classes; views are `.axaml` + code-behind in `Views/`. Keep logic in view models.
- **Cross-platform**: use `PlatformHelper.Platform` and `PlatformHelper.ExecutableExtension` for platform-specific paths/executables (Windows + Linux x64/Arm64 are supported).

## Releasing / Versioning

- Bump `<Version>` in `src/OneWare.Quartus/OneWare.Quartus.csproj`.
- Add a matching entry to `oneware-extension.json` `versions[]` with the correct `minStudioVersion`.
- Publish runs off a GitHub release tag (version comes from the csproj).

## Do / Don't

- ✅ Reuse `QsfHelper`/`QsfFile` for any `.qsf` access.
- ✅ Add unit tests in `tests/OneWare.Quartus.UnitTests` and validate with `dotnet test`.
- ✅ Keep host packages (`OneWare.Essentials`, `OneWare.UniversalFpgaProjectSystem`) as `Private="false"`.
- ❌ Don't bundle host-provided assemblies or change their copy/runtime behavior.
- ❌ Don't hardcode Quartus install paths or assume a single OS.
- ❌ Don't edit generated artifacts (`bin/`, `obj/`, `compatibility.txt`).

