# Newtonsoft.Json → System.Text.Json Migration

## Strategy
Full migration — replace all Newtonsoft.Json types with System.Text.Json equivalents, bump major version.

## Preferences
- **Flow Mode**: Automatic
- **Commit Strategy**: After Each Task
- **Source branch**: `feature/memgraph-support`
- **Working branch**: `newtonsoft-to-stj-1`

## Decisions
- Full migration chosen over compatibility shim — cleaner long-term, no dual dependency
- Public API breaking change accepted — major version bump required

## Custom Instructions
<!-- Task-specific overrides: "For {taskId}: {instruction}" -->
