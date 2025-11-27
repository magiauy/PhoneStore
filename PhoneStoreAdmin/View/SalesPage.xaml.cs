using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using MySql.Data.MySqlClient;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;

namespace PhoneStoreAdmin.View
{
    public sealed partial class SalesPage : Page, INotifyPropertyChanged
    {
        // Fields
        private string _customerName = string.Empty;
        private string _customerPhone = string.Empty;
        private string _invoiceNumber = string.Empty;
        private string _searchText = string.Empty;
        private decimal _subtotal = 0;
        private decimal _vatAmount = 0;
        private decimal _total = 0;
        private bool _isLoaded;
        private readonly IProductService _productService;
        private readonly IProductRepository _productRepository;
        private readonly IInvoiceService _invoiceService;
        private readonly IBrandRepository _brandRepository;
        private readonly IProductCategoryRepository _categoryRepository;
        private readonly IProductSerialRepository _productSerialRepository;
        private readonly IBatchProductRepository _batchProductRepository;
        private readonly ICustomerService _customerService;
        private readonly ResourceLoader _resourceLoader;
        private readonly HashSet<string> _usedSerialNumbers = new(StringComparer.OrdinalIgnoreCase);
        private Guid _customerLookupRequestId = Guid.Empty;
        private int? _selectedCustomerId;
        private bool _isExistingCustomer;
        private string _existingCustomerName = string.Empty;

        // Collections
        public ObservableCollection<SalesItem> AllProducts { get; set; }
        public ObservableCollection<SalesItem> FilteredProducts { get; set; }
        public ObservableCollection<InvoiceLineItem> InvoiceItems { get; set; }
        public ObservableCollection<string> Categories { get; set; }
        public ObservableCollection<string> Brands { get; set; }
        public ObservableCollection<ProductModelListItemViewModel> ProductModels { get; } = new();

        // Properties for binding
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        public string ExistingCustomerName
        {
            get => _existingCustomerName;
            private set => SetProperty(ref _existingCustomerName, value);
        }

        public bool IsExistingCustomer
        {
            get => _isExistingCustomer;
            private set
            {
                if (SetProperty(ref _isExistingCustomer, value))
                {
                    OnPropertyChanged(nameof(ExistingCustomerNameVisibility));
                    OnPropertyChanged(nameof(ManualCustomerNameVisibility));
                }
            }
        }

        public Visibility ExistingCustomerNameVisibility => IsExistingCustomer ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ManualCustomerNameVisibility => IsExistingCustomer ? Visibility.Collapsed : Visibility.Visible;

        private SalesItem? FindSalesItemById(int productId)
        {
            return AllProducts.FirstOrDefault(p => p.Id == productId);
        }

        private bool EnsureStockAvailability(int productId, int desiredQuantity, bool showAlert, out string errorMessage)
        {
            var product = FindSalesItemById(productId);
            var stockQty = product?.StockQuantity ?? 0;

            if (stockQty <= 0)
            {
                errorMessage = _resourceLoader.GetString("Sales_OutOfStockError");
                product?.SetStockError(errorMessage);
                if (showAlert)
                {
                    ShowMessage(errorMessage, _resourceLoader.GetString("Sales_InsufficientStockTitle"));
                }
                return false;
            }

            if (desiredQuantity > stockQty)
            {
                errorMessage = string.Format(_resourceLoader.GetString("Sales_InsufficientStockFormat"), stockQty);
                product?.SetStockError(errorMessage);
                if (showAlert)
                {
                    ShowMessage(errorMessage, _resourceLoader.GetString("Sales_InsufficientStockTitle"));
                }
                return false;
            }

            product?.ClearStockError();
            errorMessage = string.Empty;
            return true;
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set => SetProperty(ref _customerPhone, value);
        }

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public decimal Subtotal
        {
            get => _subtotal;
            set => SetProperty(ref _subtotal, value);
        }

        public decimal VatAmount
        {
            get => _vatAmount;
            set => SetProperty(ref _vatAmount, value);
        }

        public decimal Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }

        public bool HasInvoiceItems => InvoiceItems?.Count > 0;

        // Constructor
        public SalesPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            _productService = App.GetService<IProductService>();
            _productRepository = App.GetService<IProductRepository>();
            _invoiceService = App.GetService<IInvoiceService>();
            _brandRepository = App.GetService<IBrandRepository>();
            _categoryRepository = App.GetService<IProductCategoryRepository>();
            _productSerialRepository = App.GetService<IProductSerialRepository>();
            _batchProductRepository = App.GetService<IBatchProductRepository>();
            _customerService = App.GetService<ICustomerService>();
            _resourceLoader = new ResourceLoader();

