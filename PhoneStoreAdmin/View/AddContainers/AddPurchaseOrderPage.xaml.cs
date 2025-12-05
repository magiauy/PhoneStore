using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStoreAdmin.View.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class AddPurchaseOrderPage : Page, INotifyPropertyChanged
    {
        // Services
        private IProductRepository ProductRepository => App.GetService<IProductRepository>();
        private ISupplierService SupplierService => App.GetService<ISupplierService>();
        private IPurchaseOrderService PurchaseOrderService => App.GetService<IPurchaseOrderService>();
        private IProductCategoryRepository CategoryRepository => App.GetService<IProductCategoryRepository>();
        private IBrandRepository BrandRepository => App.GetService<IBrandRepository>();
        private IProductSerialRepository ProductSerialRepository => App.GetService<IProductSerialRepository>();
        private IBatchesRepository BatchesRepository => App.GetService<IBatchesRepository>();
        private ISettingStringService SettingStringService => App.GetService<ISettingStringService>();

        // Fields
        private int? _editingPurchaseOrderId = null; // For edit mode
        private PurchaseOrder? _originalPurchaseOrder = null; // For edit mode
        private Supplier? _selectedSupplier;
        private string _searchText = string.Empty;
        private string _note = string.Empty;
        private DateTime _orderDate = DateTime.Now;
        private decimal _totalAmount = 0;
        private decimal _minimumProfitMargin = 0.20m; // Default 20%
        private readonly ResourceLoader _resourceLoader;

        // Collections
        public ObservableCollection<PurchaseOrderProductItem> AllProducts { get; set; }
        public ObservableCollection<PurchaseOrderProductItem> FilteredProducts { get; set; }
        public ObservableCollection<PurchaseOrderLineItem> PurchaseOrderItems { get; set; }
        public ObservableCollection<string> Categories { get; set; }
        public ObservableCollection<string> Brands { get; set; }

        // Properties for binding
        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                SetProperty(ref _selectedSupplier, value);
            }
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        public DateTime OrderDate
        {
            get => _orderDate;
            set => SetProperty(ref _orderDate, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public bool HasPurchaseOrderItems => PurchaseOrderItems?.Count > 0 && SelectedSupplier != null;

        public bool IsEditMode => _editingPurchaseOrderId.HasValue;

        public string SaveButtonText => IsEditMode
            ? _resourceLoader.GetString("UpdatePurchaseOrderButton")
            : _resourceLoader.GetString("CreatePurchaseOrderButton");

        /// <summary>
        /// Minimum profit margin from system settings (percentage, e.g., 0.20 = 20%)
        /// </summary>
        public decimal MinimumProfitMargin => _minimumProfitMargin;

        // Constructor
        public AddPurchaseOrderPage()
        {
            _resourceLoader = new ResourceLoader();
            this.InitializeComponent();

            // Initialize collections
            AllProducts = new ObservableCollection<PurchaseOrderProductItem>();
            FilteredProducts = new ObservableCollection<PurchaseOrderProductItem>();
            PurchaseOrderItems = new ObservableCollection<PurchaseOrderLineItem>();
            Categories = new ObservableCollection<string>();
            Brands = new ObservableCollection<string>();

            // Load minimum profit margin from settings
            LoadMinimumProfitMargin();

            PurchaseOrderIdLabel.Text = _resourceLoader.GetString("PurchaseOrderIdLabel") + (PurchaseOrderService.CountAll() + 1).ToString();
            // Subscribe to collection changes
            PurchaseOrderItems.CollectionChanged += (s, e) =>
            {
                CalculatePurchaseOrderTotal();
                OnPropertyChanged(nameof(HasPurchaseOrderItems));
            };
        }

        private void LoadMinimumProfitMargin()
        {
            try
            {
                var profitMarginStr = SettingStringService.GetValue(SystemSettingCode.PROFIT_MARGIN, "0.20");
                if (decimal.TryParse(profitMarginStr, System.Globalization.NumberStyles.Number, 
                    System.Globalization.CultureInfo.InvariantCulture, out var profitMargin))
                {
                    _minimumProfitMargin = profitMargin;
                    Logger.Info($"Loaded minimum profit margin from settings: {_minimumProfitMargin:P0}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load minimum profit margin from settings, using default 20%: {ex.Message}");
                _minimumProfitMargin = 0.20m;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Check if navigated with a purchase order ID for editing
            if (e.Parameter is int purchaseOrderId)
            {
                _editingPurchaseOrderId = purchaseOrderId;
                LoadPurchaseOrderForEdit();
            }

            LoadData();
        }

        private void LoadPurchaseOrderForEdit()
        {
            if (!_editingPurchaseOrderId.HasValue) return;

            try
            {
                _originalPurchaseOrder = PurchaseOrderService.GetById(_editingPurchaseOrderId.Value);

                if (_originalPurchaseOrder == null)
                {
                    // Don't show dialog here - will show after page is loaded
                    Logger.Error("Purchase order not found for editing");
                    Frame.GoBack();
                    return;
                }

                // Check if purchase order can be edited (only DRAFT status)
                if (_originalPurchaseOrder.Status != PoStatus.DRAFT)
                {
                    // Don't show dialog here - will show after page is loaded
                    Logger.Warning($"Cannot edit purchase order {_originalPurchaseOrder.Id} - status is {_originalPurchaseOrder.Status}");
                    Frame.GoBack();
                    return;
                }

                // Update UI title
                PurchaseOrderIdLabel.Text = $"Edit {_resourceLoader.GetString("PurchaseOrderIdLabel")} #{_originalPurchaseOrder.Id}";

                // Load supplier
                _selectedSupplier = SupplierService.GetSupplierById(_originalPurchaseOrder.SupplierId);
                if (_selectedSupplier != null)
                {
                    SelectedSupplier = _selectedSupplier;
                }

                // Load other fields
                OrderDate = _originalPurchaseOrder.OrderDate;
                Note = _originalPurchaseOrder.Note ?? string.Empty;

                // Notify UI properties changed
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(SaveButtonText));

                // Load purchase order lines
                PurchaseOrderItems.Clear();

                foreach (var line in _originalPurchaseOrder.PurchaseOrderLines)
                {
                    var product = ProductRepository.GetById(line.ProductId);
                    if (product != null)
                    {
                        var item = new PurchaseOrderLineItem
                        {
                            ProductId = line.ProductId,
                            ProductName = product.Name,
                            UnitCost = line.UnitCost,
                            Quantity = line.Quantity,
                            ProfitMargin = line.ProfitMargin > 0 ? line.ProfitMargin : _minimumProfitMargin,
                            IsSerialTracked = product.IsSerialTracked,
                            ParentPage = this
                        };

                        // Load serial entries if product is serial tracked
                        if (product.IsSerialTracked)
                        {
                            // Get serials by purchase_order_line_id (for DRAFT status with batch_id = NULL)
                            var allProductSerials = ProductSerialRepository.GetByProductId(line.ProductId);
                            var serials = allProductSerials
                                .Where(s => s.PurchaseOrderLineId == line.Id)
                                .ToList();

                            int index = 1;
                            foreach (var serial in serials)
                            {
                                var serialEntry = new Controls.SerialEntry
                                {
                                    Index = index++,
                                    SerialNumber = serial.SerialNumber ?? string.Empty
                                };
                                item.SerialEntries.Add(serialEntry);
                            }
                        }

                        PurchaseOrderItems.Add(item);
                    }
                }

                CalculatePurchaseOrderTotal();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load purchase order for edit: {ex.Message}", ex);
                Frame.GoBack();
            }
        }

        // Event Handlers
        public void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                SearchText = textBox.Text;
                FilterProducts();
            }
        }

        public void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterProducts();
        }

        public void OnBrandSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterProducts();
        }

        public async void OnSelectSupplier(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SupplierSelectorDialog(SupplierService, SelectedSupplier)
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = _resourceLoader.GetString("SelectedSupplierName"),
                    PrimaryButtonText = _resourceLoader.GetString("Select"),
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && dialog.SelectedSupplier != null)
                {
                    SelectedSupplier = dialog.SelectedSupplier;
                    OnPropertyChanged(nameof(HasPurchaseOrderItems));
                }
            }
            catch (Exception ex)
            {
                await ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderErrorTitle"),
                    $"{_resourceLoader.GetString("ErrorSelectSupplier")}: {ex.Message}");
            }
        }

        public async void OnCreatePurchaseOrder(object sender, RoutedEventArgs e)
        {
            if (!HasPurchaseOrderItems)
            {
                await ShowMessageDialog(
                    _resourceLoader.GetString("NotificationTitle"),
                    _resourceLoader.GetString("PleaseSelectSupplierAndProducts"));
                return;
            }

            if (SelectedSupplier == null)
            {
                await ShowMessageDialog(
                    _resourceLoader.GetString("NotificationTitle"),
                    _resourceLoader.GetString("PleaseSelectSupplier"));
                return;
            }

            // VALIDATION: Check if all serial-tracked products have complete serial information
            var incompleteSerialItems = new List<string>();
            foreach (var item in PurchaseOrderItems.Where(i => i.IsSerialTracked))
            {
                // Check if the number of serial entries matches the quantity
                if (item.SerialEntries.Count != item.Quantity)
                {
                    incompleteSerialItems.Add($"{item.ProductName}: Has {item.Quantity} products but only {item.SerialEntries.Count} serials");
                    continue;
                }

                // Check if all serial entries have Serial Number (no IMEI required anymore)
                var missingInfo = item.SerialEntries.Where(s =>
                    string.IsNullOrWhiteSpace(s.SerialNumber)).ToList();

                if (missingInfo.Any())
                {
                    incompleteSerialItems.Add($"{item.ProductName}: Missing Serial Number for {missingInfo.Count} product(s)");
                }
            }

            // If there are incomplete serial items, show error and don't save
            if (incompleteSerialItems.Any())
            {
                var errorMessage = "Please enter Serial Number for the following products:\n\n" +
                    string.Join("\n", incompleteSerialItems);

                await ShowMessageDialog(
                    "Incomplete Information",
                    errorMessage);
                return;
            }

            // VALIDATION: Check profit margin >= minimum from system settings
            var invalidProfitMarginItems = PurchaseOrderItems
                .Where(item => item.ProfitMargin < _minimumProfitMargin)
                .Select(item => $"{item.ProductName}: {item.ProfitMargin:P0} (minimum: {_minimumProfitMargin:P0})")
                .ToList();

            if (invalidProfitMarginItems.Any())
            {
                var errorMessage = $"Profit margin must be >= {_minimumProfitMargin:P0} (system setting):\n\n" +
                    string.Join("\n", invalidProfitMarginItems);

                await ShowMessageDialog(
                    "Invalid Profit Margin",
                    errorMessage);
                return;
            }

            try
            {
                // Show notes input dialog (for both creating and editing)
                string notes = Note;
                var notesDialog = new Controls.NotesInputDialog(Note)
                {
                    Title = _resourceLoader.GetString("NoteTextBoxTitle"),
                    XamlRoot = this.XamlRoot
                };

                var dialogResult = await notesDialog.ShowAsync();

                // If user cancelled, return
                if (dialogResult != ContentDialogResult.Primary)
                {
                    return;
                }

                // Get notes from dialog
                notes = notesDialog.Notes;

                // Create or Update PurchaseOrder object
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierId = SelectedSupplier.Id,
                    CreatedBy = _editingPurchaseOrderId.HasValue ? _originalPurchaseOrder!.CreatedBy : 1, // TODO: Get from current user session
                    OrderDate = OrderDate,
                    Status = PoStatus.DRAFT,
                    TotalAmount = TotalAmount,
                    Note = string.IsNullOrWhiteSpace(notes) ? null : notes
                };

                // If editing, set the ID
                if (_editingPurchaseOrderId.HasValue)
                {
                    purchaseOrder.Id = _editingPurchaseOrderId.Value;
                }

                // Add purchase order lines
                foreach (var item in PurchaseOrderItems)
                {
                    var line = new PurchaseOrderLine
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        TotalCost = item.TotalCost,
                        ProfitMargin = item.ProfitMargin
                    };
                    purchaseOrder.PurchaseOrderLines.Add(line);
                }

                // Prepare serials dictionary (lineIndex -> serials list)
                var serialsByLineIndex = new Dictionary<int, List<ProductSerial>>();
                for (int i = 0; i < PurchaseOrderItems.Count; i++)
                {
                    var item = PurchaseOrderItems[i];
                    if (item.IsSerialTracked && item.SerialEntries.Count > 0)
                    {
                        var serials = new List<ProductSerial>();
                        foreach (var entry in item.SerialEntries)
                        {
                            var serial = new ProductSerial
                            {
                                ProductId = item.ProductId,
                                SerialNumber = entry.SerialNumber,
                                Imei1 = null, // No longer required
                                Imei2 = null, // No longer required
                                Status = SerialStatus.RESERVED, // Will be set by service
                                Note = null
                            };
                            serials.Add(serial);
                        }
                        serialsByLineIndex[i] = serials;
                    }
                }

                // Save or Update to database WITH SERIALS
                if (_editingPurchaseOrderId.HasValue)
                {
                    // For update, delete old serials first
                    var existingSerials = ProductSerialRepository.GetAll()
                         .Where(s => s.PurchaseOrderLineId.HasValue &&
                        purchaseOrder.PurchaseOrderLines.Any(l => l.Id == s.PurchaseOrderLineId.Value))
                          .ToList();
                    foreach (var serial in existingSerials)
                    {
                        ProductSerialRepository.Delete(serial.Id);
                    }

                    PurchaseOrderService.Update(purchaseOrder);

                    // Add new serials after update
                    foreach (var kvp in serialsByLineIndex)
                    {
                        var lineIndex = kvp.Key;
                        var line = purchaseOrder.PurchaseOrderLines.ElementAtOrDefault(lineIndex);
                        if (line != null)
                        {
                            PurchaseOrderService.AddProductSerials(purchaseOrder.Id, line.Id, kvp.Value);
                        }
                    }
                }
                else
                {
                    // For new PO, pass serials directly to Insert
                    PurchaseOrderService.Insert(purchaseOrder, serialsByLineIndex);
                }

                // Remove the old batch-dependent serial saving logic
                // Serial numbers are now saved in Insert() with status = RESERVED

                var successMessage = _editingPurchaseOrderId.HasValue
                         ? _resourceLoader.GetString("AddPO_UpdateSuccessMessage")
                 : _resourceLoader.GetString("PurchaseOrderCreatedSuccessfully");

                await ShowMessageDialog(
          _resourceLoader.GetString("PurchaseOrderSuccessTitle"),
                    $"{successMessage}. {_resourceLoader.GetString("TotalAmountLabel")}: {FormatPrice(TotalAmount)}");

                // Navigate back to purchase orders list page
                Frame.Navigate(typeof(PurchaseOrdersPage));
            }
            catch (Exception ex)
            {
                var errorMessage = _editingPurchaseOrderId.HasValue
                    ? _resourceLoader.GetString("AddPO_CannotUpdateMessage")
                    : _resourceLoader.GetString("CannotCreatePurchaseOrder");

                await ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderErrorTitle"),
                    $"{errorMessage}: {ex.Message}");
            }
        }

        public void OnClearPurchaseOrder(object sender, RoutedEventArgs e)
        {
            PurchaseOrderItems.Clear();
            SelectedSupplier = null;
            Note = string.Empty;
            OrderDate = DateTime.Now;
            CalculatePurchaseOrderTotal();
        }

        public void OnCancel(object sender, RoutedEventArgs e)
        {
            // Navigate back to purchase orders page
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        public async void OnPrintPurchaseOrder(object sender, RoutedEventArgs e)
        {
            if (!HasPurchaseOrderItems || SelectedSupplier == null)
            {
                await ShowMessageDialog(
                    _resourceLoader.GetString("NotificationTitle"),
                    _resourceLoader.GetString("PleaseSelectSupplierAndProducts"));
                return;
            }

            try
            {
                Logger.Info($"Starting PDF generation. Supplier: {SelectedSupplier?.Name}, Items count: {PurchaseOrderItems?.Count}");

                // Ensure supplier is not null
                if (SelectedSupplier == null)
                {
                    Logger.Error("SelectedSupplier is null");
                    await ShowMessageDialog(
                        _resourceLoader.GetString("PurchaseOrderErrorTitle"),
                        "Supplier not selected. Please select a supplier.");
                    return;
                }

                // Create temporary purchase order object for printing
                var tempPurchaseOrder = new PurchaseOrder
                {
                    Id = _editingPurchaseOrderId ?? 0,
                    SupplierId = SelectedSupplier.Id,
                    CreatedBy = _editingPurchaseOrderId.HasValue && _originalPurchaseOrder != null
                        ? _originalPurchaseOrder.CreatedBy
                        : 1,
                    OrderDate = OrderDate,
                    Status = PoStatus.DRAFT,
                    TotalAmount = TotalAmount,
                    Note = Note ?? string.Empty
                };

                // Ensure PurchaseOrderLines collection is initialized
                if (tempPurchaseOrder.PurchaseOrderLines == null)
                {
                    tempPurchaseOrder.PurchaseOrderLines = new List<PurchaseOrderLine>();
                }

                // Add purchase order lines
                if (PurchaseOrderItems != null)
                {
                    foreach (var item in PurchaseOrderItems)
                    {
                        if (item == null) continue;

                        var line = new PurchaseOrderLine
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitCost = item.UnitCost,
                            TotalCost = item.TotalCost
                        };
                        tempPurchaseOrder.PurchaseOrderLines.Add(line);
                    }
                }

                Logger.Info($"Created temp PO with {tempPurchaseOrder.PurchaseOrderLines.Count} lines");

                // Open file save dialog
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();

                // Get window handle - use a simpler approach
                var window = GetWindowForElement(this);
                if (window == null)
                {
                    Logger.Error("Could not find window for file picker");
                    throw new InvalidOperationException("Could not find window");
                }

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("PDF Document", new List<string>() { ".pdf" });
                savePicker.SuggestedFileName = $"PurchaseOrder_{tempPurchaseOrder.Id}_{DateTime.Now:yyyyMMdd}";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    Logger.Info($"File path selected: {file.Path}");

                    // Generate PDF with null checks
                    if (tempPurchaseOrder == null)
                    {
                        Logger.Error("tempPurchaseOrder is null before PDF generation");
                        throw new InvalidOperationException("Purchase order is null");
                    }

                    if (SelectedSupplier == null)
                    {
                        Logger.Error("SelectedSupplier is null before PDF generation");
                        throw new InvalidOperationException("Supplier is null");
                    }

                    Utils.PurchaseOrderPdfGenerator.GeneratePdf(tempPurchaseOrder, SelectedSupplier, file.Path);

                    Logger.Info("PDF generated successfully");

                    // Show success message
                    var dialog = new ContentDialog
                    {
                        Title = _resourceLoader.GetString("PurchaseOrderSuccessTitle"),
                        Content = $"PDF saved successfully to:\n{file.Path}",
                        PrimaryButtonText = _resourceLoader.GetString("Common_OpenFile"),
                        CloseButtonText = _resourceLoader.GetString("CloseButton/Text"),
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    // Open the file if user clicks Open
                    if (result == ContentDialogResult.Primary)
                    {
                        await Windows.System.Launcher.LaunchFileAsync(file);
                    }
                }
                else
                {
                    Logger.Info("User cancelled file picker");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to generate PDF: {ex.Message}", ex);
                await ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderErrorTitle"),
                    $"Failed to generate PDF: {ex.Message}\n\nPlease check the logs for more details.");
            }
        }

        // Helper Methods
        public string FormatPrice(decimal price)
        {
            return price.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        }

        private void LoadData()
        {
            try
            {
                // Load categories
                Categories.Clear();
                var categories = CategoryRepository.GetAll();
                Categories.Add(_resourceLoader.GetString("CategoryComboBoxAll"));
                foreach (var category in categories)
                {
                    Categories.Add(category.Name);
                }

                // Load brands
                Brands.Clear();
                var brands = BrandRepository.GetAll();
                Brands.Add(_resourceLoader.GetString("BrandComboBoxAll"));
                foreach (var brand in brands)
                {
                    Brands.Add(brand.Name);
                }

                // Load products
                AllProducts.Clear();
                FilteredProducts.Clear();
                var products = ProductRepository.GetAll();

                foreach (var product in products.Where(p => p.Status == ProductStatus.ACTIVE))
                {
                    var category = CategoryRepository.GetById(product.CategoryId);
                    var brand = product.BrandId.HasValue ? BrandRepository.GetById(product.BrandId.Value) : null;

                    var item = new PurchaseOrderProductItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Sku = product.Sku,
                        CategoryName = category?.Name ?? "N/A",
                        BrandName = brand?.Name ?? "N/A",
                        CurrentCost = product.Cost,
                        ParentPage = this
                    };

                    AllProducts.Add(item);
                    FilteredProducts.Add(item);
                }
            }
            catch (Exception ex)
            {
                _ = ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderErrorTitle"),
                    $"{_resourceLoader.GetString("CannotLoadData")}: {ex.Message}");
            }
        }

        private void FilterProducts()
        {
            FilteredProducts.Clear();

            var filteredItems = AllProducts.AsEnumerable();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filteredItems = filteredItems.Where(p =>
                    p.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.BrandName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by category
            var categoryComboBox = this.FindName("CategoryComboBox") as ComboBox;
            if (categoryComboBox?.SelectedItem is string selectedCategory && !string.IsNullOrEmpty(selectedCategory) && categoryComboBox.SelectedIndex != 0)
            {
                filteredItems = filteredItems.Where(p => p.CategoryName == selectedCategory);
            }

            // Filter by brand
            var brandComboBox = this.FindName("BrandComboBox") as ComboBox;
            if (brandComboBox?.SelectedItem is string selectedBrand && !string.IsNullOrEmpty(selectedBrand) && brandComboBox.SelectedIndex != 0)
            {
                filteredItems = filteredItems.Where(p => p.BrandName == selectedBrand);
            }

            foreach (var item in filteredItems)
            {
                FilteredProducts.Add(item);
            }
        }

        public void CalculatePurchaseOrderTotal()
        {
            TotalAmount = PurchaseOrderItems.Sum(item => item.TotalCost);
        }

        public void RemoveProductFromPurchaseOrder(PurchaseOrderLineItem item)
        {
            PurchaseOrderItems.Remove(item);
        }

        public async System.Threading.Tasks.Task EditSerialNumbersForProduct(PurchaseOrderLineItem item)
        {
            try
            {
                // Create dialog with existing serial entries
                var serialDialog = new SerialNumberInputDialog(item.ProductName, item.Quantity, item.SerialEntries.ToList())
                {
                    XamlRoot = this.XamlRoot
                };

                var result = await serialDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Clear existing entries
                    item.SerialEntries.Clear();

                    // Add updated entries
                    foreach (var entry in serialDialog.SerialEntries)
                    {
                        item.SerialEntries.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderErrorTitle"),
                    $"{_resourceLoader.GetString("AddPO_CannotEditSerialMessage")}: {ex.Message}");
            }
        }

        public async void AddProductToPurchaseOrder(PurchaseOrderProductItem product)
        {
            try
            {
                // Get the product from repository to check if it's serial tracked
                var fullProduct = ProductRepository.GetById(product.ProductId);

                if (fullProduct == null)
                {
                    await ShowMessageDialog(
                        _resourceLoader.GetString("PurchaseOrderErrorTitle"),
                        _resourceLoader.GetString("ProductNotFoundError"));
                    return;
                }

                var existingItem = PurchaseOrderItems.FirstOrDefault(item => item.ProductId == product.ProductId);

                // STEP 1: Always ask for quantity first
                int quantityToAdd = 1;
                
                var quantityDialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("AddProductDialogTitle"),
                    PrimaryButtonText = _resourceLoader.GetString("AddProductButton"),
                    CloseButtonText = _resourceLoader.GetString("BtnCancel"),
                    XamlRoot = this.XamlRoot
                };

                var numberBox = new NumberBox
                {
                    Minimum = 1,
                    Maximum = 1000,
                    Value = 1,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    Header = _resourceLoader.GetString("AddProductQuantityLabel")
                };

                var contentPanel = new StackPanel { Spacing = 12 };
                
                if (existingItem != null)
                {
                    contentPanel.Children.Add(new TextBlock 
                    { 
                        Text = string.Format(_resourceLoader.GetString("AddProductExistingMessage"), existingItem.Quantity),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
                
                // Show if product requires serial tracking
                if (fullProduct.IsSerialTracked)
                {
                    contentPanel.Children.Add(new InfoBar
                    {
                        Severity = InfoBarSeverity.Informational,
                        IsOpen = true,
                        IsClosable = false,
                        Message = "This product requires Serial Number"
                    });
                }
                
                contentPanel.Children.Add(numberBox);
                quantityDialog.Content = contentPanel;

                var result = await quantityDialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                    return;

                quantityToAdd = (int)numberBox.Value;
                if (quantityToAdd <= 0)
                    return;

                // STEP 2: If product is serial tracked, show serial input dialog
                if (fullProduct.IsSerialTracked)
                {
                    // Show serial number input dialog
                    var serialDialog = new SerialNumberInputDialog(product.ProductName, quantityToAdd)
                    {
                        XamlRoot = this.XamlRoot
                    };

                    var dialogResult = await serialDialog.ShowAsync();

                    if (dialogResult != ContentDialogResult.Primary)
                        return;

                    // VALIDATE: Check for duplicates with existing serials in the purchase order
                    var allExistingSerials = PurchaseOrderItems
                        .SelectMany(i => i.SerialEntries)
                        .Select(e => e.SerialNumber?.Trim()?.ToLower())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToHashSet();

                    var newSerials = serialDialog.SerialEntries
                        .Select(e => e.SerialNumber?.Trim()?.ToLower())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    var duplicatesWithExisting = newSerials
                        .Where(s => allExistingSerials.Contains(s))
                        .ToList();

                    if (duplicatesWithExisting.Any())
                    {
                        await ShowMessageDialog(
                            "Duplicate Serial",
                            $"The following serials already exist in the purchase order:\n\n{string.Join(", ", duplicatesWithExisting.Select(s => s?.ToUpper()))}\n\nPlease enter different serials.");
                        return;
                    }

                    // VALIDATE: Check for duplicates with existing serials in database
                    try
                    {
                        var productSerialRepository = ProductSerialRepository;
                        foreach (var newSerial in newSerials.Distinct())
                        {
                            if (string.IsNullOrWhiteSpace(newSerial)) continue;
                            
                            // Use TryGet instead of Get to avoid exception
                            var existingSerial = productSerialRepository.TryGetBySerialNumber(newSerial);
                            if (existingSerial != null)
                            {
                                await ShowMessageDialog(
                                    "Serial Already Exists",
                                    $"Serial '{newSerial.ToUpper()}' already exists in the system.\n\nPlease enter a different serial.");
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowMessageDialog(
                            "Serial Validation Error",
                            $"Unable to validate serial in the system: {ex.Message}");
                        return;
                    }

                    if (existingItem != null)
                    {
                        // Add to existing item
                        existingItem.Quantity += quantityToAdd;

                        // Store serial entries for later
                        int startIndex = existingItem.SerialEntries.Count + 1;
                        for (int i = 0; i < serialDialog.SerialEntries.Count; i++)
                        {
                            var entry = serialDialog.SerialEntries[i];
                            entry.Index = startIndex + i;
                            existingItem.SerialEntries.Add(entry);
                        }
                    }
                    else
                    {
                        // Create new item
                        var newItem = new PurchaseOrderLineItem
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            UnitCost = product.CurrentCost,
                            Quantity = quantityToAdd,
                            ProfitMargin = _minimumProfitMargin,
                            IsSerialTracked = true,
                            ParentPage = this
                        };

                        // Store serial entries
                        foreach (var entry in serialDialog.SerialEntries)
                        {
                            newItem.SerialEntries.Add(entry);
                        }

                        PurchaseOrderItems.Add(newItem);
                    }
                }
                else
                {
                    // STEP 3: For non-serial tracked products, just add quantity
                    if (existingItem != null)
                    {
                        existingItem.Quantity += quantityToAdd;
                    }
                    else
                    {
                        var newItem = new PurchaseOrderLineItem
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            UnitCost = product.CurrentCost,
                            Quantity = 1,
                            ProfitMargin = _minimumProfitMargin,
                            IsSerialTracked = false,
                            ParentPage = this
                        };
                        PurchaseOrderItems.Add(newItem);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderErrorTitle"),
                    $"{_resourceLoader.GetString("AddProductError")}: {ex.Message}");
            }
        }

        public async void OnImportFromExcel(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ImportPurchaseOrderDialog
                {
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && dialog.ValidatedProducts.Any())
                {
                    // Add imported products to purchase order
                    foreach (var (product, quantity, serials) in dialog.ValidatedProducts)
                    {
                        var existingItem = PurchaseOrderItems.FirstOrDefault(item => item.ProductId == product.Id);

                        if (existingItem != null && product.IsSerialTracked)
                        {
                            // For serial-tracked products, add new serials
                            existingItem.Quantity += quantity;

                            int startIndex = existingItem.SerialEntries.Count + 1;
                            for (int i = 0; i < serials.Count; i++)
                            {
                                var entry = serials[i];
                                entry.Index = startIndex + i;
                                existingItem.SerialEntries.Add(entry);
                            }
                        }
                        else if (existingItem != null)
                        {
                            // For non-serial tracked, just add quantity
                            existingItem.Quantity += quantity;
                        }
                        else
                        {
                            // Create new item
                            var newItem = new PurchaseOrderLineItem
                            {
                                ProductId = product.Id,
                                ProductName = product.Name,
                                UnitCost = product.Cost,
                                Quantity = quantity,
                                ProfitMargin = _minimumProfitMargin,
                                IsSerialTracked = product.IsSerialTracked,
                                ParentPage = this
                            };

                            // Add serials if serial-tracked
                            if (product.IsSerialTracked)
                            {
                                foreach (var entry in serials)
                                {
                                    newItem.SerialEntries.Add(entry);
                                }
                            }

                            PurchaseOrderItems.Add(newItem);
                        }
                    }

                    await ShowMessageDialog(
                        "Import Successful",
                        $"Imported {dialog.ValidatedProducts.Count} products with total {dialog.ValidatedProducts.Sum(p => p.quantity)} items.");
                }
            }
            catch (Exception ex)
            {
                await ShowMessageDialog(
                    "Import Error",
                    $"Failed to import Excel: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ShowMessageDialog(string title, string content)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = title,
                Content = content,
                CloseButtonText = _resourceLoader.GetString("CloseButton/Text"),
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        #region Bulk Price Edit Methods

        /// <summary>
        /// Get distinct categories from products in the purchase order
        /// </summary>
        private List<string> GetPurchaseOrderCategories()
        {
            var categories = PurchaseOrderItems
                .Select(item => 
                {
                    var product = ProductRepository.GetById(item.ProductId);
                    if (product != null)
                    {
                        var category = CategoryRepository.GetById(product.CategoryId);
                        return category?.Name ?? "Unknown";
                    }
                    return "Unknown";
                })
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            
            return categories;
        }

        /// <summary>
        /// Get distinct brands from products in the purchase order
        /// </summary>
        private List<string> GetPurchaseOrderBrands()
        {
            var brands = PurchaseOrderItems
                .Select(item =>
                {
                    var product = ProductRepository.GetById(item.ProductId);
                    if (product?.BrandId != null)
                    {
                        var brand = BrandRepository.GetById(product.BrandId.Value);
                        return brand?.Name ?? "Unknown";
                    }
                    return "No Brand";
                })
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return brands;
        }

        /// <summary>
        /// Get product selection items from purchase order
        /// </summary>
        private List<ProductSelectionItem> GetProductSelectionItems()
        {
            return PurchaseOrderItems
                .Select(item => 
                {
                    var product = ProductRepository.GetById(item.ProductId);
                    var categoryName = "Unknown";
                    var brandName = "No Brand";
                    
                    if (product != null)
                    {
                        var category = CategoryRepository.GetById(product.CategoryId);
                        categoryName = category?.Name ?? "Unknown";
                        
                        if (product.BrandId != null)
                        {
                            var brand = BrandRepository.GetById(product.BrandId.Value);
                            brandName = brand?.Name ?? "Unknown";
                        }
                    }
                    
                    return new ProductSelectionItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        CategoryName = categoryName,
                        BrandName = brandName,
                        UnitCost = item.UnitCost
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Open the Bulk Price Edit dialog
        /// </summary>
        public async void OnOpenBulkPriceEditDialog(object sender, RoutedEventArgs e)
        {
            try
            {
                var products = GetProductSelectionItems();
                var categories = GetPurchaseOrderCategories();
                var brands = GetPurchaseOrderBrands();

                var dialog = new BulkPriceEditDialog(products, categories, brands, _minimumProfitMargin)
                {
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && dialog.Result != null)
                {
                    await ApplyBulkPriceEdit(dialog.Result);
                }
            }
            catch (Exception ex)
            {
                await ShowMessageDialog("Error", $"Failed to open bulk price edit dialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply bulk price edit based on dialog result
        /// </summary>
        private async Task ApplyBulkPriceEdit(BulkPriceEditResult editResult)
        {
            try
            {
                // Determine which products to apply to
                var targetItems = GetTargetItemsForBulkEdit(editResult);

                if (!targetItems.Any())
                {
                    await ShowMessageDialog("No Products", "No products match the selected filter.");
                    return;
                }

                var inputValue = editResult.Value;

                // Determine edit mode and apply changes
                switch (editResult.Mode)
                {
                    case BulkEditMode.ProfitMargin:
                        // Input is profit margin percentage (e.g., 25 for 25%)
                        var profitMargin = (decimal)inputValue / 100m;

                        foreach (var item in targetItems)
                        {
                            item.ProfitMargin = profitMargin;
                        }
                        break;

                    case BulkEditMode.UnitCost:
                        // Input is unit cost - set unit cost and keep profit margin
                        var unitCost = (decimal)inputValue;

                        foreach (var item in targetItems)
                        {
                            item.UnitCost = unitCost;
                            // Profit margin stays the same, selling price will be recalculated
                        }
                        break;

                    case BulkEditMode.SellingPrice:
                        // Input is selling price - calculate profit margin from unit cost
                        var sellingPrice = (decimal)inputValue;
                        var invalidItems = new List<string>();

                        foreach (var item in targetItems)
                        {
                            if (item.UnitCost <= 0)
                            {
                                invalidItems.Add($"{item.ProductName}: Unit cost is 0");
                                continue;
                            }

                            // Calculate profit margin: sellingPrice = unitCost * (1 + profitMargin)
                            // profitMargin = (sellingPrice / unitCost) - 1
                            var calculatedMargin = (sellingPrice / item.UnitCost) - 1m;

                            if (calculatedMargin < _minimumProfitMargin)
                            {
                                invalidItems.Add($"{item.ProductName}: Calculated margin {calculatedMargin:P0} < minimum {_minimumProfitMargin:P0}");
                                continue;
                            }

                            item.ProfitMargin = calculatedMargin;
                        }

                        if (invalidItems.Any())
                        {
                            await ShowMessageDialog(
                                "Some Products Skipped",
                                $"The following products were not updated:\n\n{string.Join("\n", invalidItems)}");
                        }
                        break;
                }

                // Recalculate totals
                CalculatePurchaseOrderTotal();

                await ShowMessageDialog(
                    "Success",
                    $"Updated pricing for {targetItems.Count} product(s).");
            }
            catch (Exception ex)
            {
                await ShowMessageDialog("Error", $"Failed to apply bulk price edit: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the list of items to apply bulk edit based on result
        /// </summary>
        private List<PurchaseOrderLineItem> GetTargetItemsForBulkEdit(BulkPriceEditResult editResult)
        {
            switch (editResult.Scope)
            {
                case BulkEditScope.All:
                    return PurchaseOrderItems.ToList();

                case BulkEditScope.SelectedProducts:
                    if (editResult.SelectedProductIds == null || !editResult.SelectedProductIds.Any())
                        return new List<PurchaseOrderLineItem>();
                    return PurchaseOrderItems
                        .Where(item => editResult.SelectedProductIds.Contains(item.ProductId))
                        .ToList();

                case BulkEditScope.Categories:
                    if (editResult.SelectedCategories == null || !editResult.SelectedCategories.Any())
                        return new List<PurchaseOrderLineItem>();
                    return PurchaseOrderItems.Where(item =>
                    {
                        var product = ProductRepository.GetById(item.ProductId);
                        if (product != null)
                        {
                            var category = CategoryRepository.GetById(product.CategoryId);
                            return category?.Name != null && editResult.SelectedCategories.Contains(category.Name);
                        }
                        return false;
                    }).ToList();

                case BulkEditScope.Brands:
                    if (editResult.SelectedBrands == null || !editResult.SelectedBrands.Any())
                        return new List<PurchaseOrderLineItem>();
                    return PurchaseOrderItems.Where(item =>
                    {
                        var product = ProductRepository.GetById(item.ProductId);
                        if (product?.BrandId != null)
                        {
                            var brand = BrandRepository.GetById(product.BrandId.Value);
                            return brand?.Name != null && editResult.SelectedBrands.Contains(brand.Name);
                        }
                        return editResult.SelectedBrands.Contains("No Brand") && product?.BrandId == null;
                    }).ToList();

                default:
                    return new List<PurchaseOrderLineItem>();
            }
        }

        #endregion

        // Helper method to get Window from UIElement
        private Window? GetWindowForElement(UIElement element)
        {
            // Simple approach: get MainWindow from App
            var app = Application.Current as App;
            var currentWindow = app?.CurrentWindow;

            // If current window is LoginWindow, it might have been replaced by MainWindow
            // Try to find the window that contains this element
            if (currentWindow != null && currentWindow is MainWindow)
            {
                return currentWindow;
            }

            // Fallback: return current window anyway
            return currentWindow;
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Supporting Classes
    public class PurchaseOrderProductItem : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal CurrentCost { get; set; }
        public AddPurchaseOrderPage? ParentPage { get; set; }

        public void AddToPurchaseOrder(object sender, RoutedEventArgs e)
        {
            ParentPage?.AddProductToPurchaseOrder(this);
        }

        public string FormatPrice(decimal price)
        {
            return price.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PurchaseOrderLineItem : INotifyPropertyChanged
    {
        private int _quantity = 1;
        private decimal _unitCost = 0;
        private bool _isSerialTracked = false;
        private decimal _profitMargin = 0.20m; // Default 20%, will be overwritten by page's minimum

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public AddPurchaseOrderPage? ParentPage { get; set; }
        public ObservableCollection<Controls.SerialEntry> SerialEntries { get; set; }

        public PurchaseOrderLineItem()
        {
            SerialEntries = new ObservableCollection<Controls.SerialEntry>();
            SerialEntries.CollectionChanged += (s, e) => OnPropertyChanged(nameof(SerialExpanderVisibility));
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                SetProperty(ref _quantity, value);
                OnPropertyChanged(nameof(TotalCost));
                OnPropertyChanged(nameof(CalculatedSellingPrice));
                ParentPage?.CalculatePurchaseOrderTotal();
            }
        }

        public decimal UnitCost
        {
            get => _unitCost;
            set
            {
                SetProperty(ref _unitCost, value);
                OnPropertyChanged(nameof(TotalCost));
                OnPropertyChanged(nameof(UnitCostDouble));
                OnPropertyChanged(nameof(CalculatedSellingPrice));
                OnPropertyChanged(nameof(SellingPriceDouble));
                ParentPage?.CalculatePurchaseOrderTotal();
            }
        }

        public double UnitCostDouble
        {
            get => (double)_unitCost;
            set
            {
                UnitCost = (decimal)value;
            }
        }

        public decimal ProfitMargin
        {
            get => _profitMargin;
            set
            {
                SetProperty(ref _profitMargin, value);
                OnPropertyChanged(nameof(ProfitMarginPercent));
                OnPropertyChanged(nameof(ProfitMarginPercentText));
                OnPropertyChanged(nameof(CalculatedSellingPrice));
                OnPropertyChanged(nameof(SellingPriceDouble));
            }
        }

        /// <summary>
        /// Profit margin as percentage for NumberBox (e.g., 20 for 20%)
        /// </summary>
        public double ProfitMarginPercent
        {
            get => (double)Math.Round(_profitMargin * 100, 10);
            set
            {
                ProfitMargin = Math.Round((decimal)value / 100, 10);
            }
        }

        /// <summary>
        /// Profit margin as percentage text for TextBox binding (shows full precision)
        /// </summary>
        public string ProfitMarginPercentText
        {
            get => Math.Round(_profitMargin * 100, 10).ToString(System.Globalization.CultureInfo.InvariantCulture);
            set
            {
                if (decimal.TryParse(value, System.Globalization.NumberStyles.Number, 
                    System.Globalization.CultureInfo.InvariantCulture, out var percent))
                {
                    if (percent >= 0)
                    {
                        ProfitMargin = Math.Round(percent / 100, 10);
                    }
                }
            }
        }

        /// <summary>
        /// Handler khi người dùng thay đổi text của lợi nhuận
        /// </summary>
        public void OnProfitMarginTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Parse trực tiếp mà không gọi setter để tránh format lại text
                if (decimal.TryParse(textBox.Text, System.Globalization.NumberStyles.Number, 
                    System.Globalization.CultureInfo.InvariantCulture, out var percent))
                {
                    if (percent >= 0)
                    {
                        ProfitMargin = Math.Round(percent / 100, 10);
                    }
                }
            }
        }

        /// <summary>
        /// Giá bán tính toán = UnitCost * (1 + ProfitMargin)
        /// </summary>
        public decimal CalculatedSellingPrice => Math.Round(UnitCost * (1 + ProfitMargin), 0);

        /// <summary>
        /// Giá bán dự kiến dạng double cho NumberBox binding
        /// </summary>
        public double SellingPriceDouble
        {
            get => (double)CalculatedSellingPrice;
            set
            {
                // Tính lại ProfitMargin từ giá bán mới sử dụng decimal để tránh floating-point errors
                if (UnitCost > 0 && (decimal)value >= UnitCost)
                {
                    // ProfitMargin = (SellingPrice / UnitCost) - 1
                    decimal sellingPrice = (decimal)value;
                    decimal newMargin = (sellingPrice / UnitCost) - 1m;
                    // Giữ nguyên độ chính xác cao (10 chữ số thập phân)
                    ProfitMargin = Math.Round(newMargin, 10);
                }
            }
        }

        /// <summary>
        /// Handler khi người dùng thay đổi giá bán
        /// </summary>
        public void OnSellingPriceChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (!double.IsNaN(args.NewValue) && args.NewValue >= 0)
            {
                SellingPriceDouble = args.NewValue;
            }
        }

        public bool IsSerialTracked
        {
            get => _isSerialTracked;
            set
            {
                SetProperty(ref _isSerialTracked, value);
                OnPropertyChanged(nameof(SerialExpanderVisibility));
                OnPropertyChanged(nameof(EditSerialsButtonVisibility));
            }
        }

        public Visibility SerialExpanderVisibility => IsSerialTracked && SerialEntries.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility EditSerialsButtonVisibility => IsSerialTracked ? Visibility.Visible : Visibility.Collapsed;

        public decimal TotalCost => UnitCost * Quantity;

        public string FormatPrice(decimal price)
        {
            return price.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        }

        public void OnQuantityChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (!double.IsNaN(args.NewValue) && args.NewValue >= 1)
            {
                Quantity = (int)args.NewValue;
            }
        }

        public void OnUnitCostChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (!double.IsNaN(args.NewValue) && args.NewValue >= 0)
            {
                UnitCost = (decimal)args.NewValue;
            }
        }

        public void RemoveFromPurchaseOrder(object sender, RoutedEventArgs e)
        {
            ParentPage?.RemoveProductFromPurchaseOrder(this);
        }

        public async void EditSerialNumbers(object sender, RoutedEventArgs e)
        {
            await ParentPage?.EditSerialNumbersForProduct(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
