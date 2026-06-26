namespace ETStock.Models;
public class MonthlyStock
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }          // 1-12
    public string ProductName { get; set; } = string.Empty;  // ชื่อสินค้า (unique key แทน ProductId)
    public string Unit { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }
    public decimal OpeningQty { get; set; } // ยกมา
    public decimal BuyQty { get; set; }     // ซื้อ
    public decimal SellFullQty { get; set; }// ขายเต็มใบ
    public decimal SellPosQty { get; set; } // ขายหน้าร้าน
    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;
    public decimal ClosingValue => ClosingQty * CostPrice;
}
