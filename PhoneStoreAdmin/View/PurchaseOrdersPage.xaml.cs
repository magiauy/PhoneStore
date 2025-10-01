using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Models;
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

        public ObservableCollection<PurchaseOrderViewModel> PurchaseOrders { get; } = new ObservableCollection<PurchaseOrderViewModel>();
        public ObservableCollection<Supplier> Suppliers { get; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;

        public PurchaseOrdersPage()
        {
            this.InitializeComponent();
            this.Loaded += PurchaseOrdersPage_Loaded;
        }

        private void PurchaseOrdersPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadSuppliers();
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
                if (FilterSupplierBoxPurchaseOrder.SelectedItem is Supplier sup && sup.Id > 0)
                    supplierId = sup.Id;


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
            if (FilterSupplierBoxPurchaseOrder != null)
                FilterSupplierBoxPurchaseOrder.Text = SearchBox.Text;
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
                if (FilterSupplierBoxPurchaseOrder != null)
                    FilterSupplierBoxPurchaseOrder.SelectedIndex = 0;
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
            ShowErrorDialog("Info", "Create purchase order functionality - Coming Soon!");
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is PurchaseOrderViewModel po)
            {
                ShowErrorDialog("Info", $"Edit purchase order ID: {po.Id} - Coming Soon!");
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is PurchaseOrderViewModel poVM)
            {
                var po = PurchaseOrderService.GetById(poVM.Id);
                if (po != null)
                {
                    var poView = new PurchaseOrderViewModel(po);
                    poView.SupplierName = SupplierService.GetSupplierById(po.SupplierId)?.Name ?? string.Empty;
                    var details = $"ID: {po.Id}\n" +
                                  $"Supplier: {poView.SupplierName} ({po.SupplierId})\n" +
                                  $"Created By: {po.CreatedBy}\n" +
                                  $"Order Date: {po.OrderDate}\n" +
                                  $"Status: {poView.LocalizedStatusText}\n" +
                                  $"Total Amount: {po.TotalAmount}\n" +
                                  $"Note: {po.Note ?? "N/A"}\n\n" +
                                  "Purchase Order Lines:\n";

                    foreach (var line in poView.PurchaseOrderLines)
                    {
                        details += $" - Product ID: {line.ProductId}, Quantity: {line.Quantity}, Unit Cost: {line.UnitCost}, Total Cost: {line.TotalCost}\n";
                    }

                    ShowErrorDialog("Purchase Order Details", details);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is PurchaseOrderViewModel po)
            {
                try
                {
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

        private void LoadSuppliers()
        {
            Suppliers.Clear();
            var list = SupplierService.GetAll();
            foreach (var s in list)
                Suppliers.Add(s);

            // Thêm item "Tất cả"
            Suppliers.Insert(0, new Supplier { Name = "All suppliers" });
            FilterSupplierBoxPurchaseOrder.SelectedIndex = 0;
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