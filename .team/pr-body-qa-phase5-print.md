## Summary

- `InvoicePrintServiceTests` (7 tests): company field mapping, invoice header mapping, line item mapping, totals computation, null-product fallback name, null-company fallback to default Company, InvalidOperationException on missing invoice
- `PrintPreviewViewModelTests` (7 tests): constructor sets Doc, CloseCommand raises CloseRequested, BuildHtml contains company name / invoice number / tax ID / product line / VAT total / grand total
- Total: 79/79 tests pass (65 pre-existing + 14 new)

## Merge order

Merge PR #15 (BE) first, then PR #16 (FE), then this PR.

## DoD checklist

- [x] InvoicePrintService covered: company fields, invoice header, lines, totals, edge cases (null product, null company, not-found)
- [x] PrintPreviewViewModel covered: doc init, CloseCommand event, BuildHtml HTML content
- [x] 79/79 tests pass

Generated with Claude Code
