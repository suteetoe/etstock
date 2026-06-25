# Frontend Notes — Phase 1 (F1)

## Summary

CompanyViewModel and CompanyView implemented with full form for company data entry.

## Observable Properties

| Property | Type | Default |
|---|---|---|
| `Name` | `string` | `string.Empty` |
| `TaxId` | `string` | `string.Empty` |
| `Address` | `string` | `string.Empty` |
| `BranchName` | `string` | `string.Empty` |
| `BranchCode` | `string` | `"00000"` |
| `InvoicePrefix` | `string` | `string.Empty` |
| `VatRate` | `string` | `"7"` |
| `IsBusy` | `bool` | `false` |
| `StatusMessage` | `string` | `string.Empty` |

## Commands

- **LoadCommand** — calls `ICompanyRepository.GetAsync()`, maps all fields; converts `VatRate` from decimal fraction to percentage string via `(vatRate * 100).ToString("0.##")`
- **SaveCommand** — parses `VatRate` string to decimal, divides by 100, calls `ICompanyRepository.UpsertAsync(company)`

## QA Hints

- Test `LoadAsync` populates all 7 fields (Name, TaxId, Address, BranchName, BranchCode, InvoicePrefix, VatRate) from a mocked `ICompanyRepository.GetAsync()` result
- Test `SaveAsync` correctly converts `VatRate` string (e.g. `"7"`) to decimal `/100` (i.e. `0.07m`) before calling `UpsertAsync`
- Test `IsBusy` is set to `false` in the `finally` block — verify it returns to `false` even when an exception is thrown
- Design-time constructor (parameterless `CompanyViewModel()`) must not crash — `_repository` is null and all commands guard with `if (_repository is null) return`
