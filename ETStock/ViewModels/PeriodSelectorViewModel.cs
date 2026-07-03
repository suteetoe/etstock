using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ETStock.ViewModels;

public partial class PeriodSelectorViewModel : ViewModelBase
{
    public PeriodSelectorViewModel()
    {
        var today = DateTimeOffset.Now;
        _selectedDate = today;
        _selectedYear = today.Year;
        _selectedMonth = today.Month;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private DateTimeOffset? _selectedDate;

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedMonth;

    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            SelectedYear = value.Value.Year;
            SelectedMonth = value.Value.Month;
        }
    }

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

    private bool CanConfirm() => SelectedDate.HasValue;
}
