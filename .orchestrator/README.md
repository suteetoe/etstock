# `.orchestrator/` — ETStock orchestration system

Tooling the Orchestrator uses to delegate work to sub-agent CLIs, track state across
sessions, and poll PRs for review. **No feature code lives here.**

## Layout

```
.orchestrator/
├── state.json            # machine state: backlog, PRs, schema, agent mapping, policy
├── context.md            # human context: stack, conventions, decisions (read first)
├── prompts/
│   └── agent-preamble.md  # rules injected into every delegated task
├── scripts/
│   ├── lib.ps1            # shared helpers (state, prompt builder, PR body, Record-Pr)
│   ├── delegate.ps1       # checkout → agent edits → commit → (push + PR with -Publish)
│   ├── publish.ps1        # push + gh pr create for an already-committed branch (confirm first)
│   └── review-poll.ps1    # list PRs needing review; show diff + checks for one PR
└── README.md
```

## Core principle: 1 task = 1 PR

Every delegated task ends in exactly one PR on one branch. **The scripts own all git/PR steps**
— the agent only edits in-scope files and never runs git. This guarantees a PR comes out every
time, uniformly formatted, no matter which agent ran.

## Workflow

```
requirement → split into tasks (1 task = 1 reviewable PR) → delegate.ps1
            (a: checkout  b: agent edits  c: commit)        → human confirms
            → -Publish / publish.ps1 (d: push  e: PR)        → review-poll.ps1 (diff/checks)
            → request fixes (re-delegate SAME branch, max 3 rounds) or recommend merge → human merges
```

## Quick reference

```powershell
# 1. Dry-run first to inspect the composed prompt + PR title
.\scripts\delegate.ps1 -Group backend -Task "Add Product entity + DbContext" `
    -Scope "ETStock/Models/**,ETStock/Data/**" `
    -Branch "backend/inventory-add-product-entity" -FeatureId F001 -DryRun

# 2a. Delegate, stop after local commit (default, safe — no push)
.\scripts\delegate.ps1 -Group backend -Task "Add Product entity + DbContext" `
    -Scope "ETStock/Models/**,ETStock/Data/**" `
    -Branch "backend/inventory-add-product-entity" -FeatureId F001

# 2b. After human approval: publish the committed branch (push + PR)
.\scripts\publish.ps1 -Branch "backend/inventory-add-product-entity" `
    -Title "[backend] Add Product entity + DbContext" -FeatureId F001

#    ...or run the whole pipeline in one shot (only when pre-approved):
.\scripts\delegate.ps1 -Group backend -Task "..." -Scope "ETStock/Models/**" `
    -Branch "backend/inventory-add-product-entity" -FeatureId F001 -Publish

# 3. Review
.\scripts\review-poll.ps1                       # what needs review
.\scripts\review-poll.ps1 -Number 7             # diff + checks for PR #7
.\scripts\review-poll.ps1 -Number 7 -MarkReviewed -Verdict approved
```

## Agent providers/models

Each group has a Default and a Fallback provider/model (`state.json > agents`). Fallback is
config-only — pass `-Tier fallback` to use it; nothing auto-switches.

| Group | Default | Fallback |
|---|---|---|
| lead     | `claude/opus`    | `codex/gpt-5.5` |
| backend  | `codex/gpt-5.5`  | `claude/sonnet` |
| frontend | `codex/gpt-5.5`  | `claude/sonnet` |
| qa       | `codex/gpt-5.5`  | `claude/haiku`  |

Provider → CLI: `codex` → `codex exec --full-auto -m <model>`; `claude` → `claude-acp -p ... --model <model>`.
Override per task with `delegate.ps1 -Tier fallback` or `-Provider <claude|codex> -Model <name>`.
Logins use machine credentials.

## Rules enforced

- **1 task = 1 PR** — split oversized tasks before delegating.
- Agents **never run git**; the script does checkout/commit/push/PR.
- Push + PR are gated (`-Publish` / `publish.ps1`) and require human confirmation.
- Never merge/push to `main` automatically.
- Max 3 fix rounds per PR (re-delegate the same branch), then escalate to the human.
- Branch names must match `^(frontend|backend|qa)/[a-z0-9]+-[a-z0-9-]+$`.

## Future

PR tracking is poll-based today (`review-poll.ps1`). The `state.json` PR contract
(`lastReviewedSha` / `verdict` / `rounds`) is designed so an event-driven GitHub Actions
trigger can replace the poll later without changing the review logic.
