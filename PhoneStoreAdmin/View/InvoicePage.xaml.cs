using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
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

        private async void BtnDetail_Click(object sender, RoutedEventArgs e)
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

                    // Build styled content
                    var detailContent = BuildInvoiceDetailContent(po, poView);

                    var detailDialog = new ContentDialog
                    {
                        Title = $"🧾 {_resourceLoader.GetString("Invoice_DetailTitle")} #{po.Id}",
                        Content = new ScrollViewer
                        {
                            Content = detailContent,
                            MaxHeight = 500,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                        },
                        CloseButtonText = _resourceLoader.GetString("DialogClose") ?? "Đóng",
                        PrimaryButtonText = _resourceLoader.GetString("Invoice_PrintButton") ?? "In hóa đơn",
                        XamlRoot = this.XamlRoot,
                        MinWidth = 500
                    };

                    var result = await detailDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        // Trigger print
                        await PrintInvoiceAsync(po);
                    }
                }
            }
        }

        private StackPanel BuildInvoiceDetailContent(Invoice invoice, InvoiceViewModel viewModel)
        {
            var content = new StackPanel { Spacing = 16, Padding = new Thickness(0, 0, 16, 0) };
            var productRepo = App.GetService<IProductRepository>();

            // Status badge
            var statusBadge = new Border
            {
                Background = GetStatusBrush(invoice.Status),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 6, 12, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            statusBadge.Child = new TextBlock
            {
                Text = GetStatusText(invoice.Status),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 12
            };
            content.Children.Add(statusBadge);

            // Invoice Info Card
            var infoCard = CreateInfoCard(_resourceLoader.GetString("Invoice_InfoTitle") ?? "Thông tin hóa đơn");
            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddInfoItem(infoGrid, 0, 0, "📅 " + (_resourceLoader.GetString("Invoice_Date") ?? "Ngày tạo"), invoice.InvoiceDate.ToString("dd/MM/yyyy HH:mm"));
            AddInfoItem(infoGrid, 0, 1, "💳 " + (_resourceLoader.GetString("Invoice_PaymentMethod") ?? "Thanh toán"), GetPaymentMethodText(invoice.PaymentMethod));
            AddInfoItem(infoGrid, 1, 0, "👤 " + (_resourceLoader.GetString("Invoice_Customer") ?? "Khách hàng"), viewModel.CustomerName);
            AddInfoItem(infoGrid, 1, 1, "🏷️ " + (_resourceLoader.GetString("Invoice_PromotionCode") ?? "Mã KM"), viewModel.DiscountCode ?? "-");
            AddInfoItem(infoGrid, 2, 0, "👨‍💼 " + (_resourceLoader.GetString("Invoice_CreatedBy") ?? "Nhân viên"), viewModel.CreatedByName);

            ((StackPanel)infoCard.Child).Children.Add(infoGrid);
            content.Children.Add(infoCard);

            // Products Card
            var productsCard = CreateInfoCard(_resourceLoader.GetString("Invoice_ProductsTitle") ?? "Sản phẩm");
            var productsStack = (StackPanel)productsCard.Child;

            // Products header
            var headerGrid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Product name
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star) }); // Qty
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Unit price
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Total

            AddHeaderCell(headerGrid, 0, _resourceLoader.GetString("Invoice_ProductName") ?? "Sản phẩm");
            AddHeaderCell(headerGrid, 1, _resourceLoader.GetString("Invoice_Qty") ?? "SL");
            AddHeaderCell(headerGrid, 2, _resourceLoader.GetString("Invoice_UnitPrice") ?? "Đơn giá");
            AddHeaderCell(headerGrid, 3, _resourceLoader.GetString("Invoice_LineTotal") ?? "Thành tiền");

            productsStack.Children.Add(headerGrid);

            // Divider
            productsStack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Products list
            foreach (var line in viewModel.InvoiceLines)
            {
                var productName = "Sản phẩm #" + line.ProductId;
                if (productRepo != null)
                {
                    try
                    {
                        var product = productRepo.GetById(line.ProductId);
                        productName = product?.Name ?? productName;
                    }
                    catch { }
                }

                var rowGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                AddProductCell(rowGrid, 0, productName, HorizontalAlignment.Left);
                AddProductCell(rowGrid, 1, line.Quantity.ToString(), HorizontalAlignment.Center);
                AddProductCell(rowGrid, 2, FormatCurrency(line.UnitPrice), HorizontalAlignment.Right);
                AddProductCell(rowGrid, 3, FormatCurrency(line.TotalPrice), HorizontalAlignment.Right);

                productsStack.Children.Add(rowGrid);
            }

            content.Children.Add(productsCard);

            // Totals Card
            var totalsCard = CreateInfoCard(_resourceLoader.GetString("Invoice_TotalsTitle") ?? "Tổng cộng");
            var totalsStack = (StackPanel)totalsCard.Child;

            AddTotalRow(totalsStack, _resourceLoader.GetString("Invoice_Subtotal") ?? "Tạm tính", FormatCurrency(invoice.TotalAmount), false);
            
            if (invoice.DiscountAmount > 0)
            {
                AddTotalRow(totalsStack, _resourceLoader.GetString("Invoice_Discount") ?? "Giảm giá", $"-{FormatCurrency(invoice.DiscountAmount)}", false, Windows.UI.Color.FromArgb(255, 220, 53, 69));
            }

            // Final total with highlight
            var finalTotalBorder = new Border
            {
                Background = (Brush)Application.Current.Resources["BrushPrimary"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var finalTotalGrid = new Grid();
            finalTotalGrid.Children.Add(new TextBlock
            {
                Text = _resourceLoader.GetString("Invoice_FinalTotal") ?? "TỔNG THANH TOÁN",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            });
            finalTotalGrid.Children.Add(new TextBlock
            {
                Text = FormatCurrency(invoice.FinalAmount),
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 16,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            });
            finalTotalBorder.Child = finalTotalGrid;
            totalsStack.Children.Add(finalTotalBorder);

            content.Children.Add(totalsCard);

            // Note section if exists
            if (!string.IsNullOrWhiteSpace(invoice.Note))
            {
                var noteCard = CreateInfoCard(_resourceLoader.GetString("Invoice_Note") ?? "Ghi chú");
                var noteStack = (StackPanel)noteCard.Child;
                noteStack.Children.Add(new TextBlock
                {
                    Text = invoice.Note,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    FontStyle = Windows.UI.Text.FontStyle.Italic
                });
                content.Children.Add(noteCard);
            }

            return content;
        }

        private Border CreateInfoCard(string title)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 248, 249, 250)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 0)
            };

            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14,
                Foreground = (Brush)Application.Current.Resources["BrushPrimary"]
            });

            card.Child = stack;
            return card;
        }

        private void AddInfoItem(Grid grid, int row, int col, string label, string value)
        {
            var stack = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            });
            stack.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap
            });

            Grid.SetRow(stack, row);
            Grid.SetColumn(stack, col);
            grid.Children.Add(stack);
        }

        private void AddHeaderCell(Grid grid, int col, string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };
            if (col > 0) tb.HorizontalAlignment = col == 1 ? HorizontalAlignment.Center : HorizontalAlignment.Right;
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private void AddProductCell(Grid grid, int col, string text, HorizontalAlignment align)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                HorizontalAlignment = align,
                TextWrapping = col == 0 ? TextWrapping.Wrap : TextWrapping.NoWrap
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private void AddTotalRow(StackPanel container, string label, string value, bool isBold, Windows.UI.Color? valueColor = null)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = isBold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal
            });
            grid.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 13,
                FontWeight = isBold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = valueColor.HasValue ? new SolidColorBrush(valueColor.Value) : null
            });
            container.Children.Add(grid);
        }

        private SolidColorBrush GetStatusBrush(PhoneStoreRepository.Models.Enums.InvoiceStatus status)
        {
            return status switch
            {
                PhoneStoreRepository.Models.Enums.InvoiceStatus.PAID => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 40, 167, 69)),
                PhoneStoreRepository.Models.Enums.InvoiceStatus.UNPAID => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 193, 7)),
                PhoneStoreRepository.Models.Enums.InvoiceStatus.CANCELLED => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 53, 69)),
                _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };
        }

        private string GetStatusText(PhoneStoreRepository.Models.Enums.InvoiceStatus status)
        {
            return status switch
            {
                PhoneStoreRepository.Models.Enums.InvoiceStatus.PAID => _resourceLoader.GetString("InvoiceStatus_Paid") ?? "Đã thanh toán",
                PhoneStoreRepository.Models.Enums.InvoiceStatus.UNPAID => _resourceLoader.GetString("InvoiceStatus_Unpaid") ?? "Chưa thanh toán",
                PhoneStoreRepository.Models.Enums.InvoiceStatus.CANCELLED => _resourceLoader.GetString("InvoiceStatus_Cancelled") ?? "Đã hủy",
                _ => "Unknown"
            };
        }

        private string GetPaymentMethodText(PhoneStoreRepository.Models.Enums.PaymentMethod method)
        {
            return method switch
            {
                PhoneStoreRepository.Models.Enums.PaymentMethod.CASH => _resourceLoader.GetString("PaymentMethod_Cash") ?? "Tiền mặt",
                PhoneStoreRepository.Models.Enums.PaymentMethod.CARD => _resourceLoader.GetString("PaymentMethod_Card") ?? "Thẻ",
                PhoneStoreRepository.Models.Enums.PaymentMethod.BANK => _resourceLoader.GetString("PaymentMethod_Bank") ?? "Chuyển khoản",
                PhoneStoreRepository.Models.Enums.PaymentMethod.EWALLET => _resourceLoader.GetString("PaymentMethod_EWallet") ?? "Ví điện tử",
                _ => "Khác"
            };
        }

        private string FormatCurrency(decimal amount)
        {
            return string.Format(new System.Globalization.CultureInfo("vi-VN"), "{0:N0} ₫", amount);
        }

        private async Task PrintInvoiceAsync(Invoice invoice)
        {
            try
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(((App)Application.Current).CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("PDF Document", new List<string>() { ".pdf" });
                savePicker.SuggestedFileName = $"HoaDon_{invoice.Id:D6}_{DateTime.Now:yyyyMMdd}";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    Utils.InvoicePdfGenerator.GenerateInvoicePdf(invoice, file.Path);
                    var options = new Windows.System.LauncherOptions { DisplayApplicationPicker = false };
                    await Windows.System.Launcher.LaunchFileAsync(file, options);
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog(_resourceLoader.GetString("Sales_ErrorTitle"), $"{_resourceLoader.GetString("Sales_PrintError")}: {ex.Message}");
            }
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem mi || mi.Tag is not InvoiceViewModel vm)
                return;

            var invoice = InvoiceService.GetById(vm.Id);
            if (invoice == null)
            {
                ShowErrorDialog(_resourceLoader.GetString("Sales_ErrorTitle"), _resourceLoader.GetString("Invoice_NotFound"));
                return;
            }

            await PrintInvoiceAsync(invoice);
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