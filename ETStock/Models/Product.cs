namespace ETStock.Models;
public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;   // รหัสสินค้า (unique)
    public string Name { get; set; } = string.Empty;   // ชื่อสินค้า
    public string Unit { get; set; } = string.Empty;   // หน่วย เช่น ชิ้น, กล่อง
    public decimal CostPrice { get; set; }             // ต้นทุน
    public decimal SellPrice { get; set; }             // ราคาขาย
    public ICollection<MonthlyStock> MonthlyStocks { get; set; } = [];
}
