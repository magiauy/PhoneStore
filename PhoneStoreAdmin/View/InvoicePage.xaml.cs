using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Implementations;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStore.Services.Implementations;
using PhoneStore.Services.Interfaces;
using PhoneStoreAdmin.View.Controls;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PhoneStoreAdmin.View
{
    public sealed partial class InvoicePage : Page
    {
        private IInvoiceService InvoiceService => App.GetService<IInvoiceService>();
        public ObservableCollection<InvoiceViewModel> Invoices { get; set; } = new();

        // Permission properties
        public bool CanAddInvoice { get; }
        public bool CanEditInvoice { get; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;
        private ContentDialog? _currentDialog;
        private readonly ResourceLoader _resourceLoader;
        private readonly ResourceLoader _resource_loader;
        public InvoicePage()
        {
            this._resourceLoader = new ResourceLoader();
            this.InitializeComponent();
            
            // Initialize permissions
            var session = UserSession.Instance;
            CanAddInvoice = session.HasPermission("INVOICE_ADD");
            CanEditInvoice = session.HasPermission("INVOICE_EDIT");
            
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
                string? keyword = string.IsNullOrWhiteSpace(SearchBox?.Text)
                    ? null
                    : SearchBox.Text.Trim();

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
                Debug.WriteLine(CurrentPage);
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
 
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // Tìm theo tên khách hàng hoặc người tạo
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

        private ContentDialog CreateContentDialog(ContentControl content, string title)
        {
            return new ContentDialog
            {
                Title = title,
                Content = content,
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };
        }

        private InvoiceDialog? _invoiceDialog;
        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (!CanAddInvoice) return;
            
            try
            {
                Debug.WriteLine("BtnCreate_Click: start. ThreadId=" + Thread.CurrentThread.ManagedThreadId);

                var invoiceDialog = new InvoiceDialog();
                await invoiceDialog.SetMode(InvoiceDialog.DialogMode.Add); // nếu SetMode async

                // Nếu cần gán XamlRoot
                invoiceDialog.XamlRoot = this.XamlRoot;

                Debug.WriteLine("About to ShowAsync invoiceDialog");
                await invoiceDialog.ShowAsync(); // HIỂN THỊ TRỰC TIẾP
                Debug.WriteLine("invoiceDialog ShowAsync completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BtnCreate_Click ERROR: " + ex);
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEditInvoice) return;
            
            try
            {
                if (sender is not MenuFlyoutItem mi || mi.Tag is not InvoiceViewModel vm)
                    return;

                int invoiceId = vm.Id;

                var invoice = InvoiceService.GetById(invoiceId);
                if (invoice == null)
                {
                    ShowErrorDialog("Error", "Invoice not found.");
                    return;
                }

                var viewModel = new InvoiceViewModel(invoice);
                var invoiceDialog = new InvoiceDialog();

                await invoiceDialog.SetMode(InvoiceDialog.DialogMode.Edit, viewModel);

                invoiceDialog.XamlRoot = this.XamlRoot;

                invoiceDialog.PrimaryButtonText = "Update";
                invoiceDialog.CloseButtonText = "Cancel";

                invoiceDialog.PrimaryButtonClick += (dlg, args) =>
                {
                    invoiceDialog.Save();
                    if (!invoiceDialog.IsValid())
                        args.Cancel = true;
                };

                await invoiceDialog.ShowAsync();
                LoadInvoice();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", ex.Message);
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is InvoiceViewModel poVM)
            {
                var po = InvoiceService.GetById(poVM.Id);
                if (po != null)
                {
                    var poView = new InvoiceViewModel(po)
                    {
                        CustomerName = po.CustomerName,
                        CreatedByName = po.CreatedByName,
                        DiscountCode = po.DiscountCode
                    };
                    var details = $"ID: {po.Id}\n" +
                                  $"Customer Name: {poView.CustomerName}\n" +
                                  $"Created By: {poView.CreatedByName}\n" +
                                  $"Promotion code: {poView.DiscountCode}\n" +
                                  $"Invoice Date: {po.InvoiceDate}\n" +
                                  $"Status: {po.Status}\n" +
                                  $"Total amount: {po.TotalAmount}\n" +
                                  $"Discount amount: {po.DiscountAmount}\n" +
                                  $"Final amount: {po.FinalAmount}\n" +
                                  $"Payment method: {po.PaymentMethod}\n" +
                                  $"Note: {po.Note ?? "N/A"}\n\n" +
                                  "Invoice Lines:\n";

                    foreach (var line in poView.InvoiceLines)
                    {
                        details += $" - Product ID: {line.Id}, Quantity: {line.Quantity}, Unit Cost: {line.UnitPrice}, Total Cost: {line.TotalPrice}\n";
                    }

                ShowErrorDialog("Invoice Details", details);
                }
            }
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not MenuFlyoutItem mi || mi.Tag is not InvoiceViewModel vm)
                    return;

                var invoice = InvoiceService.GetById(vm.Id);
                if (invoice == null)
                {
                    ShowErrorDialog(_resourceLoader.GetString("Sales_ErrorTitle"), _resourceLoader.GetString("Invoice_NotFound"));
                    return;
                }

                // Show file picker
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                
                // Get the window handle for the picker
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(((App)Application.Current).CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("PDF Document", new List<string>() { ".pdf" });
                savePicker.SuggestedFileName = $"HoaDon_{invoice.Id:D6}_{DateTime.Now:yyyyMMdd}";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    // Generate PDF
                    Utils.InvoicePdfGenerator.GenerateInvoicePdf(invoice, file.Path);
                    
                    // Open the generated PDF
                    var options = new Windows.System.LauncherOptions
                    {
                        DisplayApplicationPicker = false
                    };
                    await Windows.System.Launcher.LaunchFileAsync(file, options);
                    
                    ShowErrorDialog(_resourceLoader.GetString("Sales_NotificationTitle"), _resourceLoader.GetString("Sales_PrintSuccess"));
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog(_resourceLoader.GetString("Sales_ErrorTitle"), $"{_resourceLoader.GetString("Sales_PrintError")}: {ex.Message}");
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