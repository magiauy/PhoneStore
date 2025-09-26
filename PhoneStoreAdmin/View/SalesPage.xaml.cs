using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

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

        // Collections
        public ObservableCollection<SalesItem> AllProducts { get; set; }
        public ObservableCollection<SalesItem> FilteredProducts { get; set; }
        public ObservableCollection<InvoiceLineItem> InvoiceItems { get; set; }
        public ObservableCollection<string> Categories { get; set; }
        public ObservableCollection<string> Brands { get; set; }

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
            
            // Initialize collections
            AllProducts = new ObservableCollection<SalesItem>();
            FilteredProducts = new ObservableCollection<SalesItem>();
            InvoiceItems = new ObservableCollection<InvoiceLineItem>();
            Categories = new ObservableCollection<string>();
            Brands = new ObservableCollection<string>();

            // Generate sample data
            LoadSampleData();
            
            // Generate invoice number
            GenerateInvoiceNumber();

            // Subscribe to collection changes
            InvoiceItems.CollectionChanged += (s, e) => {
                CalculateInvoiceTotal();
                OnPropertyChanged(nameof(HasInvoiceItems));
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Load actual data from services here
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
            ShowMessage($"Đã tạo hóa đơn {InvoiceNumber} thành công!\nTổng tiền: {Total:C}", "Thành công");
            
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

        // Helper Methods
        private void LoadSampleData()
        {
            // Sample categories and brands
            Categories.Add("Smartphone");
            Categories.Add("Tablet");
            Categories.Add("Phụ kiện");
            Categories.Add("Laptop");

            Brands.Add("Apple");
            Brands.Add("Samsung");
            Brands.Add("Xiaomi");
            Brands.Add("Oppo");
            Brands.Add("Vivo");

            // Sample products
            var sampleProducts = new List<SalesItem>
            {
                new SalesItem { Id = 1, ProductName = "iPhone 15 Pro Max 256GB", BrandName = "Apple", CategoryName = "Smartphone", Price = 34990000, StockQuantity = 50 },
                new SalesItem { Id = 2, ProductName = "Samsung Galaxy S24 Ultra", BrandName = "Samsung", CategoryName = "Smartphone", Price = 31990000, StockQuantity = 30 },
                new SalesItem { Id = 3, ProductName = "Xiaomi 14 Ultra", BrandName = "Xiaomi", CategoryName = "Smartphone", Price = 24990000, StockQuantity = 25 },
                new SalesItem { Id = 4, ProductName = "iPad Pro 12.9 inch M2", BrandName = "Apple", CategoryName = "Tablet", Price = 28990000, StockQuantity = 20 },
                new SalesItem { Id = 5, ProductName = "Samsung Galaxy Tab S9", BrandName = "Samsung", CategoryName = "Tablet", Price = 18990000, StockQuantity = 15 },
                new SalesItem { Id = 6, ProductName = "AirPods Pro 2", BrandName = "Apple", CategoryName = "Phụ kiện", Price = 6290000, StockQuantity = 100 },
                new SalesItem { Id = 7, ProductName = "Samsung Galaxy Buds2 Pro", BrandName = "Samsung", CategoryName = "Phụ kiện", Price = 4990000, StockQuantity = 80 },
                new SalesItem { Id = 8, ProductName = "Oppo Find X6 Pro", BrandName = "Oppo", CategoryName = "Smartphone", Price = 22990000, StockQuantity = 35 },
                new SalesItem { Id = 9, ProductName = "Vivo X100 Pro", BrandName = "Vivo", CategoryName = "Smartphone", Price = 21990000, StockQuantity = 40 },
                new SalesItem { Id = 10, ProductName = "MacBook Pro 14 inch M3", BrandName = "Apple", CategoryName = "Laptop", Price = 49990000, StockQuantity = 10 }
            };

            foreach (var product in sampleProducts)
            {
                product.ParentPage = this;
                AllProducts.Add(product);
                FilteredProducts.Add(product);
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