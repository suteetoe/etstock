# Frontend Brief — Phase 1 Company Form

## Context

You are an Avalonia Frontend Engineer on the ETStock project.
Working directory: D:\Source\ETStock
Current branch: feature/phase-1-company (already checked out — do NOT create or switch branches)

## Stack
- Avalonia 11.3.11 + CommunityToolkit.Mvvm 8.2.1
- MVVM: [ObservableProperty], [RelayCommand], ViewModelBase (ObservableObject)
- Compiled bindings: AvaloniaUseCompiledBindingsByDefault=true → always set x:DataType

## Read these files first

1. ETStock/ViewModels/CompanyViewModel.cs — current stub
2. ETStock/Views/CompanyView.axaml — current placeholder
3. ETStock/ViewModels/MainWindowViewModel.cs — need to update for DI
4. ETStock/Models/Company.cs — entity fields to bind
5. .team/backend-contract-1.md — ICompanyRepository interface (read this carefully)
6. ETStock/Program.cs — Program.Services is public static IServiceProvider?

## Task

### 1. Update ETStock/ViewModels/CompanyViewModel.cs

Replace the stub with:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Models;

namespace ETStock.ViewModels;

public partial class CompanyViewModel : ViewModelBase
{
    private readonly ICompanyRepository? _repository;

    // Design-time constructor (parameterless — Avalonia designer uses this)
    public CompanyViewModel() { }

    // Runtime constructor — inject ICompanyRepository
    public CompanyViewModel(ICompanyRepository repository)
    {
        _repository = repository;
    }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _taxId = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _branchName = string.Empty;
    [ObservableProperty] private string _branchCode = "00000";
    [ObservableProperty] private string _invoicePrefix = string.Empty;
    [ObservableProperty] private string _vatRate = "7";   // display as percentage e.g. "7"
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_repository is null) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var company = await _repository.GetAsync();
            if (company is not null)
            {
                Name = company.Name;
                TaxId = company.TaxId;
                Address = company.Address;
                BranchName = company.BranchName;
                BranchCode = company.BranchCode;
                InvoicePrefix = company.InvoicePrefix;
                VatRate = (company.VatRate * 100).ToString("0.##");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"โหลดข้อมูลไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_repository is null) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            if (!decimal.TryParse(VatRate, out var vatPct))
                vatPct = 7m;

            var company = new Company
            {
                Name = Name,
                TaxId = TaxId,
                Address = Address,
                BranchName = BranchName,
                BranchCode = BranchCode,
                InvoicePrefix = InvoicePrefix,
                VatRate = vatPct / 100m
            };
            await _repository.UpsertAsync(company);
            StatusMessage = "บันทึกสำเร็จ";
        }
        catch (Exception ex)
        {
            StatusMessage = $"บันทึกไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 2. Update ETStock/Views/CompanyView.axaml

Replace the placeholder content with a real form:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ETStock.ViewModels"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="600"
             x:Class="ETStock.Views.CompanyView"
             x:DataType="vm:CompanyViewModel">

    <Design.DataContext>
        <vm:CompanyViewModel/>
    </Design.DataContext>

    <ScrollViewer>
        <StackPanel Margin="24" Spacing="16" MaxWidth="600">
            <TextBlock Text="ข้อมูลบริษัท" FontSize="22" FontWeight="Bold" Margin="0,0,0,8"/>

            <Grid ColumnDefinitions="160,*" RowDefinitions="Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto" RowSpacing="10">
                <!-- Row 0: Name -->
                <TextBlock Grid.Row="0" Grid.Column="0" Text="ชื่อบริษัท" VerticalAlignment="Center"/>
                <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding Name}" IsEnabled="{Binding !IsBusy}"/>

                <!-- Row 1: TaxId -->
                <TextBlock Grid.Row="1" Grid.Column="0" Text="เลขผู้เสียภาษี" VerticalAlignment="Center"/>
                <TextBox Grid.Row="1" Grid.Column="1" Text="{Binding TaxId}" MaxLength="13" IsEnabled="{Binding !IsBusy}"/>

                <!-- Row 2: Address -->
                <TextBlock Grid.Row="2" Grid.Column="0" Text="ที่อยู่" VerticalAlignment="Top" Margin="0,4,0,0"/>
                <TextBox Grid.Row="2" Grid.Column="1" Text="{Binding Address}" TextWrapping="Wrap"
                         AcceptsReturn="True" Height="80" IsEnabled="{Binding !IsBusy}"/>

                <!-- Row 3: BranchName -->
                <TextBlock Grid.Row="3" Grid.Column="0" Text="ชื่อสาขา" VerticalAlignment="Center"/>
                <TextBox Grid.Row="3" Grid.Column="1" Text="{Binding BranchName}" IsEnabled="{Binding !IsBusy}"/>

                <!-- Row 4: BranchCode -->
                <TextBlock Grid.Row="4" Grid.Column="0" Text="รหัสสาขา" VerticalAlignment="Center"/>
                <TextBox Grid.Row="4" Grid.Column="1" Text="{Binding BranchCode}" MaxLength="5" IsEnabled="{Binding !IsBusy}"/>

                <!-- Row 5: InvoicePrefix -->
                <TextBlock Grid.Row="5" Grid.Column="0" Text="Prefix ใบกำกับ" VerticalAlignment="Center"/>
                <TextBox Grid.Row="5" Grid.Column="1" Text="{Binding InvoicePrefix}" IsEnabled="{Binding !IsBusy}"/>

                <!-- Row 6: VatRate -->
                <TextBlock Grid.Row="6" Grid.Column="0" Text="อัตรา VAT (%)" VerticalAlignment="Center"/>
                <TextBox Grid.Row="6" Grid.Column="1" Text="{Binding VatRate}" IsEnabled="{Binding !IsBusy}"/>

                <!-- Row 7: Status message -->
                <TextBlock Grid.Row="7" Grid.Column="1" Text="{Binding StatusMessage}"
                           Foreground="Green" FontStyle="Italic" IsVisible="{Binding StatusMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"/>

                <!-- Row 8: Buttons -->
                <StackPanel Grid.Row="8" Grid.Column="1" Orientation="Horizontal" Spacing="8">
                    <Button Content="โหลดข้อมูล" Command="{Binding LoadCommand}" IsEnabled="{Binding !IsBusy}"/>
                    <Button Content="บันทึก" Command="{Binding SaveCommand}" IsEnabled="{Binding !IsBusy}"/>
                </StackPanel>
            </Grid>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

