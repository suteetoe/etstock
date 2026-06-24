# ETStock — Orchestrator Shared Context

> Persistent context for the Orchestrator across sessions. Read this first when a new
> session starts. `state.json` holds machine-trackable state (backlog, PRs, schema);
> this file holds human-readable conventions and decisions.

## Project snapshot

- **App type:** .NET 10 Desktop, Avalonia UI (`WinExe`)
- **Repo:** `suteetoe/etstock` (https://github.com/suteetoe/etstock.git), default branch `main`
- **Solution:** `ETStock.slnx` → `ETStock/ETStock.csproj`
- **Current state:** fresh Avalonia MVVM template. `App.axaml`, `ViewLocator`, `MainWindow` + `MainWindowViewModel`. `Models/` folder exists but empty.

### Stack (current vs. planned)

| Layer | Status | Notes |
|---|---|---|
| UI | ✅ in repo | Avalonia 11.3.11 (Desktop, Fluent theme, Inter fonts), compiled bindings ON |
| MVVM | ✅ in repo | CommunityToolkit.Mvvm 8.2.1 (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`) |
| Data | ⏳ planned | EF Core + Npgsql → PostgreSQL. **Not yet added.** |
| Tests | ⏳ planned | xUnit + Avalonia.Headless. **No test project yet.** |

## Agent groups & provider/model mapping

Each group has a **Default** and a **Fallback** provider/model. Fallback is **config-only** —
nothing auto-switches to it; pass `delegate.ps1 -Tier fallback` (or `-Provider/-Model`) to use it.

| Group | Scope | Default | Fallback |
|---|---|---|---|
| **lead** | orchestration / planning (this role) | `claude/opus` | `codex/gpt-5.5` |
| **backend** (BE) | `ETStock/Models/**`, `ETStock/Data/**`, `ETStock/Services/**`, EF Core / Npgsql / business logic | `codex/gpt-5.5` | `claude/sonnet` |
| **frontend** (FE) | `ETStock/Views/**`, `ETStock/ViewModels/**`, `*.axaml`, `ViewLocator.cs`, `App.axaml*` | `codex/gpt-5.5` | `claude/sonnet` |
| **qa** | `ETStock.Tests/**`, test fixtures | `codex/gpt-5.5` | `claude/haiku` |

**Provider → CLI** (headless): `codex` → `codex exec --full-auto -m <model> "<prompt>"` ·
`claude` → `claude-acp -p "<prompt>" --model <model>`.

> Agents **edit files only** — they never run git. `delegate.ps1` commits/pushes/opens the PR.

> Mapping lives in `state.json > agents` (nested `default`/`fallback`). Override per-task via
> `delegate.ps1 -Tier fallback` or `-Provider <claude|codex> -Model <name>`.

## Coding conventions (agreed)

- **MVVM:** Use CommunityToolkit.Mvvm source generators — `[ObservableProperty]` for bindable
  fields, `[RelayCommand]` for commands. ViewModels derive from `ViewModelBase`. No code-behind
  logic beyond view wiring.
- **Bindings:** Compiled bindings are ON by default (`AvaloniaUseCompiledBindingsByDefault=true`).
  Always set `x:DataType` on views/data templates so bindings compile.
- **Nullable:** `<Nullable>enable</Nullable>` — respect nullability, no `!` unless justified.
- **EF Core (when added):** async APIs (`ToListAsync`, `SaveChangesAsync`); `DbContext` via DI,
  scoped lifetime; migrations checked into `ETStock/Data/Migrations`; never call `.Result`/`.Wait()`.
- **Npgsql:** connection string from config/secrets, never hard-coded; parameterized queries only.
- **Tests:** xUnit; Avalonia UI tests use `Avalonia.Headless`. Each feature PR includes tests.
- **Branches:** `<group>/<feature>-<desc>`, e.g. `backend/inventory-add-product-entity`.
- **Commits:** imperative, concise subject; reference feature id from backlog when present.

## Hard rules (orchestrator)

1. **1 task = 1 PR, always.** Every delegated task ends in exactly one PR on one branch. Split
   tasks too big to review into multiple sub-tasks (= multiple PRs) before delegating.
2. **Scripts own all git/PR steps — not the agents.** The agent only edits in-scope files. The
   `delegate.ps1` script does `checkout → (agent) → add+commit → push → PR` deterministically,
   so a PR is guaranteed and uniformly formatted regardless of the agent.
3. Never merge or push to `main`. Summarize "PR ready, recommend merge" and let the human merge.
4. Push + `gh pr create` (steps d+e) are gated behind `-Publish` / `publish.ps1` and require
   human confirmation first.
5. Max **3** fix rounds per PR (re-delegate on the SAME branch/PR). If still failing, stop and
   ask the human.
6. Ask before any repo-affecting command (push, branch delete, `gh pr merge`).
7. Credentials (gh token, agent logins) come from the machine — never request tokens.

## Decision log

- 2026-06-23: Set up orchestration system. Installed `gh` 2.95.0 (winget), `codex` 0.142.0,
  `gemini` 0.47.0 (npm). `antigravity` 1.107.0 already present (IDE; uses `chat` subcommand).
  Agent mapping: frontend→antigravity, backend→codex, qa→gemini.
- 2026-06-23: PR tracking starts as a **poll** model (`review-poll.ps1`); event-driven GitHub
  Actions deferred.
- 2026-06-24: QA agent switched from `gemini` to `claude-acp` (provider claude-acp, model `haiku`).
- 2026-06-24: Reworked agent mapping to **Default + Fallback (provider/model)** per group and added
  a **lead** group. Default→Fallback: lead `claude/opus`→`codex/gpt-5.5`; backend & frontend
  `codex/gpt-5.5`→`claude/sonnet`; qa `codex/gpt-5.5`→`claude/haiku`. Dropped `gemini`/`antigravity`.
  Providers: `claude`→`claude-acp`, `codex`→`codex`. Fallback is config-only (no auto-switch);
  `delegate.ps1` uses the group's default unless `-Tier fallback` / `-Provider`/`-Model` is passed.
  Touched `state.json` (agents nested + branchPattern +`lead`), `lib.ps1` (`Get-AgentMapping`,
  `Get-ProviderCli`, `Build-PrBody`), `delegate.ps1`, `publish.ps1`.
