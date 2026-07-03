using ETStock.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ETStock.Services;

/// <summary>
/// QuestPDF document for ใบเสร็จรับเงิน / ใบกำกับภาษี (อย่างย่อ) — A5 portrait.
/// </summary>
internal sealed class InvoicePdfDocument : IDocument
{
    private readonly InvoiceDocumentModel _doc;

    private const string ThaiFont = "TH Sarabun New";
    private const float BodySize  = 10f;

    static InvoicePdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public InvoicePdfDocument(InvoiceDocumentModel doc) => _doc = doc;

    public DocumentMetadata GetMetadata() =>
        new() { Title = $"ใบกำกับภาษีอย่างย่อ {_doc.InvoiceNo}" };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(10, Unit.Millimetre);
            page.DefaultTextStyle(t => t.FontFamily(ThaiFont).FontSize(BodySize));

            page.Content().Column(col =>
            {
                col.Spacing(3);
                ComposeHeader(col);
                ComposeTitle(col);
                ComposeRefNumbers(col);
                ComposeItemTable(col);
                ComposeSignature(col);
            });
        });
    }

    // ─── Sections ─────────────────────────────────────────────────────────

    private void ComposeHeader(ColumnDescriptor col)
    {
        string branch = string.IsNullOrWhiteSpace(_doc.CompanyBranch)
            ? "" : $" ({_doc.CompanyBranch})";

        col.Item().Text($"{_doc.CompanyName}{branch}").Bold().FontSize(12);

        if (!string.IsNullOrWhiteSpace(_doc.CompanyAddress))
            col.Item().Text(_doc.CompanyAddress).FontSize(9.5f);

        if (!string.IsNullOrWhiteSpace(_doc.CompanyPhone))
            col.Item().Text($"โทร. {_doc.CompanyPhone}").FontSize(9.5f);

        col.Item().Text($"เลขประจำตัวผู้เสียภาษี {_doc.CompanyTaxId}").FontSize(9.5f);

       
    }

    private static void ComposeTitle(ColumnDescriptor col)
    {
        col.Item().PaddingTop(4).AlignCenter()
            .Text("ใบเสร็จรับเงิน / ใบกำกับภาษี (อย่างย่อ)").Bold().FontSize(13);
        col.Item().AlignCenter().Text("TAX INVOICE").FontSize(10);
    }

    private void ComposeRefNumbers(ColumnDescriptor col)
    {
        string bookNo  = _doc.BookNo?.ToString()              ?? "";
        string runNo   = _doc.RunningNo?.ToString("D5")       ?? _doc.InvoiceNo;
        string dateStr = ThaiDate(_doc.InvoiceDate);

        col.Item().PaddingTop(2).Text($"เล่มที่  {bookNo}");
        col.Item().Row(row =>
        {
            row.RelativeItem().Text($"เลขที่  {runNo}");
            row.AutoItem().Text($"วันที่  {dateStr}");
        });
    }

    private void ComposeItemTable(ColumnDescriptor col)
    {
        // Static cell-style helpers (no capture → safe as static locals)
        static IContainer HeaderCell(IContainer c) =>
            c.Background("#C8C8C8")
             .Border(1).BorderColor("#555555")
             .DefaultTextStyle(t => t.Bold().FontSize(9.5f))
             .Padding(3);

        static IContainer DataCell(IContainer c) =>
            c.BorderLeft(1).BorderRight(1).BorderColor("#888888")
             .Padding(3)
             .MinHeight(14);

        static IContainer FooterCell(IContainer c) =>
            c.BorderLeft(1).BorderRight(1).BorderTop(1).BorderBottom(1).BorderColor("#888888")
             .Padding(3)
             .MinHeight(14);

        col.Item().PaddingTop(4).Table(table =>
        {
            // Column widths — A5 usable ≈ 397pt (148 mm @ 72dpi → minus margins)
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(22);   // ลำดับ
                cols.RelativeColumn();      // รายการสินค้า
                cols.ConstantColumn(36);   // จำนวน
                cols.ConstantColumn(56);   // ราคาต่อหน่วย
                cols.ConstantColumn(56);   // จำนวนเงิน
            });

            // ── Header ──────────────────────────────────────────────────
            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).AlignCenter().Text("ลำดับ");
                h.Cell().Element(HeaderCell).Text("รายการสินค้า");
                h.Cell().Element(HeaderCell).AlignRight().Text("จำนวน");
                h.Cell().Element(HeaderCell).AlignRight().Text("ราคาต่อหน่วย");
                h.Cell().Element(HeaderCell).AlignRight().Text("จำนวนเงิน");
            });

            // ── Data rows ────────────────────────────────────────────────
            int seq = 1;
            foreach (var line in _doc.Lines)
            {
                table.Cell().Element(DataCell).AlignCenter().Text(seq++.ToString()).FontSize(9.5f);
                table.Cell().Element(DataCell).Text(line.ProductName).FontSize(9.5f);
                table.Cell().Element(DataCell).AlignRight().Text(line.Qty.ToString("N0")).FontSize(9.5f);
                table.Cell().Element(DataCell).AlignRight().Text(line.UnitPrice.ToString("N2")).FontSize(9.5f);
                table.Cell().Element(DataCell).AlignRight().Text(line.LineTotal.ToString("N2")).FontSize(9.5f);
            }

            // ── Empty rows (pad to minimum 8 rows) ───────────────────────
            int empties = Math.Max(0, 8 - _doc.Lines.Count);
            for (int i = 0; i < empties; i++)
                for (int j = 0; j < 5; j++)
                    table.Cell().Element(DataCell).Text("");

            // ── Footer row ───────────────────────────────────────────────
            table.Cell().ColumnSpan(2).Element(FooterCell)
                .AlignCenter()
                .Text(ThaiAmountInWords(_doc.GrandTotal)).Bold().FontSize(10);

            table.Cell().ColumnSpan(2).Element(FooterCell).Column(c =>
            {
                c.Item().AlignCenter().Text("รวมทั้งสิ้น").FontSize(8.5f);
                c.Item().AlignCenter().Text("(ราคารวมภาษีมูลค่าเพิ่มแล้ว)").FontSize(7.5f);
            });

            table.Cell().Element(FooterCell)
                .AlignRight()
                .Text(_doc.GrandTotal.ToString("N2")).Bold().FontSize(10);
        });
    }

    private void ComposeSignature(ColumnDescriptor col)
    {
        col.Item().PaddingTop(8).Row(row =>
        {
            row.RelativeItem()
                .Text("ลงชื่อ  .........................................  ผู้รับเงิน")
                .FontSize(10);
            row.AutoItem()
                .Text($"วันที่  {ThaiDate(_doc.InvoiceDate)}")
                .FontSize(10);
        });
    }

    // ─── Thai text helpers ─────────────────────────────────────────────────

    private static readonly string[] ThaiMonths =
        ["", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."];

    private static string ThaiDate(DateTime d) =>
        $"{d.Day} {ThaiMonths[d.Month]} {(d.Year + 543) % 100:D2}";

    internal static string ThaiAmountInWords(decimal amount)
    {
        var rounded = Math.Round(amount, 2);
        long baht   = (long)Math.Floor(rounded);
        int satang  = (int)Math.Round((rounded - baht) * 100);

        if (baht == 0 && satang == 0) return "ศูนย์บาทถ้วน";

        var sb = new System.Text.StringBuilder();
        if (baht > 0)   { sb.Append(NumberWords(baht));   sb.Append("บาท"); }
        if (satang > 0) { sb.Append(NumberWords(satang)); sb.Append("สตางค์"); }
        else              sb.Append("ถ้วน");
        return sb.ToString();
    }

    private static string NumberWords(long n)
    {
        if (n == 0) return "";
        string[] ones = ["", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า"];
        string[] tens = ["", "สิบ", "ยี่สิบ", "สามสิบ", "สี่สิบ", "ห้าสิบ", "หกสิบ", "เจ็ดสิบ", "แปดสิบ", "เก้าสิบ"];

        if (n >= 1_000_000) return NumberWords(n / 1_000_000) + "ล้าน" + NumberWords(n % 1_000_000);

        var sb = new System.Text.StringBuilder();
        if (n >= 100_000) { sb.Append(ones[n / 100_000]).Append("แสน"); n %= 100_000; }
        if (n >= 10_000)  { sb.Append(ones[n / 10_000]).Append("หมื่น");  n %= 10_000; }
        if (n >= 1_000)   { sb.Append(ones[n / 1_000]).Append("พัน");    n %= 1_000; }
        if (n >= 100)     { sb.Append(ones[n / 100]).Append("ร้อย");     n %= 100; }
        if (n >= 10)
        {
            int t = (int)(n / 10), o = (int)(n % 10);
            sb.Append(tens[t]);
            if (o == 1) sb.Append("เอ็ด"); else if (o > 0) sb.Append(ones[o]);
        }
        else if (n > 0) sb.Append(ones[(int)n]);
        return sb.ToString();
    }
}

internal sealed class MultiInvoicePdfDocument : IDocument
{
    private readonly IReadOnlyList<InvoiceDocumentModel> _docs;

    static MultiInvoicePdfDocument()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public MultiInvoicePdfDocument(IReadOnlyList<InvoiceDocumentModel> docs) => _docs = docs;

    public DocumentMetadata GetMetadata() =>
        new() { Title = "ใบกำกับภาษีอย่างย่อรวม" };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        foreach (var doc in _docs)
        {
            new InvoicePdfDocument(doc).Compose(container);
        }
    }
}
