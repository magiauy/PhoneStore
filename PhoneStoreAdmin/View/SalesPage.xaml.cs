using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
            InvoiceItems.CollectionChanged += (s, e) => {
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
                ShowMessage("Vui lòng thêm sản phẩm vào hóa đơn", "Thông báo");
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                ShowMessage("Vui lòng nhập tên khách hàng", "Thông báo");
                return;
            }

            // Process sale logic here
            // Chuẩn bị dữ liệu hóa đơn (Header)
            var newInvoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                CreatedBy = 1,
                Status = InvoiceStatus.PAID,
                PaymentMethod = PaymentMethod.CASH,
                Note = $"Bán hàng tại quầy - {DateTime.Now:HH:mm}",
                DiscountAmount = 0
            };

            // Chuyển đổi (Map) từ UI Item sang Entity Line
            var invoiceLines = InvoiceItems.Select(item => new InvoiceLine
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPct = 0,
                // Tính toán TotalPrice cho Entity
                TotalPrice = item.UnitPrice * item.Quantity
            }).ToList();

            try
            {
                // Gọi Service với danh sách InvoiceLine đã chuyển đổi
                _invoiceService.CreateFullInvoice(newInvoice, invoiceLines, CustomerName, CustomerPhone);

                ShowMessage($"Đã thanh toán thành công!\nMã HĐ: {newInvoice.Id}\nTổng tiền: {FormatPrice(newInvoice.FinalAmount)}", "Thành công");

                OnClearInvoice(sender, e);
            }
            catch (Exception ex)
            {
                ShowMessage($"Lỗi khi thanh toán: {ex.Message}", "Lỗi hệ thống");
                Debug.WriteLine(ex.ToString());
            }

            // Clear invoice after successful sale
            OnClearInvoice(sender, e);
        }

        public void OnClearInvoice(object sender, RoutedEventArgs e)
        {
            InvoiceItems.Clear();
            CustomerName = string.Empty;
            CustomerPhone = string.Empty;
            GenerateInvoiceNumber();
            CalculateInvoiceTotal();
        }

        public void OnRefreshFilters(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            CategoryComboBox.SelectedItem = null;
            BrandComboBox.SelectedItem = null;

            FilterProducts();
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
                    // Map repository Product -> SalesItem
                    var item = new SalesItem
                    {
                        Id = p.Id,
                        ProductName = p.Name ?? string.Empty,
                        BrandId = p.BrandId,
                        BrandName = brandName,
                        CategoryName = categoryName,
                        Price = p.Price,
                        StockQuantity = 0, // nếu repo có stock field, sử dụng nó; nếu không, để 0 hoặc query thêm
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

        private void CalculateInvoiceTotal()
        {
            Subtotal = InvoiceItems.Sum(item => item.TotalPrice);
            VatAmount = Subtotal * 0.1m; // 10% VAT
            Total = Subtotal + VatAmount;
        }

        public void AddProductToInvoice(SalesItem product)
        {
            var existingItem = InvoiceItems.FirstOrDefault(item => item.ProductId == product.Id);
            
            if (existingItem != null)
            {
                existingItem.Quantity++;
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
                InvoiceItems.Add(newItem);
            }
        }

        public void RemoveProductFromInvoice(InvoiceLineItem item)
        {
            InvoiceItems.Remove(item);
        }

        private async void ShowMessage(string content, string title)
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

        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public SalesPage? ParentPage { get; set; }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public string FormatPrice(decimal price)
        {
            return price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        }

        public void AddToInvoice(object sender, RoutedEventArgs e)
        {
            ParentPage?.AddProductToInvoice(this);
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

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public SalesPage? ParentPage { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                SetProperty(ref _quantity, value);
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public decimal TotalPrice => UnitPrice * Quantity;

        public string FormatPrice(decimal price)
        {
            return price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        }

        public void RemoveFromInvoice(object sender, RoutedEventArgs e)
        {
            ParentPage?.RemoveProductFromInvoice(this);
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