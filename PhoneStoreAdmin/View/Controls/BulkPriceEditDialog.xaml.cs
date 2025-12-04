using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class BulkPriceEditDialog : ContentDialog
    {
        private readonly List<string> _categories;
        private readonly List<string> _brands;
        private readonly float _minimumProfitMargin;
        private readonly List<ProductSelectionItem> _allProducts;
        private readonly ObservableCollection<ProductSelectionItem> _filteredProducts;
        private readonly HashSet<int> _selectedProductIds;

        public BulkPriceEditResult? Result { get; private set; }

        public BulkPriceEditDialog(
            List<ProductSelectionItem> products,
            List<string> categories,
            List<string> brands,
            float minimumProfitMargin)
        {
            _allProducts = products;
            _filteredProducts = new ObservableCollection<ProductSelectionItem>(products);
            _selectedProductIds = new HashSet<int>();
            _categories = categories;
            _brands = brands;
            _minimumProfitMargin = minimumProfitMargin;

            this.InitializeComponent();
            this.Loaded += BulkPriceEditDialog_Loaded;
        }

        private void BulkPriceEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up product list
            ProductListView.ItemsSource = _filteredProducts;

            // Set up filter combo boxes with "All" option
            var categoryFilterItems = new List<string> { "All" };
            categoryFilterItems.AddRange(_categories);
            ProductFilterCategoryCombo.ItemsSource = categoryFilterItems;
            ProductFilterCategoryCombo.SelectedIndex = 0;

            var brandFilterItems = new List<string> { "All" };
            brandFilterItems.AddRange(_brands);
            ProductFilterBrandCombo.ItemsSource = brandFilterItems;
            ProductFilterBrandCombo.SelectedIndex = 0;

            // Set up category and brand lists
            CategoryListView.ItemsSource = _categories;
            BrandListView.ItemsSource = _brands;

            // Set info bar message
            MinProfitMarginInfoBar.Message = $"Minimum profit margin: {_minimumProfitMargin:P0} (system setting)";

            // Update product count
            UpdateProductCountText();
            UpdateSelectionSummary();
        }

        private void OnApplyScopeChanged(object sender, RoutedEventArgs e)
        {
            if (ProductSelectionBorder == null || CategorySelectionBorder == null || BrandSelectionBorder == null)
                return;

            // Hide all selection panels first
            ProductSelectionBorder.Visibility = Visibility.Collapsed;
            CategorySelectionBorder.Visibility = Visibility.Collapsed;
            BrandSelectionBorder.Visibility = Visibility.Collapsed;

            if (ApplyToSelectedRadio.IsChecked == true)
            {
                ProductSelectionBorder.Visibility = Visibility.Visible;
            }
            else if (ApplyToCategoryRadio.IsChecked == true)
            {
                CategorySelectionBorder.Visibility = Visibility.Visible;
            }
            else if (ApplyToBrandRadio.IsChecked == true)
            {
                BrandSelectionBorder.Visibility = Visibility.Visible;
            }
        }

        #region Product Filter Methods

        private void OnProductSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyProductFilters();
        }

        private void OnProductFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyProductFilters();
        }

        private void ApplyProductFilters()
        {
            if (ProductSearchBox == null || ProductFilterCategoryCombo == null || ProductFilterBrandCombo == null)
                return;

            var searchText = ProductSearchBox.Text?.ToLower() ?? "";
            var selectedCategory = ProductFilterCategoryCombo.SelectedItem as string ?? "All";
            var selectedBrand = ProductFilterBrandCombo.SelectedItem as string ?? "All";

            var filtered = _allProducts.Where(p =>
            {
                // Search filter
                var matchesSearch = string.IsNullOrEmpty(searchText) ||
                                   p.ProductName.ToLower().Contains(searchText);

                // Category filter
                var matchesCategory = selectedCategory == "All" ||
                                     p.CategoryName == selectedCategory;

                // Brand filter
                var matchesBrand = selectedBrand == "All" ||
                                  p.BrandName == selectedBrand;

                return matchesSearch && matchesCategory && matchesBrand;
            }).ToList();

            _filteredProducts.Clear();
            foreach (var product in filtered)
            {
                _filteredProducts.Add(product);
            }

            // Restore selection for visible items
            RestoreProductSelection();
            UpdateProductCountText();
        }

        private void RestoreProductSelection()
        {
            // Temporarily disable selection event
            ProductListView.SelectionChanged -= OnProductSelectionChanged;

            ProductListView.SelectedItems.Clear();
            foreach (var product in _filteredProducts)
            {
                if (_selectedProductIds.Contains(product.ProductId))
                {
                    ProductListView.SelectedItems.Add(product);
                }
            }

            // Re-enable selection event
            ProductListView.SelectionChanged += OnProductSelectionChanged;
        }

        private void OnProductSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add newly selected items
            foreach (ProductSelectionItem item in e.AddedItems)
            {
                _selectedProductIds.Add(item.ProductId);
            }

            // Remove deselected items
            foreach (ProductSelectionItem item in e.RemovedItems)
            {
                _selectedProductIds.Remove(item.ProductId);
            }

            UpdateSelectionSummary();
        }

        private void UpdateProductCountText()
        {
            if (ProductCountText != null)
            {
                ProductCountText.Text = $"{_filteredProducts.Count} products shown";
            }
        }

        private void UpdateSelectionSummary()
        {
            if (SelectionSummaryText != null)
            {
                SelectionSummaryText.Text = $"{_selectedProductIds.Count} products selected";
            }
        }

        #endregion

        #region Select/Deselect All Handlers

        private void OnSelectAllProducts(object sender, RoutedEventArgs e)
        {
            foreach (var product in _allProducts)
            {
                _selectedProductIds.Add(product.ProductId);
            }
            RestoreProductSelection();
            UpdateSelectionSummary();
        }

        private void OnSelectAllFilteredProducts(object sender, RoutedEventArgs e)
        {
            foreach (var product in _filteredProducts)
            {
                _selectedProductIds.Add(product.ProductId);
            }
            RestoreProductSelection();
            UpdateSelectionSummary();
        }

        private void OnDeselectAllProducts(object sender, RoutedEventArgs e)
        {
            _selectedProductIds.Clear();
            ProductListView.SelectedItems.Clear();
            UpdateSelectionSummary();
        }

        private void OnSelectAllCategories(object sender, RoutedEventArgs e)
        {
            CategoryListView.SelectAll();
        }

        private void OnDeselectAllCategories(object sender, RoutedEventArgs e)
        {
            CategoryListView.SelectedItems.Clear();
        }

        private void OnSelectAllBrands(object sender, RoutedEventArgs e)
        {
            BrandListView.SelectAll();
        }

        private void OnDeselectAllBrands(object sender, RoutedEventArgs e)
        {
            BrandListView.SelectedItems.Clear();
        }

        #endregion

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            // Validate input value
            if (double.IsNaN(BulkEditValueBox.Value) || BulkEditValueBox.Value < 0)
            {
                ErrorTextBlock.Text = "Please enter a valid positive number.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }

            // Validate scope selection
            if (ApplyToSelectedRadio.IsChecked == true && _selectedProductIds.Count == 0)
            {
                ErrorTextBlock.Text = "Please select at least one product.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }

            if (ApplyToCategoryRadio.IsChecked == true && CategoryListView.SelectedItems.Count == 0)
            {
                ErrorTextBlock.Text = "Please select at least one category.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }

            if (ApplyToBrandRadio.IsChecked == true && BrandListView.SelectedItems.Count == 0)
            {
                ErrorTextBlock.Text = "Please select at least one brand.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }

            // Validate profit margin if editing by profit margin
            if (EditByProfitMarginRadio.IsChecked == true)
            {
                var profitMargin = (float)(BulkEditValueBox.Value / 100.0);
                if (profitMargin < _minimumProfitMargin)
                {
                    ErrorTextBlock.Text = $"Profit margin must be at least {_minimumProfitMargin:P0} (system setting).";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }
            }

            // Determine scope and values
            BulkEditScope scope;
            List<int>? selectedProductIds = null;
            List<string>? selectedCategories = null;
            List<string>? selectedBrands = null;

            if (ApplyToAllRadio.IsChecked == true)
            {
                scope = BulkEditScope.All;
            }
            else if (ApplyToSelectedRadio.IsChecked == true)
            {
                scope = BulkEditScope.SelectedProducts;
                selectedProductIds = _selectedProductIds.ToList();
            }
            else if (ApplyToCategoryRadio.IsChecked == true)
            {
                scope = BulkEditScope.Categories;
                selectedCategories = CategoryListView.SelectedItems
                    .Cast<string>()
                    .ToList();
            }
            else
            {
                scope = BulkEditScope.Brands;
                selectedBrands = BrandListView.SelectedItems
                    .Cast<string>()
                    .ToList();
            }

            // Determine edit mode
            BulkEditMode mode;
            if (EditByProfitMarginRadio.IsChecked == true)
            {
                mode = BulkEditMode.ProfitMargin;
            }
            else if (EditByUnitCostRadio.IsChecked == true)
            {
                mode = BulkEditMode.UnitCost;
            }
            else
            {
                mode = BulkEditMode.SellingPrice;
            }

            Result = new BulkPriceEditResult(
                scope,
                selectedProductIds,
                selectedCategories,
                selectedBrands,
                mode,
                BulkEditValueBox.Value
            );
        }
    }

    public enum BulkEditScope
    {
        All,
        SelectedProducts,
        Categories,
        Brands
    }

    public enum BulkEditMode
    {
        ProfitMargin,
        UnitCost,
        SellingPrice
    }

    public class ProductSelectionItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }

        public string FormattedUnitCost => UnitCost.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
    }

    public class BulkPriceEditResult
    {
        public BulkEditScope Scope { get; }
        public List<int>? SelectedProductIds { get; }
        public List<string>? SelectedCategories { get; }
        public List<string>? SelectedBrands { get; }
        public BulkEditMode Mode { get; }
        public double Value { get; }

        public BulkPriceEditResult(
            BulkEditScope scope,
            List<int>? selectedProductIds,
            List<string>? selectedCategories,
            List<string>? selectedBrands,
            BulkEditMode mode,
            double value)
        {
            Scope = scope;
            SelectedProductIds = selectedProductIds;
            SelectedCategories = selectedCategories;
            SelectedBrands = selectedBrands;
            Mode = mode;
            Value = value;
        }
    }
}

