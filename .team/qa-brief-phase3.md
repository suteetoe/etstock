# QA Agent Brief - Phase 3 Monthly Transaction

## Scope

Allowed files:

- `ETStock.Tests/**`
- `.team/qa-report-phase3.md`
- product code only for minimal compile fixes discovered by tests

## Required Coverage

- Repository carry-forward chain across normal month and year boundary.
- Save then reload monthly transaction values.
- Import/carry-forward behavior does not overwrite current buy/sale values.
- ViewModel load/save/carry-forward behavior if a testable ViewModel surface exists.
- Regression: `dotnet test ETStock.slnx` must pass.

## Output

Write `.team/qa-report-phase3.md` with:

- verdict PASS or FAIL
- tests added/updated
- command run and result
- risks or gaps

Do not push or open PR.
