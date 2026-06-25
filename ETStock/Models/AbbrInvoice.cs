namespace ETStock.Models;
public class AbbrInvoice
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty; // เลขที่ใบกำกับ
    public DateTime InvoiceDate { get; set; }
    public int TaxYear { get; set; }    // ปีภาษี (พ.ศ.)
    public int TaxMonth { get; set; }   // เดือนภาษี 1-12
    public decimal TotalAmount { get; set; } // มูลค่ารวม (รวม VAT)
    public decimal VatAmount { get; set; }   // ภาษีมูลค่าเพิ่ม
    public ICollection<AbbrInvoiceItem> Items { get; set; } = [];
}
