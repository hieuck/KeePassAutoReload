# KeePass Auto Reload

KeePass 2.x plugin that periodically synchronizes the active database with its current source.

This is intended for setups where KeePass opens a remote database through a provider such as `dropbox://`, while another client such as KeePassXC edits the synced local file. The plugin asks KeePass to run its own synchronize/merge pipeline on a timer, so remote changes are pulled into the open KeePass session.

## Usage

1. Download `KeePassAutoReload.dll` from the latest GitHub release.
2. Copy `KeePassAutoReload.dll` into the KeePass `Plugins` folder.
3. Restart KeePass.
4. Open the database.
5. Use **Tools -> KeePass Auto Reload -> Enable Auto Sync**.

The plugin also provides **Synchronize Now** for a manual sync.

By default, automatic sync is skipped when the active KeePass database has unsaved local changes. Use **Sync Even When Modified** only if you accept KeePass running a merge while the local database is dirty.
