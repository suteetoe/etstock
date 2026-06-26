namespace ETStock.Models;
public class AbbrInvoiceItem
{
    public int Id { get; set; }
    public int AbbrInvoiceId { get; set; }
    public AbbrInvoice AbbrInvoice { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;  // แทน ProductId FK
    public decimal Qty { get; set; }
    public decimal Amount { get; set; }    // ก่อน VAT
    public decimal VatAmount { get; set; } // VAT ส่วนของรายการนี้
}
