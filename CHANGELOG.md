# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.3] - 2026-07-08

### Fixed
- Removed unreliable PLGX build step from CI to prevent hangs and failed releases.

## [1.2.2] - 2026-07-08

### Added
- Added PLGX package support: CI now builds and releases `KeePassAutoReload.plgx` alongside the DLL.
- Plugin now detects whether the installed package is `.dll` or `.plgx` and downloads the matching update asset.

### Fixed
- Fixed CI release upload failing when PLGX creation produced an empty file.

## [1.2.1] - 2026-07-07

### Added
- Added `KeePassAutoReload.Updater.exe` helper to replace the locked plugin DLL after KeePass exits.
- Added `PluginUpdater` to build updater arguments and schedule the update.

### Fixed
- Fixed in-app plugin updates failing with "being used by another process" by scheduling the external updater when the active DLL is locked.

## [1.2.0] - 2026-07-07

### Added
- Added `IUpdateClient` abstraction and async `HttpUpdateClient` for update checks and downloads.
- Added 30-second HTTP timeout to `HttpUpdateClient` to prevent UI freezes.
- Added `PluginPathResolver` so updates install to the plugin's actual assembly location instead of a hardcoded path.
- Added `SyncGuard.CanRunSync` to guard `Synchronize` against a null `MainWindow`.
- Added xUnit test project with 30 tests covering update checking, sync policy, plugin path resolution, TLS settings, timeouts, and cancellation.
- Added `.editorconfig` for consistent C# formatting and naming conventions.
- Added Dependabot configuration for GitHub Actions and NuGet dependencies.

### Changed
- Migrated `UpdateChecker` and update installation from obsolete `WebClient` to `HttpClient`.
- Removed global `ServicePointManager.SecurityProtocol` mutation; TLS 1.2/1.3 is now configured per `HttpClientHandler`.
- Updated CI workflow dependencies: `actions/checkout`, `actions/setup-dotnet`, `microsoft/setup-msbuild`, `actions/upload-artifact`, `softprops/action-gh-release`.
- Updated test dependencies: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`.

### Fixed
- Fixed `UpdateChecker.TryParseVersion` to ignore SemVer 2.0 build metadata (e.g. `+abc1234`) so `AssemblyInformationalVersion` with Source Link data compares correctly.
- Fixed `PluginPathResolver` to reject whitespace-only KeePass executable directories.

## [1.1.1] - 2026-07-07

### Added
- Added plugin about dialog.
- Added automatic update checker that queries GitHub releases shortly after KeePass starts.
- Added manual "Check for Updates" menu item.

### Changed
- Use semantic automatic updates.

## [1.0.0] - 2026-07-07

### Added
- Initial release of KeePass Auto Reload plugin.
- Periodic automatic synchronization of the active KeePass database with its source.
- Manual "Synchronize Now" menu item.
- "Skip When Database Has Unsaved Changes" option.
- Configurable sync interval.
