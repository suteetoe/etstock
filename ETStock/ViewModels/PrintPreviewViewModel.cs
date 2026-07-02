using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ETStock.Models;
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
        var html = BuildHtml(Doc);
        var tempPath = Path.Combine(Path.GetTempPath(), $"etstock_print_{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(tempPath, html, Encoding.UTF8);

        var uri = new Uri("file:///" + tempPath.Replace('\\', '/'));

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var launcher = TopLevel.GetTopLevel(desktop.MainWindow)?.Launcher;
            if (launcher is not null)
            {
                await launcher.LaunchUriAsync(uri);
                return;
            }
        }

        // Fallback: open directly via shell (non-blocking spawn)
        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
    }

    public static string BuildHtml(InvoiceDocumentModel doc)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><style>");
        sb.Append("body{font-family:sans-serif;margin:20mm;font-size:14px}h2{margin-bottom:4px}p{margin:2px 0}");
        sb.Append("table{width:100%;border-collapse:collapse;margin-top:12px}td,th{border:1px solid #ccc;padding:6px 10px}th{background:#f0f0f0}");
        sb.Append(".r{text-align:right}.totals{margin-top:12px;text-align:right}");
        sb.Append("</style></head><body>");
        sb.Append($"<h2>{H(doc.CompanyName)}</h2>");
        sb.Append($"<p>เลขผู้เสียภาษี: {H(doc.CompanyTaxId)}</p>");
        sb.Append($"<p>{H(doc.CompanyAddress)}</p>");
        sb.Append($"<p>สาขา: {H(doc.CompanyBranch)} ({H(doc.CompanyBranchCode)})</p>");
        sb.Append("<hr/><h3>ใบกำกับภาษีอย่างย่อ</h3>");
        sb.Append($"<p><b>เลขที่:</b> {H(doc.InvoiceNo)} &nbsp; <b>วันที่:</b> {doc.InvoiceDate:dd/MM/yyyy} &nbsp; <b>ปีภาษี (พ.ศ.):</b> {doc.TaxYear}/{doc.TaxMonth:D2}</p>");
        sb.Append("<table><thead><tr><th>ชื่อสินค้า</th><th class='r'>จำนวน</th><th class='r'>ยอดก่อน VAT</th><th class='r'>VAT</th></tr></thead><tbody>");
        foreach (var line in doc.Lines)
            sb.Append($"<tr><td>{H(line.ProductName)}</td><td class='r'>{line.Qty:N2}</td><td class='r'>{line.Amount:N2}</td><td class='r'>{line.VatAmount:N2}</td></tr>");
        sb.Append("</tbody></table>");
        sb.Append("<div class='totals'>");
        sb.Append($"<p>ยอดก่อน VAT: <b>{doc.SubTotal:N2} บาท</b></p>");
        sb.Append($"<p>VAT: <b>{doc.VatTotal:N2} บาท</b></p>");
        sb.Append($"<p style='font-size:16px'>ยอดรวม: <b>{doc.GrandTotal:N2} บาท</b></p>");
        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    private static string H(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
