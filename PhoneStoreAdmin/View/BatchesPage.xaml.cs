using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStore.Services.Implementations;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Windows.ApplicationModel.Resources;

namespace PhoneStoreAdmin.View
{
    public sealed partial class BatchesPage : Page
    {
        private IBatchesService BatchesService => App.GetService<IBatchesService>();
        private ISupplierService SupplierService => App.GetService<ISupplierService>();
        private readonly ResourceLoader _resourceLoader;

        public ObservableCollection<BatchViewModel> Batches { get; } = new ObservableCollection<BatchViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;
        private Supplier? _selectedSupplier = null;

        public BatchesPage()
        {
            this.InitializeComponent();
            _resourceLoader = new ResourceLoader();
            this.Loaded += BatchesPage_Loaded;
        }

        private void BatchesPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
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

                string? batchCode = SearchBox?.Text;
                DateTime? dateFrom = null;
                var dateOffsetFrom = FilterCreatedDateFromBox?.Date;
                if (dateOffsetFrom.HasValue && dateOffsetFrom.Value.Year > 1900)
                    dateFrom = dateOffsetFrom.Value.DateTime.Date;   // 00:00:00

                DateTime? dateTo = null;
                var dateOffsetTo = FilterCreatedDateToBox?.Date;
                if (dateOffsetTo.HasValue && dateOffsetTo.Value.Year > 1900)
                    dateTo = dateOffsetTo.Value.DateTime.Date.AddDays(1).AddSeconds(-1); // 23:59:59

                string? note = FilterNoteBox?.Text;

                int? supplierId = _selectedSupplier?.Id;

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

        private async void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is BatchViewModel batchVM)
            {
                try
                {
                    var batch = BatchesService.GetById(batchVM.Id);
                    if (batch == null)
                    {
                        ShowErrorDialog("Error", "Batch not found");
                        return;
                    }

                    var batchView = new BatchViewModel(batch);
                    
                    // Get related data
                    var purchaseOrder = App.GetService<IPurchaseOrderService>().GetById(batch.PurchaseOrderId);
                    if (purchaseOrder != null)
                    {
                        var supplier = SupplierService.GetSupplierById(purchaseOrder.SupplierId);
                        batchView.SupplierName = supplier?.Name ?? _resourceLoader.GetString("Common_Unknown");
                        batchView.PurchaseOrderOrderDate = purchaseOrder.OrderDate;
                    }

                    // Build formatted details
                    var stack = new StackPanel { Spacing = 12 };

                    // Header
                    stack.Children.Add(new TextBlock
                    {
                        Text = $"{_resourceLoader.GetString("Dlg_BatchDetails_BatchLabel")} {batch.BatchCode ?? _resourceLoader.GetString("Common_NA")}",
                        FontSize = 20,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold
                    });

                    // Supplier
                    stack.Children.Add(CreateInfoGrid(
                        _resourceLoader.GetString("Dlg_BatchDetails_SupplierLabel"), 
                        batchView.SupplierName ?? _resourceLoader.GetString("Common_NA")));
                    
                    // Batch ID
                    stack.Children.Add(CreateInfoGrid(
                        _resourceLoader.GetString("Dlg_BatchDetails_BatchIdLabel"), 
                        string.Format(_resourceLoader.GetString("Common_BatchIdFormat"), batch.id)));
                    
                    // Purchase Order
                    stack.Children.Add(CreateInfoGrid(
                        _resourceLoader.GetString("Dlg_BatchDetails_PurchaseOrderLabel"), 
                        string.Format(_resourceLoader.GetString("Common_POFormat"), batch.PurchaseOrderId)));
                    
                    // Created At
                    stack.Children.Add(CreateInfoGrid(
                        _resourceLoader.GetString("Dlg_BatchDetails_CreatedAtLabel"), 
                        batch.CreatedAt.ToString("dd/MM/yyyy HH:mm")));
                    
                    // PO Date
                    if (batchView.PurchaseOrderOrderDate != DateTime.MinValue)
                    {
                        stack.Children.Add(CreateInfoGrid(
                            _resourceLoader.GetString("Dlg_BatchDetails_PODateLabel"), 
                            batchView.PurchaseOrderOrderDate.ToString("dd/MM/yyyy")));
                    }
                    
                    // Note
                    stack.Children.Add(CreateInfoGrid(
                        _resourceLoader.GetString("Dlg_BatchDetails_NoteLabel"), 
                        batch.Note ?? _resourceLoader.GetString("Common_NA")));

                    // Batch Products Header
                    stack.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Microsoft.UI.Colors.Gray), Margin = new Thickness(0, 8, 0, 8) });
                    stack.Children.Add(new TextBlock
                    {
                        Text = string.Format(_resourceLoader.GetString("Dlg_BatchDetails_BatchProductsHeader"), batch.BatchProducts.Count),
                        FontSize = 16,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    });

                    // Batch Products
                    foreach (var product in batch.BatchProducts)
                    {
                        var productStack = new StackPanel { Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
                        productStack.Children.Add(new TextBlock
                        {
                            Text = $"{_resourceLoader.GetString("Dlg_BatchDetails_ProductIdLabel")} {product.ProductId}",
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        });
                        productStack.Children.Add(new TextBlock { 
                            Text = $"{_resourceLoader.GetString("Dlg_BatchDetails_QuantityLabel")} {product.Quantity}" 
                        });
                        productStack.Children.Add(new TextBlock { 
                            Text = $"{_resourceLoader.GetString("Dlg_BatchDetails_CostPriceLabel")} {product.CostPrice:C0}".Replace("₫", "VND") 
                        });
                        productStack.Children.Add(new TextBlock
                        {
                            Text = $"{_resourceLoader.GetString("Dlg_BatchDetails_SellingPriceLabel")} {product.SellingPrice:C0}".Replace("₫", "VND"),
                            FontWeight = Microsoft.UI.Text.FontWeights.Bold
                        });
                        stack.Children.Add(productStack);
                    }

                    var scrollViewer = new ScrollViewer
                    {
                        Content = stack,
                        MaxHeight = 600
                    };

                    var dialog = new ContentDialog
                    {
                        Title = _resourceLoader.GetString("Dlg_BatchDetails_Title"),
                        Content = scrollViewer,
                        CloseButtonText = _resourceLoader.GetString("Dlg_BatchDetails_CloseButton"),
                        XamlRoot = this.XamlRoot
                    };

                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error", $"Cannot load batch details: {ex.Message}");
                }
            }
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

        private async void SelectSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Controls.SupplierSelectorDialog(SupplierService, _selectedSupplier)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _selectedSupplier = dialog.SelectedSupplier;
                UpdateSupplierUI();
                CurrentPage = 1;
                LoadBatches();
            }
        }

        private void UpdateSupplierUI()
        {
            if (_selectedSupplier != null)
            {
                // Show chip with supplier name
                SelectedSupplierText.Text = _selectedSupplier.Name;
            }
            else
            {
                // Show default text
                SelectedSupplierText.Text = "All Suppliers";
            }
        }

        private void ClearSupplierFilter_Click(object sender, RoutedEventArgs e)
        {
            _selectedSupplier = null;
            UpdateSupplierUI();
            CurrentPage = 1;
            LoadBatches();
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