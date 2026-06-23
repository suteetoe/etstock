# Sub-agent task contract (ETStock)

You are a specialized sub-agent working on the **ETStock** repo (.NET 10 + Avalonia UI,
PostgreSQL via EF Core + Npgsql planned). An orchestrator delegated this task to you.

## Non-negotiable rules

1. **Scope:** Only modify files matching the ALLOWED SCOPE listed in the task. Do NOT touch
   any file outside it. If the task cannot be done within scope, stop and explain why instead
   of editing out-of-scope files.
2. **DO NOT run git. At all.** No `git checkout`, `git add`, `git commit`, `git push`, no
   branch creation, no PR. The orchestrator script owns every git and PR operation — you are
   already on the correct branch, and the script will commit your changes and open exactly one
   PR for this task. Your only job is to edit the in-scope files correctly. (If you commit,
   you will create duplicate/empty commits and break the 1-task-=-1-PR guarantee.)
3. **No secrets:** Never hard-code connection strings, tokens, or passwords.

## Coding conventions

- MVVM via CommunityToolkit.Mvvm: `[ObservableProperty]`, `[RelayCommand]`, derive VMs from
  `ViewModelBase`. Keep code-behind minimal.
- Compiled bindings are ON - set `x:DataType` on every view / data template.
- `Nullable` is enabled - honor nullability; avoid `!` unless justified.
- EF Core (if in scope): async APIs only, `DbContext` via DI (scoped), parameterized queries,
  migrations under `ETStock/Data/Migrations`.
- Tests (if in scope): xUnit; Avalonia UI tests use `Avalonia.Headless`.

## Before you finish

- Build must succeed: `dotnet build ETStock.slnx`.
- If you added/changed logic and a test project exists, run `dotnet test`.
- Leave the files edited and the build green. Do NOT commit. Summarize what you changed and
  any follow-ups in your final message so the orchestrator can write the PR body.
