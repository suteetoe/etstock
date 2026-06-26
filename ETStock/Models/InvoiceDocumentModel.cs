namespace ETStock.Models;

public record InvoiceDocumentLine(
    string ProductName,
    decimal Qty,
    decimal Amount,
    decimal VatAmount);

public record InvoiceDocumentModel(
    string CompanyName,
    string CompanyTaxId,
    string CompanyAddress,
    string CompanyBranch,
    string CompanyBranchCode,
    string InvoiceNo,
    DateTime InvoiceDate,
    int TaxYear,
    int TaxMonth,
    IReadOnlyList<InvoiceDocumentLine> Lines,
    decimal SubTotal,
    decimal VatTotal,
    decimal GrandTotal);
