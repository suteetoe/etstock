using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ETStock.ViewModels;

public partial class PeriodSelectorViewModel : ViewModelBase
{
    public PeriodSelectorViewModel()
    {
        var today = DateTime.Today;
        _selectedYear = today.Year;
        _selectedMonth = today.Month;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private int _selectedYear;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private int _selectedMonth;

    public bool IsConfirmed { get; private set; }

    public event EventHandler? ConfirmRequested;
    public event EventHandler? CancelRequested;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        IsConfirmed = true;
        ConfirmRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CancelRequested?.Invoke(this, EventArgs.Empty);

    private bool CanConfirm() =>
        SelectedYear >= 2000 && SelectedYear <= 2100 &&
        SelectedMonth >= 1 && SelectedMonth <= 12;
}
