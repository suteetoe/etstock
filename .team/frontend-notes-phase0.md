# Frontend Notes — Phase 0 Foundation

**Build status:** `dotnet build ETStock.slnx` → **0 errors, 0 warnings**

---

## Views Created

| View class | File path | x:DataType |
|---|---|---|
| `HomeView` | `ETStock/Views/HomeView.axaml` | `vm:HomeViewModel` |
| `CompanyView` | `ETStock/Views/CompanyView.axaml` | `vm:CompanyViewModel` |
| `ProductView` | `ETStock/Views/ProductView.axaml` | `vm:ProductViewModel` |
| `InvoiceView` | `ETStock/Views/InvoiceView.axaml` | `vm:InvoiceViewModel` |

Each view is a `UserControl` with a centered placeholder `StackPanel` that displays:
- `TextBlock` bound to `{Binding Title}` (the Thai page name)
- A second `TextBlock` noting which phase will fill in the real content

Each view also has a `.axaml.cs` code-behind with only `InitializeComponent()` — no additional logic.

---

## ViewModels

| Class | File path | `Title` value |
|---|---|---|
| `HomeViewModel` | `ETStock/ViewModels/HomeViewModel.cs` | `"หน้าแรก"` |
| `CompanyViewModel` | `ETStock/ViewModels/CompanyViewModel.cs` | `"ข้อมูลบริษัท"` |
| `ProductViewModel` | `ETStock/ViewModels/ProductViewModel.cs` | `"สินค้า / สต๊อก"` |
| `InvoiceViewModel` | `ETStock/ViewModels/InvoiceViewModel.cs` | `"ใบกำกับภาษี"` |

All ViewModels:
- Are `partial class` (required for CommunityToolkit source generators)
- Inherit `ViewModelBase` (which inherits `ObservableObject`)
- Expose `Title` via `[ObservableProperty]` → generates `string Title` property

`MainWindowViewModel` was replaced with navigation logic:
- `ObservableCollection<NavItem> NavItems` — the sidebar list
- `ViewModelBase CurrentPage` (via `[ObservableProperty]`) — bound to `ContentControl`
- `[RelayCommand] Navigate(NavItem item)` — switches `CurrentPage`
- `NavItem` is a `record(string Label, ViewModelBase Page)` in the same file/namespace

---

## ViewLocator Pattern

**File:** `ETStock/ViewLocator.cs` — left unchanged; already correct.

The locator uses:
```csharp
var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
var type = Type.GetType(name);
```

This resolves:
- `ETStock.ViewModels.HomeViewModel` → `ETStock.Views.HomeView`
- `ETStock.ViewModels.CompanyViewModel` → `ETStock.Views.CompanyView`
- `ETStock.ViewModels.ProductViewModel` → `ETStock.Views.ProductView`
- `ETStock.ViewModels.InvoiceViewModel` → `ETStock.Views.InvoiceView`

`App.axaml` already had `<local:ViewLocator/>` in `Application.DataTemplates` — no change needed.

The `ViewLocator.Match()` method returns `true` for any `ViewModelBase` instance, so `ContentControl.Content="{Binding CurrentPage}"` will automatically render the correct view when the page changes.

---

## MainWindow Layout

`ETStock/Views/MainWindow.axaml` — 2-column `Grid`:
- **Column 0 (220px):** Dark sidebar (`#1E1E2E`) with app title and `ListBox` of `NavItems`
- **Column 1 (*):** `ContentControl` bound to `CurrentPage` — ViewLocator resolves the correct view automatically

The `DataTemplate` inside the `ListBox` has `x:DataType="vm:NavItem"` for compiled binding compliance. The `NavigateCommand` is reached via `$parent[Window].DataContext.NavigateCommand` — this binding is specified in the brief and compiled successfully with 0 warnings under Avalonia 11.3.11.

Window title: `"ETStock — ระบบออกใบกำกับภาษีอย่างย่อ"`, size `1024×680`.

---

## Deviations from Brief

1. **`SelectedItem` removed from `ListBox`** — The brief snippet had `SelectedItem="{Binding}"` which is an invalid binding target and would produce a warning/error under compiled bindings. Removed; visual selection state can be re-added in a later phase when a `SelectedNavItem` property is defined on the ViewModel.
2. **Theme stays as `SimpleTheme`** — `App.axaml` already used `<SimpleTheme />`. The csproj references `Avalonia.Themes.Fluent` but the loaded theme is Simple. Switching to Fluent Dark is a one-line change in `App.axaml` (`<FluentTheme/>` + `RequestedThemeVariant="Dark"`) and is left for the team to decide in a later phase.
3. **`Icon` attribute removed from `MainWindow`** — The original `MainWindow.axaml` referenced `/Assets/avalonia-logo.ico`. The brief's replacement layout did not include it; removed to keep the scaffold clean. Add back if needed.

---

## What QA Should Know

### Smoke tests to write (Avalonia.Headless / xUnit)

1. **MainWindowViewModel constructs cleanly** — instantiate with `new MainWindowViewModel()`, assert `NavItems.Count == 4` and `CurrentPage` is `HomeViewModel`.
2. **Navigate command switches page** — call `vm.NavigateCommand.Execute(vm.NavItems[1])`, assert `vm.CurrentPage` is `CompanyViewModel`.
3. **ViewLocator resolves all four pages** — for each ViewModel type, `viewLocator.Build(instance)` should return a non-null control of the matching View type (not a "Not Found" TextBlock).
4. **`Match()` returns true for ViewModelBase subclasses** — and false for non-ViewModelBase objects.

### Runtime notes

- Navigation does **not** dispose/recreate ViewModels on each click — the same instances are reused (they were created once in the constructor). This is intentional for Phase 0 scaffold; Phase 1+ can switch to factory/DI patterns if needed.
- The `ContentControl` relies on the `ViewLocator` IDataTemplate registered in `App.axaml`. If that registration is ever removed, the content area will be blank/text.
- Database connectivity (PostgreSQL) is not exercised by any of these views — Phase 0 is purely UI scaffold.
