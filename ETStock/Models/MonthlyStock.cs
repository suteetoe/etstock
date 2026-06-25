namespace ETStock.Models;
public class MonthlyStock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }          // 1-12
    public decimal OpeningQty { get; set; } // ยกมา
    public decimal BuyQty { get; set; }     // ซื้อ
    public decimal SellFullQty { get; set; }// ขายเต็มใบ
    public decimal SellPosQty { get; set; } // ขายหน้าร้าน
    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;
}
