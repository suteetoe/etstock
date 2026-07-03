namespace ETStock.Models;
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;           // ชื่อบริษัท
    public string TaxId { get; set; } = string.Empty;          // เลขผู้เสียภาษี 13 หลัก
    public string Address { get; set; } = string.Empty;        // ที่อยู่
    public string BranchName { get; set; } = string.Empty;     // ชื่อสาขา
    public string BranchCode { get; set; } = "00000";          // รหัสสาขา
    public string PhoneNumber { get; set; } = string.Empty;      // เบอร์โทรศัพท์
    public string InvoicePrefix { get; set; } = string.Empty;  // prefix เลขใบกำกับ
    public decimal VatRate { get; set; } = 0.07m;              // อัตราภาษี (7%)
}
