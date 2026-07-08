# Contributing to KeePass Auto Reload

Thank you for your interest in improving KeePass Auto Reload!

## How to Report Bugs

1. Check the [existing issues](https://github.com/hieuck/KeePassAutoReload/issues) to avoid duplicates.
2. Open a new issue using the **Bug report** template.
3. Include KeePass version, Windows version, plugin version, and clear reproduction steps.
4. Attach logs or screenshots if they help explain the problem.

## How to Propose Features

1. Open a new issue using the **Feature request** template.
2. Describe the use case and the behavior you expect.
3. Wait for maintainer feedback before investing significant implementation time.

## Development Workflow

This repository uses isolated git worktrees. Feature and fix branches live under `.worktrees/<branch-name>` and are merged through pull requests.

1. Create a new worktree from `main`:
   ```powershell
   git worktree add .worktrees/feature-<name> -b feature/<name>
   cd .worktrees/feature-<name>
   ```
2. Make focused, minimal changes.
3. Ensure `dotnet test tests/KeePassAutoReload.Tests.csproj` passes locally.
4. Commit using [Conventional Commits](https://www.conventionalcommits.org/):
   - `feat:` for new features
   - `fix:` for bug fixes
   - `refactor:` for code restructuring
   - `test:` for test-only changes
   - `docs:` for documentation changes
   - `ci:` for CI changes
5. Push the branch and open a pull request.
6. Wait for CI to pass before merging.

## Code Conventions

- Target .NET Framework 4.8 for the plugin and updater projects.
- Keep the unit test project (`tests/KeePassAutoReload.Tests.csproj`) passing on .NET 8.
- Avoid adding new external dependencies unless absolutely necessary.
- Prefer small, focused changes over large refactoring PRs.

## Testing

Run the test suite with:

```powershell
dotnet test tests/KeePassAutoReload.Tests.csproj
```

All changes should include tests when possible. The local plugin build (`msbuild src/KeePassAutoReload.csproj`) requires the .NET Framework 4.8 targeting pack; CI is the authoritative build environment if the targeting pack is not installed locally.

## Code of Conduct

Be respectful and constructive. Harassment or abusive behavior will not be tolerated.
