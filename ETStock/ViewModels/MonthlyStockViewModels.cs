using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Services;

namespace ETStock.ViewModels;


public partial class MonthlyStockRowViewModel : ViewModelBase
{
    public MonthlyStockRowViewModel(ProductWithStock product)
    {
        Name = product.ProductName;
        Unit = product.Unit;
        _costPrice = product.CostPrice;
        _sellPrice = product.SellPrice;

        var stock = product.MonthlyStock;
        StockId = stock?.Id ?? 0;
        _openingQty = stock?.OpeningQty ?? 0;
        _buyQty = stock?.BuyQty ?? 0;
        _sellFullQty = stock?.SellFullQty ?? 0;
        _sellPosQty = stock?.SellPosQty ?? 0;
        CarryForwardQty = stock?.CarryForwardQty ?? 0;
    }

    public MonthlyStockRowViewModel(ExcelImportRecord record)
    {
        Name = record.ProductName;
        Unit = string.Empty;
        StockId = 0;
        _costPrice = record.CostPrice;
        _sellPrice = record.SellPrice;
        _openingQty = record.OpeningQty;
        _buyQty = 0;
        _sellFullQty = 0;
        _sellPosQty = 0;
    }

    public int StockId { get; }
    public string Name { get; }
    public string Unit { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    private decimal _costPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SalesAmount))]
    private decimal _sellPrice;

    public decimal ClosingValue => ClosingQty * CostPrice;

    [ObservableProperty]
    private int _lineNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    private decimal _openingQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    private decimal _buyQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    [NotifyPropertyChangedFor(nameof(SalesQty))]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    private decimal _sellFullQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    [NotifyPropertyChangedFor(nameof(SalesQty))]
    [NotifyPropertyChangedFor(nameof(SalesAmount))]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    private decimal _sellPosQty;

    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;
    public decimal SalesQty => SellFullQty + SellPosQty;
    public decimal SalesAmount => SellPosQty * SellPrice;
    public decimal TotalCost => SalesQty * CostPrice;
    public decimal CarryForwardQty { get; private set; }

    public MonthlyStockInput ToInput(int year, int month) => new(
        Name,
        Unit,
        CostPrice,
        SellPrice,
        year,
        month,
        OpeningQty,
        BuyQty,
        SellFullQty,
        SellPosQty);
}
