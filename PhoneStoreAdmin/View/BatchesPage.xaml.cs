using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Implementations;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace PhoneStoreAdmin.View
{
    public sealed partial class BatchesPage : Page
    {
        private IBatchesService BatchesService => App.GetService<IBatchesService>();
        private ISupplierService SupplierService => App.GetService<ISupplierService>();

        public ObservableCollection<BatchViewModel> Batches { get; } = new ObservableCollection<BatchViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        public ObservableCollection<Supplier> Suppliers { get; } = new ObservableCollection<Supplier>();

        private bool _isInitialized = false;

        public BatchesPage()
        {
            this.InitializeComponent();
            this.Loaded += BatchesPage_Loaded;
        }

        private void BatchesPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadSuppliers();
            LoadBatches();
        }

        private void LoadBatches()
        {
            // Prevent execution if UI controls are not yet initialized
            if (!_isInitialized)
                return;

            try
            {
                Batches.Clear();

                int? purchaseOrderId = null;

                string? batchCode = FilterBatchCodeBox?.Text;
                DateTime? dateFrom = null;
                var dateOffsetFrom = FilterCreatedDateFromBox?.Date;
                if (dateOffsetFrom.HasValue && dateOffsetFrom.Value.Year > 1900)
                    dateFrom = dateOffsetFrom.Value.DateTime.Date;   // 00:00:00

                DateTime? dateTo = null;
                var dateOffsetTo = FilterCreatedDateToBox?.Date;
                if (dateOffsetTo.HasValue && dateOffsetTo.Value.Year > 1900)
                    dateTo = dateOffsetTo.Value.DateTime.Date.AddDays(1).AddSeconds(-1); // 23:59:59

                string? note = FilterNoteBox?.Text;

                int? supplierId = null;
                if (FilterSupplierBoxPurchaseOrder.SelectedItem is Supplier sup && sup.Id > 0)
                    supplierId = sup.Id;

                var result = BatchesService.GetBatchesFiltered(
                    purchaseOrderId,
                    supplierId,
                    batchCode,
                    dateFrom,
                    dateTo,
                    note,
                    CurrentPage,
                    PageSize);

                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to load batches.");
                    return;
                }

                TotalPages = result.Info.TotalPages;
                TotalRecords = result.Info.TotalRecords;
                foreach (var batchVM in result.Batches)
                {
                    Batches.Add(batchVM);
                }

                // Safely update UI controls with null checks
                if (PageInfoText != null)
                    PageInfoText.Text = $"{CurrentPage} / {TotalPages}";

                if (PreviousPageButton != null)
                    PreviousPageButton.IsEnabled = CurrentPage > 1;

                if (NextPageButton != null)
                    NextPageButton.IsEnabled = CurrentPage < TotalPages;

                if (RecordCountText != null)
                    RecordCountText.Text = $"{Batches.Count} / {TotalRecords}";

                UpdateEmptyStateVisibility();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load batches", ex.Message);
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = Batches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (BatchesListView != null)
                BatchesListView.Visibility = Batches.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
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
            LoadBatches();
        }

        private void FilterChangeDate(object sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (!_isInitialized) return;

            CurrentPage = 1;
            LoadBatches();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchBox != null)
                    SearchBox.Text = string.Empty;
                if (FilterBatchCodeBox != null)
                    FilterBatchCodeBox.Text = string.Empty;
                if (FilterCreatedDateFromBox != null)
                    FilterCreatedDateFromBox.SetValue(
                        CalendarDatePicker.DateProperty, null);

                if (FilterCreatedDateToBox != null)
                    FilterCreatedDateToBox.SetValue(
                        CalendarDatePicker.DateProperty, null);

                if (FilterNoteBox != null)
                    FilterNoteBox.Text = string.Empty;

                CurrentPage = 1;
                LoadBatches();
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
            ShowErrorDialog("Info", "Create batch functionality - Coming Soon!");
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is BatchViewModel batch)
            {
                ShowErrorDialog("Info", $"Edit batch ID: {batch.Id} - Coming Soon!");
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is BatchViewModel batchVM)
            {
                var batch = BatchesService.GetById(batchVM.Id);
                if (batch != null)
                {
                    var details = $"ID: {batch.id}\n" +
                                  $"Purchase Order ID: {batch.PurchaseOrderId}\n" +
                                  $"Batch Code: {batch.BatchCode ?? "N/A"}\n" +
                                  $"Created At: {batch.CreatedAt}\n" +
                                  $"Note: {batch.Note ?? "N/A"}\n\n" +
                                  "Batch Products:\n";

                    foreach (var product in batch.BatchProducts)
                    {
                        details += $" - Product ID: {product.ProductId}, Quantity: {product.Quantity}, Cost Price: {product.CostPrice}, Selling Price: {product.SellingPrice}\n";
                    }

                    ShowErrorDialog("Batch Details", details);
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is BatchViewModel batch)
            {
                try
                {
                    BatchesService.Delete(batch.Id);
                    LoadBatches();
                    ShowErrorDialog("Success", "Batch deleted successfully!");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error", $"Failed to delete batch: {ex.Message}");
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
                LoadBatches();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadBatches();
            }
        }

        private void FilterChange(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;

            CurrentPage = 1;
            LoadBatches();
        }

        private void LoadSuppliers()
        {
            Suppliers.Clear();
            var list = SupplierService.GetSelectBox();
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
}