# Frontend Brief — Phase: pdf-preview-webview

## Context
- คุณคือ **Frontend Agent** ของทีม ETStock
- Branch ปัจจุบัน: `feature/phase-pdf-preview-webview` (Lead สร้างไว้แล้ว อยู่บน `develop`)
- Working directory: `D:\Source\ETStock`
- โปรเจกต์: Avalonia 11.3 (.NET 10, WinExe) + CommunityToolkit.Mvvm 8.2

## เป้าหมาย
เปลี่ยนหน้าจอ preview PDF ของใบกำกับภาษีอย่างย่อ จาก MuPDFCore ไปใช้ **WebView ของ Avalonia** (`Avalonia.WebView` package) ฝังใน Window ของเราเอง พร้อม toolbar ปุ่ม "พิมพ์" และ "ปิด"

## Read these files first
1. `ETStock/Views/PdfPreviewWindow.axaml` (XAML เดิม — ใช้ MuPDF `PDFRenderer`)
2. `ETStock/Views/PdfPreviewWindow.axaml.cs` (code-behind เดิม — ZoomIn/ZoomOut/Fit/Print/Close)
3. `ETStock/Views/InvoiceView.axaml.cs` (ตัวเรียก `PdfPreviewWindow`)
4. `ETStock/ETStock.csproj` (ดู package refs)
5. `.team/plan-pdf-preview-webview.md`

## Scope (แก้ไฟล์เหล่านี้เท่านั้น)
- `ETStock/ETStock.csproj` — เพิ่ม package `Avalonia.WebView`, **ลบ** `MuPDFCore.MuPDFRenderer`
- `ETStock/Views/PdfPreviewWindow.axaml` — เขียนใหม่ ใช้ WebView แทน PDFRenderer
- `ETStock/Views/PdfPreviewWindow.axaml.cs` — เขียนใหม่ จัดการ temp file + WebView lifecycle
- ห้ามแก้ไฟล์อื่นโดยเฉพาะ ViewModels, Services, Repositories

## รายละเอียดงาน

### 1. แก้ csproj
- เพิ่ม `<PackageReference Include="Avalonia.WebView" Version="12.0.1" />` (หรือเวอร์ชันล่าสุดที่เข้ากับ Avalonia 11.3 — ถ้า 12.0.1 ไม่เข้าให้ใช้เวอร์ชัน 11.x ที่คอมไพล์ผ่าน)
- **ลบ** `<PackageReference Include="MuPDFCore.MuPDFRenderer" Version="2.0.1" />`
- ถ้ามี using `MuPDFCore` ค้างอยู่ในไฟล์อื่น ให้ลบออกด้วย

### 2. สร้าง `PdfPreviewWindow.axaml` ใหม่
- เก็บโครงเดิม: Grid RowDefinitions="Auto,*"
- Row 0: Toolbar — ปุ่ม **"พิมพ์"** (Classes="accent") + **"ปิด"** (ขวา) — **เอา ZoomIn/ZoomOut/FitPage ออก** เพราะ WebView มี zoom ของตัวเอง (Ctrl+scroll /  pinch)
- Row 1: ใส่ `<WebView>` control (namespace `https://github.com/avaloniaui/webview` หรือตามที่ package กำหนด — ตรวจจากตัวอย่าง package)
  - ตั้งชื่อ `x:Name="WebView"`
  - ไม่ต้องผูก Url ตอน XAML — จะตั้งใน code-behind
- Window: Title="พรีวิวใบกำกับภาษีอย่างย่อ", Width=760, Height=900, WindowStartupLocation="CenterOwner" (เหมือนเดิม)

### 3. สร้าง `PdfPreviewWindow.axaml.cs` ใหม่
สร้างคลาสตามรูปแบบนี้:

```csharp
public partial class PdfPreviewWindow : Window
{
    private string _tempPdfPath = string.Empty;

    public PdfPreviewWindow() => InitializeComponent();

    public PdfPreviewWindow(byte[] pdfBytes) : this()
    {
        _tempPdfPath = Path.Combine(Path.GetTempPath(), $"etstock_invoice_{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(_tempPdfPath, pdfBytes);

        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        // โหลด PDF เข้า WebView ผ่าน file URI
        var uri = new Uri("file:///" + _tempPdfPath.Replace('\\', '/'));
        // ใช้ API ที่ package ให้ (เช่น WebView.Url = uri หรือ WebView.CoreWebView2.Navigate)
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        // ลบ temp file
        try { if (File.Exists(_tempPdfPath)) File.Delete(_tempPdfPath); } catch { }
    }

    private async void Print_Click(object? sender, RoutedEventArgs e)
    {
        // เรียกใช้ฟังก์ชันพิมพ์ของ WebView (เช่น ExecuteScriptAsync("window.print()")
        // หรือ API เฉพาะของ package ถ้ามี
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
```

**สำคัญ**: API ที่แน่นอน (property/method ของ WebView) ขึ้นกับ package — อ่าน README ของ `Avalonia.WebView` ที่หลัง `dotnet add package` แล้วปรับให้คอมไพล์ผ่าน ตัวอย่างที่ต้องดู:
- วิธี set URL / Navigate
- วิธีเรียก script (สำหรับ `window.print()`)
- namespace ที่ต้อง using

### 4. ไม่ต้องแก้ `InvoiceView.axaml.cs`
`OnPdfPreviewRequested(byte[] pdfBytes)` ยังเปิด `new PdfPreviewWindow(pdfBytes)` เหมือนเดิม — ไม่ต้องแตะ

## Acceptance Criteria
- [ ] `dotnet build` ผ่าน ไม่มี error
- [ ] `MuPDFCore` ถูกลบจาก csproj และไม่มี using ค้าง
- [ ] `Avalonia.WebView` package ถูกเพิ่ม
- [ ] `PdfPreviewWindow` แสดง PDF ผ่าน WebView เมื่อกดปุ่ม "พิมพ์" ใน InvoiceView
- [ ] ปุ่ม "พิมพ์" บน toolbar เรียก print dialog ของเบราว์เซอร์/WebView
- [ ] ปุ่ม "ปิด" ปิด Window
- [ ] temp file ถูกลบเมื่อปิด Window
- [ ] ไม่มี logic ใหม่ใน ViewModels/Services

## Commit
```
feat(fe): replace MuPDF preview with Avalonia.WebView + toolbar print [phase pdf-preview-webview]
```

## หลังทำเสร็จ
- commit งาน (prefix `feat(fe):`) บน branch `feature/phase-pdf-preview-webview`
- รายงานกลับ: ไฟล์ที่แก้, package ที่ใช้จริง (เวอร์ชัน), ปัญหาที่เจอ (ถ้ามี)
