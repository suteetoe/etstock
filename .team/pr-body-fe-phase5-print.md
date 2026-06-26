## Summary

- `PrintPreviewViewModel`: wraps `InvoiceDocumentModel`, exposes `CloseCommand` and `PrintCommand` (generates HTML to temp file, opens system browser print dialog)
- `PrintPreviewWindow.axaml`: shows company header, invoice header, items table, totals
- `InvoiceViewModel`: new `IInvoicePrintService?` field + 2-param ctor + `PrintPreviewRequested` event + `PrintAsync` command
- `InvoiceView.axaml.cs`: subscribes to `PrintPreviewRequested`, opens `PrintPreviewWindow` as dialog
- `InvoiceView.axaml`: Print button added to each invoice row
- `MainWindowViewModel`: resolves `IInvoicePrintService` from DI, passes it to `InvoiceViewModel`
- Build: 0 errors, 0 warnings

## Merge order

Merge PR #15 (BE) first, then this PR.

## Test plan

- [ ] dotnet build passes
- [ ] Click Print on an invoice row — PrintPreviewWindow opens showing company + invoice data
- [ ] Click Print button in preview — system browser opens with formatted HTML for printing
- [ ] Click Close — window closes
- [ ] QA PR covers InvoicePrintService and PrintPreviewViewModel tests

Generated with Claude Code