### 3. Update ETStock/ViewModels/MainWindowViewModel.cs

Inject ICompanyRepository from Program.Services into CompanyViewModel:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace ETStock.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public ObservableCollection<NavItem> NavItems { get; } = [];

    public MainWindowViewModel()
    {
        var repo = Program.Services?.CreateScope()
                          .ServiceProvider
                          .GetService<ICompanyRepository>();

        var home = new HomeViewModel();
        var company = repo is not null ? new CompanyViewModel(repo) : new CompanyViewModel();
        var product = new ProductViewModel();
        var invoice = new InvoiceViewModel();

        NavItems.Add(new NavItem("หน้าแรก", home));
        NavItems.Add(new NavItem("ข้อมูลบริษัท", company));
        NavItems.Add(new NavItem("สินค้า / สต๊อก", product));
        NavItems.Add(new NavItem("ใบกำกับภาษี", invoice));

        _currentPage = home;
    }

    [RelayCommand]
    private void Navigate(NavItem item) => CurrentPage = item.Page;
}

public record NavItem(string Label, ViewModelBase Page);
```

### 4. Build

Run: dotnet build ETStock.slnx

Must complete with 0 errors.

### 5. Write notes file

Write D:\Source\ETStock\.team\frontend-notes-1.md with:
- Summary of UI changes
- All observable properties and their bindings
- Commands (Load, Save)
- QA hints (what to test)

### 6. Commit your work

```
cd D:\Source\ETStock
git add ETStock/ViewModels/CompanyViewModel.cs ETStock/Views/CompanyView.axaml ETStock/ViewModels/MainWindowViewModel.cs .team/frontend-notes-1.md
git commit -m "feat(fe): implement CompanyViewModel + CompanyView form [phase 1]"
```

Do NOT push. Do NOT open a PR.

## Acceptance criteria
- 0 build errors
- CompanyViewModel has all 7 field properties + IsBusy + StatusMessage
- CompanyViewModel has LoadCommand + SaveCommand using ICompanyRepository
- CompanyView shows form with all 7 fields + save button
- Design.DataContext uses parameterless constructor (for Avalonia designer)
- MainWindowViewModel injects ICompanyRepository from Program.Services
- .team/frontend-notes-1.md written
- Changes committed
