namespace ETStock.Models;
public class AbbrInvoiceItem
{
    public int Id { get; set; }
    public int AbbrInvoiceId { get; set; }
    public AbbrInvoice AbbrInvoice { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Qty { get; set; }
    public decimal Amount { get; set; }    // ก่อน VAT
    public decimal VatAmount { get; set; } // VAT ส่วนของรายการนี้
}
