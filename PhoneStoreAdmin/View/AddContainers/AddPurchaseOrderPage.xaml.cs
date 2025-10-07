using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
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
            LoadData();
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

            try
            {
                // Create PurchaseOrder object
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierId = SelectedSupplier.Id,
                    CreatedBy = 1, // TODO: Get from current user session
                    OrderDate = OrderDate,
                    Status = PoStatus.DRAFT,
                    TotalAmount = TotalAmount,
                    Note = string.IsNullOrWhiteSpace(Note) ? null : Note
                };

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

                // Save to database (this will automatically create batches via PurchaseOrderService)
                PurchaseOrderService.Insert(purchaseOrder);

                // Now get the created batch and add serial numbers for serial-tracked products
                var batches = BatchesRepository.GetByPurchaseOrderId(purchaseOrder.Id);
                var batch = batches.FirstOrDefault();

                if (batch != null)
                {
                    foreach (var item in PurchaseOrderItems.Where(i => i.IsSerialTracked))
                    {
                        // Save serial numbers
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

                await ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderSuccessTitle"), 
                    $"{_resourceLoader.GetString("PurchaseOrderCreatedSuccessfully")}. {_resourceLoader.GetString("TotalAmountLabel")}: {FormatPrice(TotalAmount)}");
                
                // Navigate back or clear form
                OnClearPurchaseOrder(sender, e);
            }
            catch (Exception ex)
            {
                await ShowMessageDialog(
                    _resourceLoader.GetString("PurchaseOrderErrorTitle"), 
                    $"{_resourceLoader.GetString("CannotCreatePurchaseOrder")}: {ex.Message}");
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
                            foreach (var entry in serialDialog.SerialEntries)
                            {
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
        public ObservableCollection<Controls.SerialEntry> SerialEntries { get; set; } = new ObservableCollection<Controls.SerialEntry>();

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
            set => SetProperty(ref _isSerialTracked, value);
        }

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
