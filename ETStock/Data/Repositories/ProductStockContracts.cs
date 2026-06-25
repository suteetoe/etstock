namespace ETStock.Data.Repositories;

public enum ProductWriteResult
{
    Success,
    DuplicateCode,
    NotFound
}

public sealed record ProductWithStock(
    int Id,
    string Code,
    string Name,
    string Unit,
    decimal CostPrice,
    decimal SellPrice,
    MonthlyStockSnapshot? MonthlyStock);

public sealed record MonthlyStockInput(
    int ProductId,
    int Year,
    int Month,
    decimal OpeningQty,
    decimal BuyQty,
    decimal SellFullQty,
    decimal SellPosQty);

public sealed record MonthlyStockSnapshot(
    int Id,
    int ProductId,
    int Year,
    int Month,
    decimal OpeningQty,
    decimal BuyQty,
    decimal SellFullQty,
    decimal SellPosQty)
{
    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;
}