            // Initialize collections
            AllProducts = new ObservableCollection<SalesItem>();
            FilteredProducts = new ObservableCollection<SalesItem>();
            InvoiceItems = new ObservableCollection<InvoiceLineItem>();
            Categories = new ObservableCollection<string>();
            Brands = new ObservableCollection<string>();

            // Generate invoice number
            GenerateInvoiceNumber();

            _isLoaded = false;

            // Subscribe to collection changes
            InvoiceItems.CollectionChanged += (s, e) =>
            {
                CalculateInvoiceTotal();
                OnPropertyChanged(nameof(HasInvoiceItems));
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedTo ENTER. _isLoaded = " + _isLoaded);
            base.OnNavigatedTo(e);

            if (!_isLoaded)
            {
                Debug.WriteLine("Calling LoadInitialData()");
                _isLoaded = true;
                LoadInitialData();
            }
            else
            {
                Debug.WriteLine("Skipped LoadInitialData because _isLoaded is true");
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

        // Helper Methods for Binding
        public string FormatPrice(decimal price)
        {
            return price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        }

        public void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterProducts();
        }

        public void OnBrandSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterProducts();
        }

        public void OnProcessSale(object sender, RoutedEventArgs e)
        {
            if (!HasInvoiceItems)
            {
                ShowMessage(_resourceLoader.GetString("Sales_AddProductsMessage"), _resourceLoader.GetString("Sales_NotificationTitle"));
                return;
            }

            foreach (var item in InvoiceItems)
            {
                if (item.IsSerialTracked)
                {
                    int validCount = item.SerialEntries.Count(s => s.IsValid);
                    if (validCount < item.Quantity)
                    {
                        ShowMessage(string.Format(_resourceLoader.GetString("Sales_MissingSerialFormat"), item.ProductName, item.Quantity, validCount), _resourceLoader.GetString("Sales_MissingInfoTitle"));
                        return;
                    }
                }
            }

            if (!EnsureCustomerInfo(out var invoiceCustomerName))
            {
                return;
            }

            var newInvoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                CreatedBy = 1,
                Status = InvoiceStatus.PAID,
                PaymentMethod = PaymentMethod.CASH,
                Note = string.Format(_resourceLoader.GetString("Sales_InvoiceNoteFormat"), DateTime.Now.ToString("HH:mm")),
                DiscountAmount = 0
            };

            var invoiceLines = new List<InvoiceLine>();
            var serialLineRequests = new List<InvoiceLineSerialRequest>();

            foreach (var item in InvoiceItems)
            {
                var line = new InvoiceLine
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPct = 0,
                    TotalPrice = item.UnitPrice * item.Quantity
                };

                invoiceLines.Add(line);

                if (!item.IsSerialTracked)
                {
                    continue;
                }

                var serialNumbers = item.SerialEntries
                    .Where(entry => entry.IsValid && !string.IsNullOrWhiteSpace(entry.LastValidSerial))
                    .Select(entry => entry.LastValidSerial!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(item.Quantity)
                    .ToList();

                if (serialNumbers.Count > 0)
                {
                    serialLineRequests.Add(new InvoiceLineSerialRequest
                    {
                        Line = line,
                        SerialNumbers = serialNumbers
                    });
                }
            }

            try
            {
                _invoiceService.CreateFullInvoice(newInvoice, invoiceLines, invoiceCustomerName, CustomerPhone?.Trim(), serialLineRequests);

                ShowMessage(string.Format(_resourceLoader.GetString("Sales_PaymentSuccessFormat"), newInvoice.Id, FormatPrice(newInvoice.FinalAmount)), _resourceLoader.GetString("Sales_SuccessTitle"));

                OnClearInvoice(sender, e);
            }
            catch (Exception ex)
            {
                ShowMessage(string.Format(_resourceLoader.GetString("Sales_PaymentErrorFormat"), ex.Message), _resourceLoader.GetString("Sales_SystemErrorTitle"));
                Debug.WriteLine(ex.ToString());
            }

