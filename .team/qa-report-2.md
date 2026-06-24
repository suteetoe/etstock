# QA Report — Phase 2 Product & Stock

## Verdict

**PASS**

All required QA coverage exists, the full xUnit suite passes, and no production
code was edited.

## Execution Summary

- QA agent: Codex fallback because the configured Gemini CLI was not
  authenticated.
- Build:
  - Command: `dotnet build ETStock.Tests/ETStock.Tests.csproj --artifacts-path
    <temp> --no-restore -p:UsedAvaloniaProducts=`
  - Result: **PASS**
  - Errors: 0
  - Warnings: 2 (`NU1900`; NuGet vulnerability data was unavailable because the
    sandbox could not reach `https://api.nuget.org/v3/index.json`).
- Tests:
  - Command: `dotnet test ETStock.Tests/ETStock.Tests.csproj --artifacts-path
    <temp> --no-build -p:UsedAvaloniaProducts=`
  - Result: **PASS**
  - Total: 33
  - Passed: 33
  - Failed: 0
  - Skipped: 0

The temporary artifacts path was required because the sandbox denied writes to
the repository's existing `obj` cache. `UsedAvaloniaProducts` was cleared for
the verification process because Avalonia BuildServices otherwise attempted to
write telemetry under a denied user-profile path.

## Required Coverage

| Requirement | Result |
|---|---|
| Closing formula: positive, zero, negative | PASS |
| Duplicate product code `AddAsync` result | PASS |
| Deterministic duplicate message in `ProductViewModel` | PASS |
| Blank required fields rejected | PASS |
| Negative prices rejected | PASS |
| Live closing update for all four editable quantities | PASS |
| Monthly stock insert followed by update of the same period row | PASS |

Detailed test-to-criterion mapping is in `.team/qa-plan-2.md`.

## Issues

- No production defects found in the required Phase 2 coverage.
- No failed or skipped tests.

## Risks

- EF Core InMemory does not reproduce PostgreSQL collation, constraint timing,
  or provider-specific unique-index race behavior. The deterministic
  pre-existing duplicate path is covered; the PostgreSQL exception-conversion
  race path is not integration-tested here.
- This scope validates view-model behavior and property notifications, not
  rendered Avalonia controls or compiled-binding behavior.
