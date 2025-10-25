using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Implementations;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class PurchaseOrdersPage : Page
    {
        private IPurchaseOrderService PurchaseOrderService => App.GetService<IPurchaseOrderService>();
        private ISupplierService SupplierService => App.GetService<ISupplierService>();
        private readonly ResourceLoader _resourceLoader;

        public ObservableCollection<PurchaseOrderViewModel> PurchaseOrders { get; } = new ObservableCollection<PurchaseOrderViewModel>();
        public ObservableCollection<Supplier> Suppliers { get; } = new();
        private Supplier? _selectedSupplier = null;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;

        public PurchaseOrdersPage()
        {
            this.InitializeComponent();
            _resourceLoader = new ResourceLoader();
            this.Loaded += PurchaseOrdersPage_Loaded;
        }

        private void PurchaseOrdersPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadPurchaseOrders();
        }

        private void LoadPurchaseOrders()
        {
            // Prevent execution if UI controls are not yet initialized
            if (!_isInitialized)
                return;

            try
            {
                PurchaseOrders.Clear();

                PoStatus? status = null;
                if (FilterStatusBoxPurchaseOrder?.SelectedItem is ComboBoxItem item && item.Tag is string tag && !string.IsNullOrEmpty(tag))
                {
                    status = Enum.Parse<PoStatus>(tag);
                }

                DateTime? dateFrom = null;
                var dateOffsetFrom = FilterOrderDateFromBoxPurchaseOrder?.Date;
                if (dateOffsetFrom.HasValue && dateOffsetFrom.Value.Year > 1900)
                    dateFrom = dateOffsetFrom.Value.DateTime.Date;   // 00:00:00

                DateTime? dateTo = null;
                var dateOffsetTo = FilterOrderDateToBoxPurchaseOrder?.Date;
                if (dateOffsetTo.HasValue && dateOffsetTo.Value.Year > 1900)
                    dateTo = dateOffsetTo.Value.DateTime.Date.AddDays(1).AddSeconds(-1); // 23:59:59

                decimal? amountMin = null;
                if (!string.IsNullOrEmpty(FilterTotalAmountMinBoxPurchaseOrder?.Text) && decimal.TryParse(FilterTotalAmountMinBoxPurchaseOrder.Text, out decimal min))
                    amountMin = min;

                decimal? amountMax = null;
                if (!string.IsNullOrEmpty(FilterTotalAmountMaxBoxPurchaseOrder?.Text) && decimal.TryParse(FilterTotalAmountMaxBoxPurchaseOrder.Text, out decimal max))
                    amountMax = max;

                int? supplierId = null;
                if (_selectedSupplier != null && _selectedSupplier.Id > 0)
                    supplierId = _selectedSupplier.Id;

                int? createdBy = null;
                string? note = null;

                string supplierName = SearchBox.Text;

                var result = PurchaseOrderService.GetPurchaseOrdersFiltered(
                    supplierName,
                    supplierId,
                    createdBy,
                    status,
                    note,
                    dateFrom,
                    dateTo,
                    amountMin,
                    amountMax,
                    CurrentPage,
                    PageSize);

                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to load purchase orders.");
                    return;
                }
                System.Diagnostics.Debug.WriteLine("Thông tin debug 3 ở đây");


                TotalPages = result.Info.TotalPages;
                TotalRecords = result.Info.TotalRecords;
                foreach (var poVM in result.PurchaseOrders)
                {
                    poVM.SupplierName = SupplierService.GetSupplierById(poVM.SupplierId)?.Name ?? "Unknown";
                    PurchaseOrders.Add(poVM);
                }

                System.Diagnostics.Debug.WriteLine("Thông tin debug 4 ở đây");


                // Safely update UI controls with null checks
                if (PageInfoText != null)
                    PageInfoText.Text = $"{CurrentPage} / {TotalPages}";

                if (PreviousPageButton != null)
                    PreviousPageButton.IsEnabled = CurrentPage > 1;

                if (NextPageButton != null)
                    NextPageButton.IsEnabled = CurrentPage < TotalPages;

                if (RecordCountText != null)
                    RecordCountText.Text = $"{PurchaseOrders.Count} / {TotalRecords}";

                UpdateEmptyStateVisibility();
                System.Diagnostics.Debug.WriteLine("Thông tin debug 5 ở đây");
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load purchase orders", ex.Message);
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = PurchaseOrders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (PurchaseOrdersListView != null)
                PurchaseOrdersListView.Visibility = PurchaseOrders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilterPanel != null)
            {
                FilterPanel.Visibility = FilterPanel.Visibility == Visibility.Collapsed
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            CurrentPage = 1;
            LoadPurchaseOrders();
        }

        private void FilterChangeDate(object sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (!_isInitialized) return;

            CurrentPage = 1;
            LoadPurchaseOrders();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchBox != null)
                    SearchBox.Text = string.Empty;
                if (FilterStatusBoxPurchaseOrder != null)
                    FilterStatusBoxPurchaseOrder.SelectedIndex = 0;
                if (FilterOrderDateFromBoxPurchaseOrder != null)
                    FilterOrderDateFromBoxPurchaseOrder.SetValue(
                        CalendarDatePicker.DateProperty, null);

                if (FilterOrderDateToBoxPurchaseOrder != null)
                    FilterOrderDateToBoxPurchaseOrder.SetValue(
                        CalendarDatePicker.DateProperty, null);

                if (FilterTotalAmountMinBoxPurchaseOrder != null)
                    FilterTotalAmountMinBoxPurchaseOrder.Text = string.Empty;
                if (FilterTotalAmountMaxBoxPurchaseOrder != null)
                    FilterTotalAmountMaxBoxPurchaseOrder.Text = string.Empty;
                
                // Clear supplier selection
                _selectedSupplier = null;
                if (SelectedSupplierText != null)
                    SelectedSupplierText.Text = "All Suppliers";
                
                if (SearchBox != null)
                    SearchBox.Text = string.Empty;

                CurrentPage = 1;
                LoadPurchaseOrders();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error clearing filters", ex.Message);
            }
        }

        private async void ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddPurchaseOrderPage));
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is PurchaseOrderViewModel po)
            {
                // Only allow editing DRAFT purchase orders
                if (po.Status != PoStatus.DRAFT)
                {
                    ShowErrorDialog("Cannot Edit", "Only DRAFT purchase orders can be edited.");
                    return;
                }

                // Navigate to add page with purchase order ID for editing
                Frame.Navigate(typeof(AddPurchaseOrderPage), po.Id);
            }
        }

        private async void BtnReceive_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is PurchaseOrderViewModel po)
            {
                // Only allow receiving DRAFT purchase orders
                if (po.Status != PoStatus.DRAFT)
                {
                    ShowErrorDialog("Cannot Receive", "Only DRAFT purchase orders can be received.");
                    return;
                }

                try
                {
                    // Show confirmation dialog
                    var dialog = new ContentDialog
                    {
                        Title = "Confirm Receive Purchase Order",
                        Content = "Are you sure you want to mark this purchase order as received? This will create inventory batches.",
                        PrimaryButtonText = "Confirm",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result != ContentDialogResult.Primary)
                        return;

                    PurchaseOrderService.MarkAsReceived(po.Id);
                    LoadPurchaseOrders();
                    ShowErrorDialog("Success", "Purchase order received successfully!");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error", $"Failed to receive purchase order: {ex.Message}");
                }
            }
        }

        private async void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is PurchaseOrderViewModel poVM)
            {
                try
                {
                    var po = PurchaseOrderService.GetById(poVM.Id);
                    if (po == null)
                    {
                        ShowErrorDialog("Error", "Purchase order not found");
                        return;
                    }

                    var supplier = SupplierService.GetSupplierById(po.SupplierId);
                    
                    var stack = new StackPanel { Spacing = 16, Padding = new Thickness(4) };

                    // Header with Status Badge
                    var headerGrid = new Grid();
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var idBorder = new Border
                    {
                        Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 232, 240, 254)),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12, 8, 12, 8)
                    };
                    idBorder.Child = new TextBlock
                    {
                        Text = string.Format(_resourceLoader.GetString("Common_POFormat"), po.Id),
                        FontSize = 18,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 79, 70, 229))
                    };
                    Grid.SetColumn(idBorder, 0);

                    var statusBorder = new Border
                    {
                        Background = new SolidColorBrush(ParseHexColor(poVM.StatusColor)),
                        CornerRadius = new CornerRadius(16),
                        Padding = new Thickness(12, 6, 12, 6),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    statusBorder.Child = new TextBlock
                    {
                        Text = poVM.LocalizedStatusText,
                        FontSize = 13,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
                    };
                    Grid.SetColumn(statusBorder, 2);

                    var dateBorder = new TextBlock
                    {
                        Text = po.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    Grid.SetColumn(dateBorder, 3);

                    headerGrid.Children.Add(idBorder);
                    headerGrid.Children.Add(statusBorder);
                    headerGrid.Children.Add(dateBorder);
                    stack.Children.Add(headerGrid);

                    // Supplier Information Card
                    stack.Children.Add(CreateSectionCard(
                        _resourceLoader.GetString("Dlg_PODetails_SupplierInfoTitle"),
                        "\uE77B", 
                        new[]
                        {
                            (_resourceLoader.GetString("Dlg_PODetails_SupplierNameLabel"), supplier?.Name ?? _resourceLoader.GetString("Common_Unknown")),
                            (_resourceLoader.GetString("Dlg_PODetails_SupplierPhoneLabel"), supplier?.Phone ?? _resourceLoader.GetString("Common_NA")),
                            (_resourceLoader.GetString("Dlg_PODetails_SupplierEmailLabel"), supplier?.Email ?? _resourceLoader.GetString("Common_NA")),
                            (_resourceLoader.GetString("Dlg_PODetails_SupplierAddressLabel"), supplier?.Address ?? _resourceLoader.GetString("Common_NA"))
                        }));

                    // Order Details Card
                    stack.Children.Add(CreateSectionCard(
                        _resourceLoader.GetString("Dlg_PODetails_OrderDetailsTitle"),
                        "\uE77F", 
                        new[]
                        {
                            (_resourceLoader.GetString("Dlg_PODetails_CreatedByLabel"), string.Format(_resourceLoader.GetString("Common_UserIdFormat"), po.CreatedBy)),
                            (_resourceLoader.GetString("Dlg_PODetails_TotalAmountLabel"), $"{po.TotalAmount:C0}".Replace("₫", "VND")),
                            (_resourceLoader.GetString("Dlg_PODetails_NoteLabel"), po.Note ?? _resourceLoader.GetString("Common_NoNotes"))
                        }));

                    // Order Lines Card
                    var linesCard = new Border
                    {
                        Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 249, 250, 251)),
                        BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 229, 231, 235)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(16)
                    };

                    var linesStack = new StackPanel { Spacing = 12 };
                    
                    var linesHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    linesHeader.Children.Add(new FontIcon
                    {
                        Glyph = "\uE8F1",
                        FontSize = 18,
                        Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 79, 70, 229))
                    });
                    linesHeader.Children.Add(new TextBlock
                    {
                        Text = string.Format(_resourceLoader.GetString("Dlg_PODetails_OrderLinesTitle"), po.PurchaseOrderLines.Count),
                        FontSize = 16,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    });
                    linesStack.Children.Add(linesHeader);

                    foreach (var line in po.PurchaseOrderLines)
                    {
                        var lineItemBorder = new Border
                        {
                            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 229, 231, 235)),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(6),
                            Padding = new Thickness(12),
                            Margin = new Thickness(0, 4, 0, 0)
                        };

                        var lineStack = new StackPanel { Spacing = 6 };
                        lineStack.Children.Add(new TextBlock
                        {
                            Text = $"{_resourceLoader.GetString("Dlg_PODetails_ProductIdLabel")} {line.ProductId}",
                            FontSize = 14,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        });

                        var lineInfoGrid = new Grid();
                        lineInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        lineInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        lineInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var qtyText = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) };
                        qtyText.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = _resourceLoader.GetString("Dlg_PODetails_QtyLabel") + " " });
                        qtyText.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = line.Quantity.ToString(), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                        Grid.SetColumn(qtyText, 0);

                        var unitText = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) };
                        unitText.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = _resourceLoader.GetString("Dlg_PODetails_UnitLabel") + " " });
                        unitText.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = $"{line.UnitCost:C0}".Replace("₫", "VND"), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                        Grid.SetColumn(unitText, 1);

                        var totalText = new TextBlock
                        {
                            Text = $"{line.TotalCost:C0}".Replace("₫", "VND"),
                            FontSize = 13,
                            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 79, 70, 229)),
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        Grid.SetColumn(totalText, 2);

                        lineInfoGrid.Children.Add(qtyText);
                        lineInfoGrid.Children.Add(unitText);
                        lineInfoGrid.Children.Add(totalText);

                        lineStack.Children.Add(lineInfoGrid);
                        lineItemBorder.Child = lineStack;
                        linesStack.Children.Add(lineItemBorder);
                    }

                    linesCard.Child = linesStack;
                    stack.Children.Add(linesCard);

                    var scrollViewer = new ScrollViewer
                    {
                        Content = stack,
                        MaxHeight = 600
                    };

                    var dialog = new ContentDialog
                    {
                        Title = _resourceLoader.GetString("Dlg_PODetails_Title"),
                        Content = scrollViewer,
                        CloseButtonText = _resourceLoader.GetString("Dlg_PODetails_CloseButton"),
                        XamlRoot = this.XamlRoot
                    };

                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error", $"Cannot load purchase order details: {ex.Message}");
                }
            }
        }

        private Border CreateSectionCard(string title, string icon, (string label, string value)[] items)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 249, 250, 251)),
                BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 229, 231, 235)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16)
            };

            var stack = new StackPanel { Spacing = 12 };
            
            // Header
            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            header.Children.Add(new FontIcon
            {
                Glyph = icon,
                FontSize = 18,
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 79, 70, 229))
            });
            header.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            stack.Children.Add(header);

            // Items
            var itemsStack = new StackPanel { Spacing = 8 };
            foreach (var (label, value) in items)
            {
                itemsStack.Children.Add(CreateInfoGrid(label, value));
            }
            stack.Children.Add(itemsStack);

            card.Child = stack;
            return card;
        }

        private Grid CreateInfoGrid(string label, string value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var valueBlock = new TextBlock
            {
                Text = value,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(valueBlock);

            return grid;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is PurchaseOrderViewModel po)
            {
                try
                {
                    // Only allow cancelling if not already cancelled
                    if (po.Status == PoStatus.CANCELLED)
                    {
                        ShowErrorDialog("Cannot Cancel", "Purchase order is already cancelled.");
                        return;
                    }

                    PurchaseOrderService.CancelOrder(po.Id);
                    LoadPurchaseOrders();
                    ShowErrorDialog("Success", "Purchase order cancelled successfully!");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error", $"Failed to cancel purchase order: {ex.Message}");
                }
            }
        }

        private void BtnActions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
        }

        private void ActionMenuFlyout_Opening(object sender, object e)
        {
            if (sender is MenuFlyout flyout && flyout.Target is FrameworkElement element)
            {
                var po = element.Tag as PurchaseOrderViewModel;
                if (po == null) return;

                // Find menu items - they are children of the flyout
                MenuFlyoutItem? editMenuItem = null;
                MenuFlyoutItem? receiveMenuItem = null;
                MenuFlyoutItem? cancelMenuItem = null;

                foreach (var item in flyout.Items)
                {
                    if (item is MenuFlyoutItem menuItem)
                    {
                        // Identify items by checking their Click event handlers or names
                        if (menuItem.Name == "EditMenuItem")
                            editMenuItem = menuItem;
                        else if (menuItem.Name == "ReceiveMenuItem")
                            receiveMenuItem = menuItem;
                        else if (menuItem.Name == "CancelMenuItem")
                            cancelMenuItem = menuItem;
                    }
                }

                // Control visibility based on status
                switch (po.Status)
                {
                    case PoStatus.DRAFT:
                        // DRAFT: Can edit, receive, or cancel
                        if (editMenuItem != null) editMenuItem.Visibility = Visibility.Visible;
                        if (receiveMenuItem != null) receiveMenuItem.Visibility = Visibility.Visible;
                        if (cancelMenuItem != null) cancelMenuItem.Visibility = Visibility.Visible;
                        break;

                    case PoStatus.RECEIVED:
                        // RECEIVED: Can only cancel
                        if (editMenuItem != null) editMenuItem.Visibility = Visibility.Collapsed;
                        if (receiveMenuItem != null) receiveMenuItem.Visibility = Visibility.Collapsed;
                        if (cancelMenuItem != null) cancelMenuItem.Visibility = Visibility.Visible;
                        break;

                    case PoStatus.CANCELLED:
                        // CANCELLED: No actions available except view details
                        if (editMenuItem != null) editMenuItem.Visibility = Visibility.Collapsed;
                        if (receiveMenuItem != null) receiveMenuItem.Visibility = Visibility.Collapsed;
                        if (cancelMenuItem != null) cancelMenuItem.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadPurchaseOrders();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadPurchaseOrders();
            }
        }

        private void FilterChange(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;

            CurrentPage = 1;
            LoadPurchaseOrders();
        }

        private async void SelectSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SupplierSelectorDialog(SupplierService, _selectedSupplier)
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = "Select Supplier",
                    PrimaryButtonText = "Select"
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && dialog.SelectedSupplier != null)
                {
                    _selectedSupplier = dialog.SelectedSupplier;
                    if (SelectedSupplierText != null)
                    {
                        SelectedSupplierText.Text = _selectedSupplier.Name;
                    }
                    
                    // Reload data with new filter
                    CurrentPage = 1;
                    LoadPurchaseOrders();
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error selecting supplier", ex.Message);
            }
        }

        public async void LogMessage(string message)
        {
            Debug.WriteLine(message); // vẫn in ra Output

            var dialog = new ContentDialog
            {
                Title = "Log Message",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot // cần cho WinUI 3
            };

            await dialog.ShowAsync();
        }

        private Windows.UI.Color ParseHexColor(string hexColor)
        {
            // Remove the # if present
            hexColor = hexColor.TrimStart('#');

            byte a = 255; // Default alpha
            byte r, g, b;

            if (hexColor.Length == 6)
            {
                // #RRGGBB format
                r = Convert.ToByte(hexColor.Substring(0, 2), 16);
                g = Convert.ToByte(hexColor.Substring(2, 2), 16);
                b = Convert.ToByte(hexColor.Substring(4, 2), 16);
            }
            else if (hexColor.Length == 8)
            {
                // #AARRGGBB format
                a = Convert.ToByte(hexColor.Substring(0, 2), 16);
                r = Convert.ToByte(hexColor.Substring(2, 2), 16);
                g = Convert.ToByte(hexColor.Substring(4, 2), 16);
                b = Convert.ToByte(hexColor.Substring(6, 2), 16);
            }
            else
            {
                // Invalid format, return gray
                return Windows.UI.Color.FromArgb(255, 128, 128, 128);
            }

            return Windows.UI.Color.FromArgb(a, r, g, b);
        }
    }

    public class DateOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dt)
                return dt.ToString("dd/MM/yyyy");
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal d)
                return d.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}