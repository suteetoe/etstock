namespace ETStock.Data.Repositories;

public sealed record ProductWithStock(
    string ProductName,
    string Unit,
    decimal CostPrice,
    decimal SellPrice,
    MonthlyStockSnapshot? MonthlyStock);

public sealed record MonthlyStockInput(
    string ProductName,
    string Unit,
    decimal CostPrice,
    decimal SellPrice,
    int Year,
    int Month,
    decimal OpeningQty,
    decimal BuyQty,
    decimal SellFullQty,
    decimal SellPosQty);

public sealed record MonthlyStockSnapshot(
    int Id,
    string ProductName,
    int Year,
    int Month,
    decimal OpeningQty,
    decimal BuyQty,
    decimal SellFullQty,
    decimal SellPosQty)
{
    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;
}
