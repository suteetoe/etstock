using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ETStock.Models;
using ETStock.Services;
using QuestPDF.Fluent;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ETStock.ViewModels;

public partial class PrintPreviewViewModel : ViewModelBase
{
    public InvoiceDocumentModel Doc { get; }

    public PrintPreviewViewModel(InvoiceDocumentModel doc)
    {
        Doc = doc;
    }

    public event Action? CloseRequested;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    [RelayCommand]
    private async Task PrintAsync()
    {
        var pdfPath = Path.Combine(Path.GetTempPath(), $"etstock_invoice_{Guid.NewGuid():N}.pdf");
        await Task.Run(() => new InvoicePdfDocument(Doc).GeneratePdf(pdfPath));

        var uri = new Uri("file:///" + pdfPath.Replace('\\', '/'));

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var launcher = TopLevel.GetTopLevel(desktop.MainWindow)?.Launcher;
            if (launcher is not null)
            {
                await launcher.LaunchUriAsync(uri);
                return;
            }
        }

        Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
    }

    public static string BuildHtml(InvoiceDocumentModel doc)
    {
        var sb = new StringBuilder();

        // ─── Head + CSS ──────────────────────────────────────────────────
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><style>");
        sb.Append("@page{size:A5 portrait;margin:8mm 10mm}");
        sb.Append("*{box-sizing:border-box;margin:0;padding:0}");
        sb.Append("body{font-family:'TH Sarabun New','Cordia New','AngsanaUPC',Tahoma,sans-serif;font-size:10pt}");
        sb.Append("@media screen{body{max-width:148mm;margin:10mm auto;padding:2mm}}");
        sb.Append(".co-name{font-size:11.5pt;font-weight:bold;margin-bottom:1pt}");
        sb.Append(".co-info{font-size:9.5pt;line-height:1.4}");
        sb.Append(".title{text-align:center;margin:5pt 0 3pt}");
        sb.Append(".title-th{font-size:13pt;font-weight:bold}");
        sb.Append(".title-en{font-size:10pt}");
        sb.Append(".ref{font-size:10pt;margin:2pt 0}");
        sb.Append(".ref-row{display:flex;justify-content:space-between}");
        sb.Append(".uline{border-bottom:1px solid #000;display:inline-block;min-width:70pt;padding:0 3pt}");
        sb.Append("table{width:100%;border-collapse:collapse;margin-top:5pt;font-size:9.5pt}");
        sb.Append("thead tr{background:#CCCCCC}");
        sb.Append("th,td{border:1px solid #444;padding:2pt 4pt;vertical-align:top}");
        sb.Append("th{text-align:center;font-weight:bold}");
        sb.Append(".c{text-align:center}.r{text-align:right}");
        sb.Append("tbody tr{height:14pt}");
        sb.Append(".tf-words{font-weight:bold;font-size:10pt;vertical-align:middle}");
        sb.Append(".tf-label{text-align:right;font-size:8.5pt;line-height:1.3}");
        sb.Append(".tf-amount{text-align:right;font-weight:bold;vertical-align:middle}");
        sb.Append(".sig{display:flex;justify-content:space-between;margin-top:6pt;font-size:9.5pt}");
        sb.Append("</style></head><body>");

        // ─── Company header ──────────────────────────────────────────────
        string branchSuffix = string.IsNullOrWhiteSpace(doc.CompanyBranch) ? "" : $" ({H(doc.CompanyBranch)})";
        sb.Append($"<div class='co-name'>{H(doc.CompanyName)}{branchSuffix}</div>");
        if (!string.IsNullOrWhiteSpace(doc.CompanyAddress))
            sb.Append($"<div class='co-info'>{H(doc.CompanyAddress)}</div>");
        sb.Append($"<div class='co-info'>เลขประจำตัวผู้เสียภาษี {H(doc.CompanyTaxId)}</div>");
        if (!string.IsNullOrWhiteSpace(doc.CompanyPhone))
            sb.Append($"<div class='co-info'>โทร. {H(doc.CompanyPhone)}</div>");

        // ─── Document title ──────────────────────────────────────────────
        sb.Append("<div class='title'>");
        sb.Append("<div class='title-th'>ใบเสร็จรับเงิน / ใบกำกับภาษี (อย่างย่อ)</div>");
        sb.Append("<div class='title-en'>TAX INVOICE</div>");
        sb.Append("</div>");

        // ─── Book / Running / Date ───────────────────────────────────────
        string bookDisplay    = doc.BookNo.HasValue    ? doc.BookNo.Value.ToString()          : "";
        string runDisplay     = doc.RunningNo.HasValue ? doc.RunningNo.Value.ToString("D5")   : doc.InvoiceNo;
        string dateDisplay    = FormatThaiDate(doc.InvoiceDate);

        sb.Append($"<div class='ref'>เล่มที่ <span class='uline'>&nbsp;{H(bookDisplay)}&nbsp;</span></div>");
        sb.Append("<div class='ref ref-row'>");
        sb.Append($"<span>เลขที่ <span class='uline'>&nbsp;{H(runDisplay)}&nbsp;</span></span>");
        sb.Append($"<span>วันที่ <span class='uline'>&nbsp;{H(dateDisplay)}&nbsp;</span></span>");
        sb.Append("</div>");

        // ─── Items table ─────────────────────────────────────────────────
        sb.Append("<table><thead><tr>");
        sb.Append("<th style='width:20pt'>ลำดับ</th>");
        sb.Append("<th>รายการสินค้า</th>");
        sb.Append("<th style='width:34pt'>จำนวน</th>");
        sb.Append("<th style='width:54pt'>ราคาต่อหน่วย</th>");
        sb.Append("<th style='width:54pt'>จำนวนเงิน</th>");
        sb.Append("</tr></thead><tbody>");

        int seq = 1;
        foreach (var line in doc.Lines)
        {
            sb.Append("<tr>");
            sb.Append($"<td class='c'>{seq++}</td>");
            sb.Append($"<td>{H(line.ProductName)}</td>");
            sb.Append($"<td class='r'>{line.Qty:N0}</td>");
            sb.Append($"<td class='r'>{line.UnitPrice:N2}</td>");
            sb.Append($"<td class='r'>{line.LineTotal:N2}</td>");
            sb.Append("</tr>");
        }

        // Pad to at least 8 rows so the table fills a good portion of the A5 page
        int empties = Math.Max(0, 8 - doc.Lines.Count);
        for (int i = 0; i < empties; i++)
            sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr>");

        sb.Append("</tbody>");

        // ─── Footer: Thai words + grand total ────────────────────────────
        sb.Append("<tfoot><tr>");
        sb.Append($"<td colspan='3' class='tf-words'>{H(ThaiAmountInWords(doc.GrandTotal))}</td>");
        sb.Append("<td class='tf-label'>รวมทั้งสิ้น<br/>(ราคารวมภาษีมูลค่าเพิ่มแล้ว)</td>");
        sb.Append($"<td class='tf-amount'>{doc.GrandTotal:N2}</td>");
        sb.Append("</tr></tfoot></table>");

        // ─── Signature ───────────────────────────────────────────────────
        sb.Append("<div class='sig'>");
        sb.Append("<span>ลงชื่อ <span class='uline' style='min-width:110pt'></span> ผู้รับเงิน</span>");
        sb.Append("<span>วันที่ <span class='uline' style='min-width:80pt'></span></span>");
        sb.Append("</div>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    // ─── Thai helpers ─────────────────────────────────────────────────────

    private static readonly string[] ThaiMonthsAbbr =
        ["", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."];

    private static string FormatThaiDate(DateTime date)
    {
        int beYear = date.Year + 543;
        return $"{date.Day} {ThaiMonthsAbbr[date.Month]} {beYear % 100:D2}";
    }

    public static string ThaiAmountInWords(decimal amount)
    {
        var rounded = Math.Round(amount, 2);
        long baht = (long)Math.Floor(rounded);
        int satang = (int)Math.Round((rounded - baht) * 100);

        if (baht == 0 && satang == 0)
            return "ศูนย์บาทถ้วน";

        var result = new StringBuilder();

        if (baht > 0)
        {
            result.Append(NumberToThaiWords(baht));
            result.Append("บาท");
        }

        if (satang > 0)
        {
            result.Append(NumberToThaiWords(satang));
            result.Append("สตางค์");
        }
        else
        {
            result.Append("ถ้วน");
        }

        return result.ToString();
    }

    private static string NumberToThaiWords(long number)
    {
        if (number == 0) return "";

        string[] ones = ["", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า"];
        string[] tens = ["", "สิบ", "ยี่สิบ", "สามสิบ", "สี่สิบ", "ห้าสิบ", "หกสิบ", "เจ็ดสิบ", "แปดสิบ", "เก้าสิบ"];

        if (number >= 1_000_000)
            return NumberToThaiWords(number / 1_000_000) + "ล้าน" + NumberToThaiWords(number % 1_000_000);

        var sb = new StringBuilder();

        if (number >= 100_000) { sb.Append(ones[number / 100_000]).Append("แสน"); number %= 100_000; }
        if (number >= 10_000)  { sb.Append(ones[number / 10_000]).Append("หมื่น");  number %= 10_000; }
        if (number >= 1_000)   { sb.Append(ones[number / 1_000]).Append("พัน");    number %= 1_000; }
        if (number >= 100)     { sb.Append(ones[number / 100]).Append("ร้อย");     number %= 100; }

        if (number >= 10)
        {
            int t = (int)(number / 10);
            int o = (int)(number % 10);
            sb.Append(tens[t]);
            if (o == 1)      sb.Append("เอ็ด");
            else if (o > 0)  sb.Append(ones[o]);
        }
        else if (number > 0)
        {
            sb.Append(ones[(int)number]);
        }

        return sb.ToString();
    }

    private static string H(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
