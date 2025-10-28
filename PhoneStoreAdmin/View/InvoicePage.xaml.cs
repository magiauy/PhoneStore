using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Implementations;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Implementations;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class InvoicePage : Page
    {
        private IInvoiceService InvoiceService => App.GetService<IInvoiceService>();
        public ObservableCollection<InvoiceViewModel> Invoices { get; set; } = new();

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;

        public InvoicePage()
        {
            this.InitializeComponent();
            this.Loaded += InvoicePage_Loaded;
        }

        private void InvoicePage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadInvoice();
        }

        private void LoadInvoice()
        {
            if (!_isInitialized)
                return;

            try
            {
                Invoices.Clear();

                // --- Lọc dữ liệu (tùy UI bạn có) ---
                InvoiceStatus? status = null;
                if (FilterStatusBoxInvoice?.SelectedItem is ComboBoxItem item && item.Tag is string tag && !string.IsNullOrEmpty(tag))
                {
                    status = Enum.Parse<InvoiceStatus>(tag);
                }

                int? customerId = null;
              

                DateTime? dateFrom = null;
                var dateOffsetFrom = FilterOrderDateFromBoxInvoice?.Date;
                if (dateOffsetFrom.HasValue && dateOffsetFrom.Value.Year > 1900)
                    dateFrom = dateOffsetFrom.Value.DateTime.Date;

                DateTime? dateTo = null;
                var dateOffsetTo = FilterOrderDateToBoxInvoice?.Date;
                if (dateOffsetTo.HasValue && dateOffsetTo.Value.Year > 1900)
                    dateTo = dateOffsetTo.Value.DateTime.Date.AddDays(1).AddSeconds(-1);

                decimal? amountMin = null;
                if (!string.IsNullOrEmpty(FilterTotalAmountMinBoxInvoice?.Text) && decimal.TryParse(FilterTotalAmountMinBoxInvoice.Text, out decimal min))
                    amountMin = min;

                decimal? amountMax = null;
                if (!string.IsNullOrEmpty(FilterTotalAmountMaxBoxInvoice?.Text) && decimal.TryParse(FilterTotalAmountMaxBoxInvoice.Text, out decimal max))
                    amountMax = max;

                int? createdBy = null;
                string? customerName = SearchBox.Text;

                // --- Gọi service để lấy dữ liệu ---
                var result = InvoiceService.GetInvoicesFiltered(
                    customerName,   // string? customerName
                    null,           // int? customerId (tạm vô hiệu hóa lọc customer)
                    createdBy,      // int? createdBy
                    status,         // InvoiceStatus? status
                             // string? note
                    dateFrom,       // DateTime? fromDate
                    dateTo,         // DateTime? toDate
                    amountMin,      // decimal? minAmount
                    amountMax,      // decimal? maxAmount
                    CurrentPage,    // int page
                    PageSize        // int pageSize
                );
                System.Diagnostics.Debug.WriteLine($"SL hóa đơn lấy được: {result?.Invoices?.Count() ?? 0}");

                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to load invoices.");
                    return;
                }

                // --- Gán dữ liệu vào ObservableCollection ---
                TotalPages = result.Info.TotalPages;
                TotalRecords = result.Info.TotalRecords;

                foreach (var inv in result.Invoices)
                {
                    Invoices.Add(inv);
                }

                // --- Cập nhật UI ---
                PageInfoText.Text = $"{CurrentPage} / {TotalPages}";
                PreviousPageButton.IsEnabled = CurrentPage > 1;
                NextPageButton.IsEnabled = CurrentPage < TotalPages;
                RecordCountText.Text = $"{Invoices.Count} / {TotalRecords}";

                UpdateEmptyStateVisibility();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load invoices", ex.Message);
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = Invoices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (InvoiceListView != null)
                InvoiceListView.Visibility = Invoices.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
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
            LoadInvoice();
        }

        private void FilterChangeDate(object sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (!_isInitialized) return;

            CurrentPage = 1;
            LoadInvoice();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchBox != null)
                    SearchBox.Text = string.Empty;
                if (FilterStatusBoxInvoice != null)
                    FilterStatusBoxInvoice.SelectedIndex = 0;
                if (FilterOrderDateFromBoxInvoice != null)
                    FilterOrderDateFromBoxInvoice.SetValue(
                        CalendarDatePicker.DateProperty, null);

                if (FilterOrderDateToBoxInvoice != null)
                    FilterOrderDateToBoxInvoice.SetValue(
                        CalendarDatePicker.DateProperty, null);

                if (FilterTotalAmountMinBoxInvoice != null)
                    FilterTotalAmountMinBoxInvoice.Text = string.Empty;
                if (FilterTotalAmountMaxBoxInvoice != null)
                    FilterTotalAmountMaxBoxInvoice.Text = string.Empty;

                if (SearchBox != null)
                    SearchBox.Text = string.Empty;

                CurrentPage = 1;
                LoadInvoice();
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
            
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void BtnActions_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadInvoice();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadInvoice();
            }
        }

        private void FilterChange(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;

            CurrentPage = 1;
            LoadInvoice();
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