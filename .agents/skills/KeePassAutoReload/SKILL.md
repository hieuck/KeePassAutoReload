```markdown
# KeePassAutoReload Development Patterns

> Auto-generated skill from repository analysis

## Overview
This skill teaches the core development patterns and conventions used in the KeePassAutoReload C# codebase. You'll learn how to structure files, write and organize code, follow commit message conventions, and run or write tests. The repository focuses on automating the reload of KeePass databases, with a clean and maintainable code style.

## Coding Conventions

### File Naming
- Use **PascalCase** for all file names.
  - Example: `AutoReloadManager.cs`, `DatabaseWatcher.cs`

### Imports
- Use **relative import paths** for referencing other files within the project.
  - Example:
    ```csharp
    using KeePassAutoReload.Helpers;
    ```

### Exports
- Use **named exports** (i.e., explicitly declare classes, interfaces, etc.).
  - Example:
    ```csharp
    public class AutoReloadManager
    {
        // ...
    }
    ```

### Commit Messages
- Follow **conventional commit** format.
- Use prefixes like `chore` and `refactor`.
- Keep commit messages concise (around 57 characters).
  - Example:
    ```
    chore: update dependencies for security patches
    refactor: extract reload logic into separate class
    ```

## Workflows

### Refactoring Code
**Trigger:** When improving code structure or readability without changing functionality  
**Command:** `/refactor`

1. Identify code that can be improved (e.g., duplicated logic, unclear structure).
2. Update code, ensuring no change in external behavior.
3. Use PascalCase for new/renamed files.
4. Use relative imports for new dependencies.
5. Commit with a message like:  
   `refactor: extract watcher logic to DatabaseWatcher.cs`

### Dependency Maintenance
**Trigger:** When updating or adding dependencies  
**Command:** `/chore-deps`

1. Update dependency references in the project.
2. Test the application to ensure compatibility.
3. Commit with a message like:  
   `chore: update dependency XYZ to v1.2.3`

### Writing Tests
**Trigger:** When adding new features or fixing bugs  
**Command:** `/add-test`

1. Create a new test file matching the pattern `*.test.*` (e.g., `AutoReloadManager.test.cs`).
2. Write tests for the relevant feature or bug fix.
3. Use named exports for test classes.
4. Run tests to verify correctness.
5. Commit with a message like:  
   `chore: add tests for auto-reload functionality`

## Testing Patterns

- Test files follow the `*.test.*` naming convention (e.g., `Feature.test.cs`).
- The testing framework is **unknown**; check existing test files for structure.
- Place tests alongside implementation or in a dedicated test directory.
- Use named classes for test cases.
  - Example:
    ```csharp
    public class AutoReloadManagerTests
    {
        // Test methods here
    }
    ```

## Commands
| Command      | Purpose                                        |
|--------------|------------------------------------------------|
| /refactor    | Refactor code for clarity or maintainability   |
| /chore-deps  | Update or add dependencies                     |
| /add-test    | Add or update tests for new/existing features  |
```