            OnClearInvoice(sender, e);
        }

        public void OnClearInvoice(object sender, RoutedEventArgs e)
        {
            InvoiceItems.Clear();
            ResetCustomerInfo();
            GenerateInvoiceNumber();
            CalculateInvoiceTotal();
            ClearAllStockErrors();
        }

        public void OnRefreshFilters(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            CategoryComboBox.SelectedItem = null;
            BrandComboBox.SelectedItem = null;

            FilterProducts();
        }

        public async void OnCheckPhoneNumberChanged(object sender, TextChangedEventArgs e)
        {
            await LookupCustomerByPhoneAsync(CustomerPhoneTextBox?.Text);
        }

        public async void OnCheckPhoneNumberClick(object sender, RoutedEventArgs e)
        {
            await LookupCustomerByPhoneAsync(CustomerPhoneTextBox?.Text);
        }


        private async Task LookupCustomerByPhoneAsync(string? rawPhone)
        {
            if (_customerService == null)
            {
                return;
            }

            var phone = rawPhone?.Trim() ?? string.Empty;
            if (!string.Equals(CustomerPhone, phone, StringComparison.Ordinal))
            {
                CustomerPhone = phone;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                ClearCustomerSelection(true);
                return;
            }

            if (phone.Length < 6)
            {
                ClearCustomerSelection(false);
                return;
            }

            var lookupId = Guid.NewGuid();
            _customerLookupRequestId = lookupId;

            try
            {
                var customer = await _customerService.GetCustomerByPhoneAsync(phone);
                if (_customerLookupRequestId != lookupId)
                {
                    return;
                }

                if (customer != null)
                {
                    ApplyExistingCustomer(customer);
                }
                else
                {
                    ClearCustomerSelection(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OnCheckPhoneNumber lookup failed: " + ex);
            }
        }

        private void LoadInitialData(int? selectedModelId = null)
        {
            Debug.WriteLine("LoadInitialData START");

            try
            {
                if (_productService == null)
                {
                    Debug.WriteLine("LoadInitialData: _productService == null");
                    return;
                }

                var summaries = _productService.GetProductModelSummaries();
                Debug.WriteLine($"LoadInitialData: summaries is {(summaries == null ? "NULL" : "NOT NULL")}");

                if (summaries == null)
                {
                    return;
                }

                var list = summaries.ToList();
                Debug.WriteLine($"LoadInitialData: summaries.Count = {list.Count}");

                ProductModels.Clear();
                foreach (var model in list)
                {
                    Debug.WriteLine($" - model.Id={model.Id}, model.Name={model.Name ?? "<no name>"}");
                    ProductModels.Add(model);
                }

                ProductModelListItemViewModel? selected = null;
                if (selectedModelId.HasValue)
                {
                    selected = ProductModels.FirstOrDefault(m => m.Id == selectedModelId.Value);
                    Debug.WriteLine("SelectedModelId provided: " + selectedModelId.Value + " -> selected is " + (selected == null ? "NULL" : "FOUND"));
                }

                if (selected == null)
                {
                    selected = ProductModels.FirstOrDefault();
                    Debug.WriteLine("Selected is null, take first -> " + (selected == null ? "NULL" : $"Id={selected.Id}"));
                }

                if (selected != null)
                {
                    Debug.WriteLine("Calling LoadModelDetail for id=" + selected.Id);
                    LoadModelDetail(selected.Id);
                }
                else
                {
                    Debug.WriteLine("No product models available -> SelectedModelDetail = null");
                    SelectedModelDetail = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadInitialData EXCEPTION: " + ex);
            }

            Debug.WriteLine("LoadInitialData END");
        }

        private async Task LoadModelDetail(int modelId)
        {
            Debug.WriteLine("LoadModelDetail START modelId=" + modelId);
            try
            {
                var detail = _productService.GetProductModelDetail(modelId);

                if (detail != null && _productRepository != null)
                {

                    Debug.WriteLine("FALLBACK: Calling GetAll() on IProductRepository.");
                    var allProductEntities = _productRepository.GetAll();


                    var variantEntities = allProductEntities
                        .Where(p => (p.ModelId == modelId))
                        .ToList();

                    var variantViewModels = variantEntities.Select(MapToProductListItemViewModel).ToList();

                    detail = new ProductModelDetailViewModel(
                        detail.Model,
                        variantViewModels,
                        detail.Attributes
                    );
                }

                SelectedModelDetail = detail;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadModelDetail EXCEPTION: " + ex);
                SelectedModelDetail = null;
            }
            Debug.WriteLine("LoadModelDetail END");
        }

        private ProductListItemViewModel MapToProductListItemViewModel(object productEntity)
        {
            dynamic product = productEntity;

            return new ProductListItemViewModel
            {
                Id = (int)product.Id,
                Name = product.Name ?? string.Empty,
                Price = product.Price
            };
        }

        private ProductModelDetailViewModel? _selectedModelDetail;
        private bool _usingSyntheticVariants;
        public bool UsingSyntheticVariants
        {
            get => _usingSyntheticVariants;
            private set => SetProperty(ref _usingSyntheticVariants, value);
        }

        public ProductModelDetailViewModel? SelectedModelDetail
        {
            get => _selectedModelDetail;
            private set
            {
                Debug.WriteLine("SelectedModelDetail SETTER ENTER");
                if (_selectedModelDetail == value)
                {
                    Debug.WriteLine("SelectedModelDetail unchanged -> exit");
                    return;
                }

                _selectedModelDetail = value;
                Debug.WriteLine("SelectedModelDetail assigned: " + (_selectedModelDetail == null ? "NULL" : "NOT NULL"));

                UpdateVariantMetadataSummaries();

                PopulateProductsFromRepository();

                // Thông báo thay đổi cho các property liên quan
                OnPropertyChanged(nameof(SelectedModelDetail));
                OnPropertyChanged(nameof(SelectedModelVariants));
                OnPropertyChanged(nameof(AllProducts));
                OnPropertyChanged(nameof(FilteredProducts));
                OnPropertyChanged(nameof(Brands));
                OnPropertyChanged(nameof(Categories));
                OnPropertyChanged(nameof(HasInvoiceItems));

                // cập nhật trạng thái synthetic (getter SelectedModelVariants sẽ set UsingSyntheticVariants)
                var _ = SelectedModelVariants;

                Debug.WriteLine("SelectedModelDetail SETTER EXIT");
            }
        }

        private void PopulateProductsFromRepository()
        {
            Debug.WriteLine("PopulateProductsFromRepository START");

            AllProducts.Clear();
            FilteredProducts.Clear();
            Categories.Clear();
            Brands.Clear();

            if (_productRepository == null)
            {
                Debug.WriteLine("PopulateProductsFromRepository: _productRepository == null -> abort");
                return;
            }

            try
            {
                var products = _productRepository.GetAll()?.ToList() ?? new List<Product>();

                Debug.WriteLine($"PopulateProductsFromRepository: fetched products.Count = {products.Count}");
                var brands = _brandRepository?.GetAll()?.ToDictionary(b => b.Id, b => b.Name) ?? new Dictionary<int, string>();
                var categories = _categoryRepository?.GetAll()?.ToDictionary(c => c.Id, c => c.Name) ?? new Dictionary<int, string>();
                var serialTrackedIds = products.Where(p => p.IsSerialTracked).Select(p => p.Id).ToList();
                var batchTrackedIds = products.Where(p => !p.IsSerialTracked).Select(p => p.Id).ToList();

                // Mirror PhoneStoreUser inventory logic: serial tracked items count ProductSerials, others sum BatchProducts quantities.
                var serialStockLookup = serialTrackedIds.Count > 0
                    ? _productSerialRepository?.GetInStockCountsByProductIds(serialTrackedIds) ?? new Dictionary<int, int>()
                    : new Dictionary<int, int>();
                var batchStockLookup = batchTrackedIds.Count > 0
                    ? _batchProductRepository?.GetQuantitiesByProductIds(batchTrackedIds) ?? new Dictionary<int, int>()
                    : new Dictionary<int, int>();

                var brandSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var categorySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var p in products)
                {
                    string brandName = "Unknown";
                    if (p.BrandId.HasValue && brands.TryGetValue(p.BrandId.Value, out var bName))
                    {
                        brandName = bName;
                    }

                    string categoryName = "Unknown";
                    if (p.CategoryId != 0 && categories.TryGetValue(p.CategoryId, out var cName))
                    {
                        categoryName = cName;
                    }
                    var stockQty = p.IsSerialTracked
                        ? (serialStockLookup.TryGetValue(p.Id, out var serialQty) ? serialQty : 0)
                        : (batchStockLookup.TryGetValue(p.Id, out var batchQty) ? batchQty : 0);
                    // Map repository Product -> SalesItem
                    var item = new SalesItem
                    {
                        Id = p.Id,
                        ProductName = p.Name ?? string.Empty,
                        BrandId = p.BrandId,
                        BrandName = brandName,
                        CategoryName = categoryName,
                        Price = p.Price,
                        StockQuantity = stockQty,
                        IsSerialTracked = p.IsSerialTracked,
                        ParentPage = this
                    };

                    AllProducts.Add(item);

                    if (!string.IsNullOrWhiteSpace(item.BrandName) && brandSet.Add(item.BrandName))
                        Brands.Add(item.BrandName);

                    if (!string.IsNullOrWhiteSpace(item.CategoryName) && categorySet.Add(item.CategoryName))
                        Categories.Add(item.CategoryName);
                }

                // Populate filtered list initially with all products
                foreach (var p in AllProducts) FilteredProducts.Add(p);

                // Notify bindings (thường ObservableCollection tự notify nhưng vẫn an toàn)
                OnPropertyChanged(nameof(AllProducts));
                OnPropertyChanged(nameof(FilteredProducts));
                OnPropertyChanged(nameof(Brands));
                OnPropertyChanged(nameof(Categories));

                Debug.WriteLine($"PopulateProductsFromRepository END: AllProducts={AllProducts.Count}, FilteredProducts={FilteredProducts.Count}, Brands={Brands.Count}, Categories={Categories.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PopulateProductsFromRepository EXCEPTION: " + ex);
            }
        }

        private readonly List<string> _selectedBrandNames = new();
        private readonly List<string> _selectedCategoryNames = new();
        private void UpdateVariantMetadataSummaries()
        {
            _selectedBrandNames.Clear();
            _selectedCategoryNames.Clear();

            if (_selectedModelDetail?.Variants == null)
            {
                return;
            }

            var brandSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var categorySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var variant in _selectedModelDetail.Variants)
            {
                if (!string.IsNullOrWhiteSpace(variant.BrandName) && brandSet.Add(variant.BrandName!))
                {
                    _selectedBrandNames.Add(variant.BrandName!);
                }

                if (!string.IsNullOrWhiteSpace(variant.CategoryName) && categorySet.Add(variant.CategoryName))
                {
                    _selectedCategoryNames.Add(variant.CategoryName);
                }
            }

            _selectedBrandNames.Sort(StringComparer.OrdinalIgnoreCase);
            _selectedCategoryNames.Sort(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<ProductListItemViewModel>? SelectedModelVariants
        {
            get
            {
                // Nếu có variants thực thì trả về
                if (_selectedModelDetail?.Variants != null && _selectedModelDetail.Variants.Any())
                {
                    UsingSyntheticVariants = false;
                    return _selectedModelDetail.Variants;
                }

                // Không có variants -> trả về danh sách giả lập
                UsingSyntheticVariants = true;
                return CreateListFromModel(_selectedModelDetail);
            }
        }

        private IReadOnlyList<ProductListItemViewModel> CreateListFromModel(ProductModelDetailViewModel? detail)
        {
            var list = new List<ProductListItemViewModel>();

            if (detail?.Model == null)
                return list;

            var model = detail.Model;

            var item = new ProductListItemViewModel
            {
                Id = model.Id,
                Name = model.Name ?? "<No name>",
                SerialCount = 0 // hoặc gán theo logic riêng của bạn
            };

            list.Add(item);
            return list;
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
                    p.BrandName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by category
            var categoryComboBox = this.FindName("CategoryComboBox") as ComboBox;
            if (categoryComboBox?.SelectedItem is string selectedCategory && !string.IsNullOrEmpty(selectedCategory))
            {
                filteredItems = filteredItems.Where(p => p.CategoryName == selectedCategory);
            }

            // Filter by brand
            var brandComboBox = this.FindName("BrandComboBox") as ComboBox;
            if (brandComboBox?.SelectedItem is string selectedBrand && !string.IsNullOrEmpty(selectedBrand))
            {
                filteredItems = filteredItems.Where(p => p.BrandName == selectedBrand);
            }

            foreach (var item in filteredItems)
            {
                FilteredProducts.Add(item);
            }
        }

        private void GenerateInvoiceNumber()
        {
            InvoiceNumber = $"HD{DateTime.Now:yyyyMMddHHmmss}";
        }

        public void CalculateInvoiceTotal()
        {
            Subtotal = InvoiceItems.Sum(item => item.TotalPrice);
            VatAmount = Subtotal * 0.1m; // 10% VAT
            Total = Subtotal + VatAmount;
        }

        private bool EnsureCustomerInfo(out string invoiceCustomerName)
        {
            invoiceCustomerName = string.Empty;

            var phone = CustomerPhone?.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                ShowMessage(_resourceLoader.GetString("Sales_EnterPhoneMessage"), _resourceLoader.GetString("Sales_NotificationTitle"));
                return false;
            }

            if (IsExistingCustomer)
            {
                invoiceCustomerName = string.IsNullOrWhiteSpace(ExistingCustomerName)
                    ? CustomerName
                    : ExistingCustomerName;

                if (string.IsNullOrWhiteSpace(invoiceCustomerName))
                {
                    ShowMessage(_resourceLoader.GetString("Sales_CustomerNameNotFoundMessage"), _resourceLoader.GetString("Sales_NotificationTitle"));
                    return false;
                }

                return true;
            }

            var trimmedName = CustomerName?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                ShowMessage(_resourceLoader.GetString("Sales_EnterCustomerNameMessage"), _resourceLoader.GetString("Sales_NotificationTitle"));
                return false;
            }

            CustomerName = trimmedName;
            if (_customerService == null)
            {
                ShowMessage(_resourceLoader.GetString("Sales_CustomerServiceErrorMessage"), _resourceLoader.GetString("Sales_SystemErrorTitle"));
                return false;
            }

            var newCustomer = new Customer(trimmedName)
            {
                Phone = phone,
                PersonType = PersonType.CUSTOMER,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (!_customerService.Insert(newCustomer))
            {
                ShowMessage(_resourceLoader.GetString("Sales_CreateCustomerErrorMessage"), _resourceLoader.GetString("Sales_SystemErrorTitle"));
                return false;
            }

            ApplyExistingCustomer(newCustomer);
            invoiceCustomerName = newCustomer.FullName;
            return true;
        }

        private void ApplyExistingCustomer(Customer customer)
        {
            if (customer == null)
            {
                return;
            }

            _selectedCustomerId = customer.Id;
            var name = customer.FullName ?? string.Empty;
            ExistingCustomerName = name;
            CustomerName = name;
            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                CustomerPhone = customer.Phone?.Trim() ?? CustomerPhone;
            }

            IsExistingCustomer = true;
        }

        private void ClearCustomerSelection(bool clearManualName)
        {
            var shouldClearName = clearManualName || IsExistingCustomer;
            _selectedCustomerId = null;
            ExistingCustomerName = string.Empty;
            IsExistingCustomer = false;

            if (shouldClearName)
            {
                CustomerName = string.Empty;
            }
        }

        private void ResetCustomerInfo()
        {
            _customerLookupRequestId = Guid.Empty;
            ClearCustomerSelection(true);
            CustomerPhone = string.Empty;
        }

        private void ClearAllStockErrors()
        {
            foreach (var product in AllProducts)
            {
                product.ClearStockError();
            }
        }

        public void AddProductToInvoice(SalesItem product)
        {
            var existingItem = InvoiceItems.FirstOrDefault(item => item.ProductId == product.Id);
            var desiredQuantity = (existingItem?.Quantity ?? 0) + 1;

            if (!EnsureStockAvailability(product.Id, desiredQuantity, showAlert: true, out _))
            {
                return;
            }

            if (existingItem != null)
            {
                existingItem.Quantity = desiredQuantity;
                CalculateInvoiceTotal();
            }
            else
            {
                var newItem = new InvoiceLineItem
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    UnitPrice = product.Price,
                    Quantity = 1,
                    ParentPage = this
                };
                newItem.IsSerialTracked = product.IsSerialTracked;
                if (product.IsSerialTracked)
                {
                    newItem.InitializeSerialEntries();
                }
                InvoiceItems.Add(newItem);
            }
        }

        public void ChangeInvoiceItemQuantity(InvoiceLineItem item, int desiredQuantity)
        {
            if (item == null)
            {
                return;
            }

            if (desiredQuantity < 1)
            {
                desiredQuantity = 1;
            }

            if (desiredQuantity == item.Quantity)
            {
                return;
            }

            if (!EnsureStockAvailability(item.ProductId, desiredQuantity, showAlert: true, out _))
            {
                return;
            }

            item.Quantity = desiredQuantity;
        }

        public async Task HandleSerialEntryAsync(InvoiceLineItem item, SerialEntryViewModel entry)
        {
            if (item == null || entry == null)
            {
                return;
            }

            var serialText = entry.SerialInput?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(entry.LastValidSerial) &&
                !string.Equals(entry.LastValidSerial, serialText, StringComparison.OrdinalIgnoreCase))
            {
                RemoveSerialUsage(new[] { entry.LastValidSerial });
                entry.LastValidSerial = null;
            }

            if (string.IsNullOrWhiteSpace(serialText))
            {
                entry.SetValidation(false, string.Empty);
                return;
            }

            if (IsDuplicateSerialInInvoice(serialText, entry))
            {
                entry.SetValidation(false, "Serial đã nhập ở dòng khác.");
                return;
            }

            if (_usedSerialNumbers.Contains(serialText) && entry.LastValidSerial != serialText)
            {
                entry.SetValidation(false, "Serial đã được sử dụng.");
                return;
            }

            if (_productSerialRepository == null)
            {
                return;
            }

            try
            {
                var serialEntity = _productSerialRepository.TryGetBySerialNumber(serialText);

                if (serialEntity == null)
                {
                    entry.SetValidation(false, "Serial không tồn tại.");
                }
                else if (serialEntity.ProductId != item.ProductId)
                {
                    entry.SetValidation(false, "Serial không khớp sản phẩm này.");
                }
                else if (serialEntity.Status != SerialStatus.IN_STOCK)
                {
                    entry.SetValidation(false, $"Serial không khả dụng ({serialEntity.Status}).");
                }
                else
                {
                    entry.SetValidation(true, string.Empty);
                    entry.LastValidSerial = serialText;
                    _usedSerialNumbers.Add(serialText);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                entry.SetValidation(false, "Lỗi kiểm tra serial.");
            }

            await Task.CompletedTask;
        }

        private bool IsDuplicateSerialInInvoice(string serialToCheck, SerialEntryViewModel currentEntry)
        {
            if (string.IsNullOrWhiteSpace(serialToCheck))
            {
                return false;
            }

            foreach (var item in InvoiceItems)
            {
                foreach (var entry in item.SerialEntries)
                {
                    if (entry == currentEntry)
                    {
                        continue;
                    }

                    if (string.Equals(entry.SerialInput?.Trim(), serialToCheck.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal void RemoveSerialUsage(IEnumerable<string?> serials)
        {
            if (serials == null)
            {
                return;
            }

            foreach (var serial in serials)
            {
                if (string.IsNullOrWhiteSpace(serial))
                {
                    continue;
                }

                _usedSerialNumbers.Remove(serial);
            }
        }

        public void RemoveProductFromInvoice(InvoiceLineItem item)
        {
            RemoveSerialUsage(item?.SerialEntries.Select(entry => entry.LastValidSerial));
            InvoiceItems.Remove(item);
        }

        public async void ShowMessage(string content, string title)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = title,
                Content = content,
                CloseButtonText = "Đóng",
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
    public class SalesItem : INotifyPropertyChanged
    {
        private int _quantity = 1;
        private bool _hasStockError;
        private string _stockErrorMessage = string.Empty;

        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsSerialTracked { get; set; }
        public SalesPage? ParentPage { get; set; }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public bool HasStockError
        {
            get => _hasStockError;
            private set
            {
                if (SetProperty(ref _hasStockError, value))
                {
                    OnPropertyChanged(nameof(StockBorderBrush));
                }
            }
        }

        public string StockErrorMessage
        {
            get => _stockErrorMessage;
            private set
            {
                if (SetProperty(ref _stockErrorMessage, value))
                {
                    OnPropertyChanged(nameof(StockErrorVisibility));
                }
            }
        }

        public Visibility StockErrorVisibility => string.IsNullOrWhiteSpace(StockErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

        public Brush StockBorderBrush => HasStockError
            ? (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"]
            : (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        public string FormatPrice(decimal price)
        {
            return price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        }

        public string FormatStock(int quantity)
        {
            var resourceLoader = new ResourceLoader();
            return string.Format(resourceLoader.GetString("Sales_StockFormat"), quantity);
        }

        public void AddToInvoice(object sender, RoutedEventArgs e)
        {
            ParentPage?.AddProductToInvoice(this);
        }

        public void SetStockError(string message)
        {
            StockErrorMessage = message ?? string.Empty;
            HasStockError = !string.IsNullOrEmpty(message);
        }

        public void ClearStockError()
        {
            StockErrorMessage = string.Empty;
            HasStockError = false;
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

    public class InvoiceLineItem : INotifyPropertyChanged
    {
        private int _quantity = 1;
        private readonly ObservableCollection<SerialEntryViewModel> _serialEntries = new();

        public InvoiceLineItem()
        {
            _serialEntries.CollectionChanged += OnSerialEntriesChanged;
        }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public SalesPage? ParentPage { get; set; }
        public bool IsSerialTracked { get; set; }

        public ObservableCollection<SerialEntryViewModel> SerialEntries => _serialEntries;

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value < 1)
                {
                    value = 1;
                }

                SetQuantityValue(value);
            }
        }

        public decimal TotalPrice => UnitPrice * Quantity;

        public bool CanDecrease => Quantity > 1;
        public bool CanIncrease => true;

        public string FormatPrice(decimal price)
        {
            return ParentPage?.FormatPrice(price) ?? price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        }

        public Brush SerialContainerBorderBrush => SerialEntries.Any(entry => entry.HasError)
            ? (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"]
            : SerialEntries.Any(entry => entry.IsValid)
                ? (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
                : (Brush)Application.Current.Resources["SurfaceStrokeColorDefaultBrush"];

        public void InitializeSerialEntries()
        {
            _serialEntries.Clear();
            UpdateSerialEntriesCount(Math.Max(1, Quantity));
        }

        private void UpdateSerialEntriesCount(int newCount)
        {
            if (!IsSerialTracked)
            {
                _serialEntries.Clear();
                return;
            }

            newCount = Math.Max(1, newCount);

            while (_serialEntries.Count < newCount)
            {
                int nextIndex = _serialEntries.Count + 1;
                _serialEntries.Add(new SerialEntryViewModel(this, nextIndex));
            }

            while (_serialEntries.Count > newCount)
            {
                var lastEntry = _serialEntries.Last();
                if (!string.IsNullOrEmpty(lastEntry.LastValidSerial))
                {
                    ParentPage?.RemoveSerialUsage(new[] { lastEntry.LastValidSerial });
                }

                _serialEntries.Remove(lastEntry);
            }
        }

        private void SetQuantityValue(int value)
        {
            if (SetProperty(ref _quantity, value))
            {
                if (IsSerialTracked)
                {
                    UpdateSerialEntriesCount(value);
                }

                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(CanDecrease));
                OnPropertyChanged(nameof(CanIncrease));

                ParentPage?.CalculateInvoiceTotal();
            }
        }

        public void RemoveFromInvoice(object sender, RoutedEventArgs e)
        {
            ParentPage?.RemoveProductFromInvoice(this);
        }

        public void IncreaseQuantity(object sender, RoutedEventArgs e)
        {
            ParentPage?.ChangeInvoiceItemQuantity(this, Quantity + 1);
        }

        public void DecreaseQuantity(object sender, RoutedEventArgs e)
        {
            ParentPage?.ChangeInvoiceItemQuantity(this, Quantity - 1);
        }

        private void OnSerialEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SerialEntryViewModel entry in e.NewItems)
                {
                    entry.PropertyChanged += SerialEntryOnPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (SerialEntryViewModel entry in e.OldItems)
                {
                    entry.PropertyChanged -= SerialEntryOnPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(SerialContainerBorderBrush));
        }

        private void SerialEntryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SerialEntryViewModel.IsValid) or nameof(SerialEntryViewModel.Message))
            {
                OnPropertyChanged(nameof(SerialContainerBorderBrush));
            }
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

    public class SerialEntryViewModel : INotifyPropertyChanged
    {
        private string _serialInput = string.Empty;
        private string _message = string.Empty;
        private bool _isValid;

        public InvoiceLineItem Owner { get; }

        public int Index { get; set; }

        public SerialEntryViewModel(InvoiceLineItem owner, int index)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Index = index;
        }

        public string SerialInput
        {
            get => _serialInput;
            set
            {
                if (SetProperty(ref _serialInput, value))
                {
                    IsValid = false;
                    Message = string.Empty;
                    LastValidSerial = null;
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(IconVisibility));
                    OnPropertyChanged(nameof(BorderBrush));
                }
            }
        }

        public bool IsValid
        {
            get => _isValid;
            private set
            {
                if (SetProperty(ref _isValid, value))
                {
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(IconVisibility));
                    OnPropertyChanged(nameof(BorderBrush));
                }
            }
        }

        public string Message
        {
            get => _message;
            private set
            {
                if (SetProperty(ref _message, value))
                {
                    OnPropertyChanged(nameof(MessageVisibility));
                    OnPropertyChanged(nameof(BorderBrush));
                }
            }
        }

        public string? LastValidSerial { get; set; }

        public bool HasError => !IsValid && !string.IsNullOrWhiteSpace(Message);

        public string PlaceholderText => $"#{Index} Serial / IMEI...";

        public string StatusIcon => IsValid ? "" : "";

        public Brush StatusColor => IsValid
            ? (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
            : (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];

        public Brush BorderBrush => (IsValid || !string.IsNullOrEmpty(Message))
            ? StatusColor
            : (Brush)Application.Current.Resources["SurfaceStrokeColorDefaultBrush"];

        public Visibility IconVisibility => (IsValid || !string.IsNullOrEmpty(Message))
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility MessageVisibility => string.IsNullOrWhiteSpace(Message)
            ? Visibility.Collapsed
            : Visibility.Visible;

        public async void OnLostFocus(object sender, RoutedEventArgs e)
        {
            await Owner.ParentPage?.HandleSerialEntryAsync(Owner, this);
        }

        public void SetValidation(bool valid, string message)
        {
            IsValid = valid;
            Message = message;
            if (!valid)
            {
                LastValidSerial = null;
            }
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
