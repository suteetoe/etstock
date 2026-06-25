# Frontend Brief — Phase 0 Foundation

**Branch:** feature/phase-0-foundation (already exists; just edit files)
**Commit convention:** `feat(fe): <description> [phase 0]`
**DO NOT run any git commands.**

## Context
Read backend contract first: D:\Source\ETStock\.team\backend-contract-phase0.md

ETStock เป็นระบบออกใบกำกับภาษีอย่างย่อสำหรับร้านค้าปลีก
Stack: Avalonia 11.3.11, CommunityToolkit.Mvvm 8.2.1, .NET 10, compiled bindings ON.
Current project: D:\Source\ETStock\ETStock\ETStock.csproj

## Objective

Scaffold the MainWindow + navigation shell ที่ต่อยอดได้ใน phase หลัง:
- Sidebar navigation ซ้าย (รายชื่อหน้า)
- Content area ขวา (แสดง view ที่เลือก)
- Theme: Fluent Dark (ปรับเป็น Light ก็ได้ขึ้นกับ preference)
- ชื่อหน้าต่าง: "ETStock — ระบบออกใบกำกับภาษีอย่างย่อ"

## Tasks

### 1. Create navigation pages (placeholder views)

สร้าง Views และ ViewModels สำหรับ 4 หน้าหลัก (placeholder — จะใส่เนื้อหาจริงใน phase 1-5):

**หน้าที่ต้องสร้าง:**
| View class | File | Label ใน sidebar |
|---|---|---|
| `HomeView` | `ETStock/Views/HomeView.axaml` | หน้าแรก |
| `CompanyView` | `ETStock/Views/CompanyView.axaml` | ข้อมูลบริษัท |
| `ProductView` | `ETStock/Views/ProductView.axaml` | สินค้า / สต๊อก |
| `InvoiceView` | `ETStock/Views/InvoiceView.axaml` | ใบกำกับภาษี |

แต่ละ View:
- เป็น `UserControl` (ไม่ใช่ Window)
- มี `x:DataType` ชี้ไป ViewModel ของตัวเอง
- เนื้อหา placeholder เช่น `<TextBlock>` แสดงชื่อหน้า

แต่ละ ViewModel:
- ไฟล์ใน `ETStock/ViewModels/<Name>ViewModel.cs`
- inherit `ViewModelBase`
- มี property `Title` แสดงชื่อหน้าภาษาไทย

### 2. Update MainWindowViewModel.cs

เพิ่ม navigation logic:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.ViewModels;
using System.Collections.ObjectModel;

namespace ETStock.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public ObservableCollection<NavItem> NavItems { get; } = [];

    public MainWindowViewModel()
    {
        var home = new HomeViewModel();
        var company = new CompanyViewModel();
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

### 3. Update MainWindow.axaml

Replace the current TextBlock with a 2-column layout:
- **Column 0 (220px):** sidebar ListBox ของ NavItems
- **Column 1 (*):** ContentControl แสดง CurrentPage

Layout กรอบกว้างๆ (ไม่ต้องสวยงามมาก — phase 0 คือ scaffold):

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ETStock.ViewModels"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d" d:DesignWidth="1024" d:DesignHeight="680"
        x:Class="ETStock.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="ETStock — ระบบออกใบกำกับภาษีอย่างย่อ"
        Width="1024" Height="680">

    <Design.DataContext>
        <vm:MainWindowViewModel/>
    </Design.DataContext>

    <Grid ColumnDefinitions="220,*">
        <!-- Sidebar -->
        <Border Grid.Column="0" Background="#1E1E2E" Padding="8">
            <StackPanel>
                <TextBlock Text="ETStock" FontSize="18" FontWeight="Bold"
                           Foreground="White" Margin="8,12,8,16"/>
                <ListBox ItemsSource="{Binding NavItems}"
                         SelectedItem="{Binding}"
                         Background="Transparent">
                    <ListBox.ItemTemplate>
                        <DataTemplate x:DataType="vm:NavItem">
                            <Button Content="{Binding Label}"
                                    Command="{Binding $parent[Window].DataContext.NavigateCommand}"
                                    CommandParameter="{Binding}"
                                    HorizontalAlignment="Stretch"
                                    Background="Transparent"/>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </StackPanel>
        </Border>

        <!-- Content -->
        <ContentControl Grid.Column="1" Content="{Binding CurrentPage}" Margin="16"/>
    </Grid>
</Window>
```

### 4. Add ViewLocator DataTemplates

The `ViewLocator.cs` already exists — verify it can resolve `HomeViewModel → HomeView`, etc.  
If it uses the standard pattern `{Namespace}.ViewModels.XxxViewModel → {Namespace}.Views.XxxView`,
it should work automatically.

If `ViewLocator.cs` needs updating, update it. Otherwise leave it.

### 5. Verify App.axaml has ViewLocator registered

Check `App.axaml` — it should have `<local:ViewLocator/>` in `Application.DataTemplates`.  
If missing, add it. Otherwise leave it.

## Conventions to follow

- Compiled bindings ON (`x:DataType` required on every view/data template)
- No logic in code-behind beyond view wiring
- ViewModels inherit `ViewModelBase`
- Use `[ObservableProperty]` and `[RelayCommand]` source generators

## Definition of Done (for this agent's scope)

- `dotnet build ETStock.slnx` passes — 0 errors
- 4 placeholder views exist with proper XAML structure
- MainWindow has sidebar + content area layout
- Navigation between pages compiles (even if not tested at runtime yet)

## Output: write notes to .team/frontend-notes-phase0.md
After completing all work, write `.team/frontend-notes-phase0.md` summarizing:
- Views created and their paths
- ViewModels and their `Title` property values
- How ContentControl resolves views (ViewLocator pattern)
- Any deviations from this brief
- Anything QA should know when writing smoke tests
