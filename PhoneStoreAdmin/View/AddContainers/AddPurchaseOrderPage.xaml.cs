using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.View.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

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

        // Fields
        private int? _editingPurchaseOrderId = null; // For edit mode
        private PurchaseOrder? _originalPurchaseOrder = null; // For edit mode
        private Supplier? _selectedSupplier;
        private string _searchText = string.Empty;
        private string _note = string.Empty;
        private DateTime _orderDate = DateTime.Now;
        private decimal _totalAmount = 0;
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

            PurchaseOrderIdLabel.Text = _resourceLoader.GetString("PurchaseOrderIdLabel") + (PurchaseOrderService.CountAll() + 1).ToString();
            // Subscribe to collection changes
            PurchaseOrderItems.CollectionChanged += (s, e) => {
                CalculatePurchaseOrderTotal();
                OnPropertyChanged(nameof(HasPurchaseOrderItems));
            };
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
                
                // Get the batch for this purchase order to load serials
                var batches = BatchesRepository.GetByPurchaseOrderId(_originalPurchaseOrder.Id);
                var batch = batches.FirstOrDefault();
                
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
                            IsSerialTracked = product.IsSerialTracked,
                            ParentPage = this
                        };

                        // Load serial entries if product is serial tracked and batch exists
                        if (product.IsSerialTracked && batch != null)
                        {
                            // Get all serials for this product in this batch
                            var allProductSerials = ProductSerialRepository.GetByProductId(line.ProductId);
                            // Filter by batch ID
                            var serials = allProductSerials.Where(s => s.BatchId == batch.id).ToList();
                            
                            int index = 1;
                            foreach (var serial in serials)
                            {
                                var serialEntry = new Controls.SerialEntry
                                {
                                    Index = index++,
                                    SerialNumber = serial.SerialNumber,
                                    Imei1 = serial.Imei1,
                                    Imei2 = serial.Imei2 ?? string.Empty
                                };
                                item.SerialEntries.Add(serialEntry);
                            }
                        }

                        PurchaseOrderItems.Add(item);
                    }
                }

                CalculatePurchaseOrderTotal();
                
                // Show info message after page is fully loaded and has XamlRoot
                // Schedule it to run after the page is loaded
                this.Loaded += async (s, e) =>
                {
                    // Show info message if this is a DRAFT PO with serial-tracked products
                    if (_originalPurchaseOrder != null && _originalPurchaseOrder.Status == PoStatus.DRAFT)
                    {
                        var hasSerialTrackedProducts = PurchaseOrderItems.Any(i => i.IsSerialTracked);
                        if (hasSerialTrackedProducts && batch == null)
                        {
                            await ShowMessageDialog(
                                _resourceLoader.GetString("AddPO_InfoTitle"),
                                _resourceLoader.GetString("AddPO_DraftSerialMessage"));
                        }
                    }
                };
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
                    incompleteSerialItems.Add($"{item.ProductName}: Có {item.Quantity} sản phẩm nhập chưa có {item.SerialEntries.Count} serial");
                    continue;
                }

                // Check if all serial entries have complete information (Serial Number and IMEI1)
                var missingInfo = item.SerialEntries.Where(s => 
                    string.IsNullOrWhiteSpace(s.SerialNumber) || 
                    string.IsNullOrWhiteSpace(s.Imei1)).ToList();

                if (missingInfo.Any())
                {
                    incompleteSerialItems.Add($"{item.ProductName}: Thiếu thông tin Serial Number hoặc IMEI cho {missingInfo.Count} sản phẩm");
                }
            }

            // If there are incomplete serial items, show error and don't save
            if (incompleteSerialItems.Any())
            {
                var errorMessage = "Vui lòng nhập thông tin Serial Number và IMEI cho các sản phẩm sau:\n\n" + 
                    string.Join("\n", incompleteSerialItems);
                
                await ShowMessageDialog(
                    "Thông tin chưa đầy đủ", 
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
                        TotalCost = item.TotalCost
                    };
                    purchaseOrder.PurchaseOrderLines.Add(line);
                }

                // Save or Update to database
                if (_editingPurchaseOrderId.HasValue)
                {
                    PurchaseOrderService.Update(purchaseOrder);
                }
                else
                {
                    PurchaseOrderService.Insert(purchaseOrder);
                }

                // Only save serial numbers if purchase order has batches (status = RECEIVED)
                // For DRAFT purchase orders, serials will be saved when marking as RECEIVED
                var batches = BatchesRepository.GetByPurchaseOrderId(purchaseOrder.Id);
                var batch = batches.FirstOrDefault();

                if (batch != null)
                {
                    foreach (var item in PurchaseOrderItems.Where(i => i.IsSerialTracked))
                    {
                        // Delete existing serials for this batch and product (in case of update)
                        var existingSerials = ProductSerialRepository.GetByProductId(item.ProductId)
                            .Where(s => s.BatchId == batch.id).ToList();
                        foreach (var existingSerial in existingSerials)
                        {
                            ProductSerialRepository.Delete(existingSerial.Id);
                        }
                        
                        // Save new serial numbers
                        foreach (var serialEntry in item.SerialEntries)
                        {
                            var productSerial = new ProductSerial
                            {
                                ProductId = item.ProductId,
                                SerialNumber = serialEntry.SerialNumber,
                                Imei1 = serialEntry.Imei1,
                                Imei2 = string.IsNullOrWhiteSpace(serialEntry.Imei2) ? null : serialEntry.Imei2,
                                BatchId = batch.id,
                                Status = SerialStatus.IN_STOCK,
                                PurchaseOrderLineId = purchaseOrder.PurchaseOrderLines
                                    .FirstOrDefault(l => l.ProductId == item.ProductId)?.Id,
                                Note = null
                            };

                            ProductSerialRepository.Insert(productSerial);
                        }
                    }
                }
                else
                {
                    // Log that serials will be saved later when marked as RECEIVED
                    Logger.Info($"Purchase order {purchaseOrder.Id} is in DRAFT status. Serial numbers will be saved when marked as RECEIVED.");
                }

                var successMessage = _editingPurchaseOrderId.HasValue
                    ? _resourceLoader.GetString("AddPO_UpdateSuccessMessage")
                    : _resourceLoader.GetString("PurchaseOrderCreatedSuccessfully");

                await ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderSuccessTitle"), 
                    $"{successMessage}. {_resourceLoader.GetString("TotalAmountLabel")}: {FormatPrice(TotalAmount)}");
                
                // Navigate back or clear form
                if (_editingPurchaseOrderId.HasValue)
                {
                    Frame.GoBack();
                }
                else
                {
                    OnClearPurchaseOrder(sender, e);
                }
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
                
                // If product is serial tracked, show dialog to enter serial numbers
                if (fullProduct.IsSerialTracked)
                {
                    // Ask for quantity first if it's a new item
                    int quantityToAdd = 1;
                    
                    if (existingItem != null)
                    {
                        // For existing items, ask if they want to add more
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
                            Maximum = 100,
                            Value = 1,
                            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline
                        };

                        quantityDialog.Content = new StackPanel
                        {
                            Spacing = 12,
                            Children =
                            {
                                new TextBlock { Text = string.Format(_resourceLoader.GetString("AddProductExistingMessage"), existingItem.Quantity) },
                                new TextBlock { Text = _resourceLoader.GetString("AddProductQuantityLabel") },
                                numberBox
                            }
                        };

                        var result = await quantityDialog.ShowAsync();
                        if (result != ContentDialogResult.Primary)
                            return;

                        quantityToAdd = (int)numberBox.Value;
                    }

                    // Show serial number input dialog
                    var serialDialog = new SerialNumberInputDialog(product.ProductName, quantityToAdd)
                    {
                        XamlRoot = this.XamlRoot
                    };

                    var dialogResult = await serialDialog.ShowAsync();
                    
                    if (dialogResult == ContentDialogResult.Primary)
                    {
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
                }
                else
                {
                    // For non-serial tracked products, just add quantity
                    if (existingItem != null)
                    {
                        existingItem.Quantity++;
                    }
                    else
                    {
                        var newItem = new PurchaseOrderLineItem
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            UnitCost = product.CurrentCost,
                            Quantity = 1,
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

    public class SerialEntry : INotifyPropertyChanged
    {
        private int _index;
        private string _serialNumber = string.Empty;
        private string _imei1 = string.Empty;
        private string _imei2 = string.Empty;

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        public string SerialNumber
        {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }

        public string Imei1
        {
            get => _imei1;
            set => SetProperty(ref _imei1, value);
        }

        public string Imei2
        {
            get => _imei2;
            set => SetProperty(ref _imei2, value);
        }

        public string GetMachineTitle()
        {
            return $"Máy {Index}";
        }

        public Visibility HasImei2()
        {
            return string.IsNullOrWhiteSpace(Imei2) ? Visibility.Collapsed : Visibility.Visible;
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
