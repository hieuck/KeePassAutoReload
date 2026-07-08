# KeePass Auto Reload

KeePass 2.x plugin that periodically synchronizes the active database with its current source.

This is intended for setups where KeePass opens a remote database through a provider such as `dropbox://`, while another client such as KeePassXC edits the synced local file. The plugin asks KeePass to run its own synchronize/merge pipeline on a timer, so remote changes are pulled into the open KeePass session.

## Usage

1. Download `KeePassAutoReload.dll` from the latest GitHub release.
2. Copy `KeePassAutoReload.dll` into the KeePass `Plugins` folder (or any subfolder KeePass is configured to load plugins from).
3. Restart KeePass.
4. Open the database.
5. Use **Tools -> KeePass Auto Reload -> Enable Auto Sync**.

The plugin also provides **Synchronize Now** for a manual sync.

By default, automatic sync is skipped when the active KeePass database has unsaved local changes. Use **Sync Even When Modified** only if you accept KeePass running a merge while the local database is dirty.

The plugin automatically checks GitHub releases shortly after KeePass starts. Use **Tools -> KeePass Auto Reload -> Check for Updates** to check manually. If a newer version tag is available, the plugin downloads and installs the release asset, then asks you to restart KeePass.

## Auto-Update Behavior

The plugin can check GitHub releases and install updates automatically.

- Update checks and downloads time out after 30 seconds to avoid blocking KeePass.
- Release assets are verified against `SHA256SUMS.txt` before installation.
- When an update is installed, the plugin tries to replace its own DLL file. Because Windows locks a loaded DLL, the replacement often cannot happen while KeePass is running.
- If the DLL is locked, the plugin saves the new file next to the current one with a `.new` extension and launches `KeePassAutoReload.Updater.exe` to perform the replacement after KeePass exits.

### Updater EXE

If you want in-place updates to complete without manual intervention, place `KeePassAutoReload.Updater.exe` in the same folder as `KeePassAutoReload.dll` (the KeePass `Plugins` folder). When an update is downloaded:

1. The plugin writes the new DLL as `KeePassAutoReload.dll.new`.
2. The updater is started with the KeePass process ID, the `.new` path, the current DLL path, and the KeePass executable path.
3. The updater waits for KeePass to exit.
4. The updater copies `KeePassAutoReload.dll.new` over `KeePassAutoReload.dll`.
5. The updater deletes the `.new` file.
6. The updater restarts KeePass.

You do not need the updater EXE for the plugin to function; it is only required for fully automatic in-place updates.

## Requirements & Behavior

- KeePass 2.x on Windows.
- Outgoing HTTPS connections to GitHub use TLS 1.2 or newer.
- When an update is installed, the plugin replaces its own DLL file wherever KeePass loaded it from. If the plugin file cannot be replaced while KeePass is running, the downloaded file is saved next to it with a `.new` extension.
