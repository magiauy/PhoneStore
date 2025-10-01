using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PhoneStoreAdmin.ViewModels;
namespace PhoneStoreAdmin.View
{
    public sealed partial class SuppliersPage : Page
    {
        private ISupplierService SupplierService => App.GetService<ISupplierService>();

        public ObservableCollection<SupplierViewModel> Suppliers { get; } = new ObservableCollection<SupplierViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;

        public SuppliersPage()
        {
            this.InitializeComponent();
            this.Loaded += SuppliersPage_Loaded;
        }

        private void SuppliersPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            // Prevent execution if UI controls are not yet initialized
            if (!_isInitialized)
                return;

            try
            {
                Suppliers.Clear();

                bool? isActive = null;
                if (FilterActiveBoxSupplier?.SelectedItem is ComboBoxItem item)
                {
                    isActive = item.Tag?.ToString() switch
                    {
                        "1" => true,
                        "0" => false,
                        _ => null
                    };
                }

                var result = SupplierService.GetSuppliersFiltered(
                    FilterNameBoxSupplier?.Text,
                    FilterPhoneBoxSupplier?.Text,
                    FilterEmailBoxSupplier?.Text,
                    FilterAddressBoxSupplier?.Text,
                    FilterTaxBoxSupplier?.Text,
                    isActive,
                    CurrentPage,
                    PageSize);

                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to load suppliers.");
                    return;
                }

                TotalPages = result.Info.TotalPages;
                TotalRecords = result.Info.TotalRecords;

                foreach (var supplier in result.Suppliers)
                {
                    Suppliers.Add(new SupplierViewModel
                    {
                        Id = supplier.Id,
                        Name = supplier.Name,
                        Phone = supplier.Phone ?? string.Empty,
                        Email = supplier.Email ?? string.Empty,
                        Address = supplier.Address ?? string.Empty,
                        TaxNumber = supplier.TaxNumber ?? string.Empty,
                        IsActive = supplier.IsActive
                    });
                }

                // Safely update UI controls with null checks
                if (PageInfoText != null)
                    PageInfoText.Text = $"{CurrentPage} / {TotalPages}";

                if (PreviousPageButton != null)
                    PreviousPageButton.IsEnabled = CurrentPage > 1;

                if (NextPageButton != null)
                    NextPageButton.IsEnabled = CurrentPage < TotalPages;

                if (RecordCountText != null)
                    RecordCountText.Text = $"{Suppliers.Count} / {TotalRecords}";

                UpdateEmptyStateVisibility();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load suppliers", ex.Message);
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = Suppliers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (SuppliersListView != null)
                SuppliersListView.Visibility = Suppliers.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            LoadSuppliers();
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
            FilterNameBoxSupplier.Text = SearchBox.Text;
            CurrentPage = 1;
            LoadSuppliers();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FilterNameBoxSupplier?.ClearValue(TextBox.TextProperty);
                FilterPhoneBoxSupplier?.ClearValue(TextBox.TextProperty);
                FilterEmailBoxSupplier?.ClearValue(TextBox.TextProperty);
                FilterAddressBoxSupplier?.ClearValue(TextBox.TextProperty);
                FilterTaxBoxSupplier?.ClearValue(TextBox.TextProperty);
                FilterActiveBoxSupplier?.ClearValue(ComboBox.SelectedIndexProperty);
                SearchBox?.ClearValue(TextBox.TextProperty);

                CurrentPage = 1;
                LoadSuppliers();
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
            ShowErrorDialog("Info", "Create supplier functionality - Coming Soon!");
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            if (int.TryParse(id, out int supplierId))
            {
                ShowErrorDialog("Info", $"Edit supplier ID: {supplierId} - Coming Soon!");
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            if (int.TryParse(id, out int supplierId))
            {
                var supplier = SupplierService.GetSupplierById(supplierId);
                if (supplier != null)
                {
                    var details = $"ID: {supplier.Id}\n" +
                                  $"Name: {supplier.Name}\n" +
                                  $"Phone: {supplier.Phone ?? "N/A"}\n" +
                                  $"Email: {supplier.Email ?? "N/A"}\n" +
                                  $"Address: {supplier.Address ?? "N/A"}\n" +
                                  $"Tax Number: {supplier.TaxNumber ?? "N/A"}\n" +
                                  $"Status: {(supplier.IsActive ? "Active" : "Inactive")}";
                    ShowErrorDialog("Supplier Details", details);
                }
            }
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            if (int.TryParse(id, out int supplierId))
            {
                try
                {
                    SupplierService.ActivateSupplier(supplierId);
                    LoadSuppliers();
                    ShowErrorDialog("Success", "Supplier activated successfully!");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error", $"Failed to activate supplier: {ex.Message}");
                }
            }
        }

        private void BtnDeActivate_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            if (int.TryParse(id, out int supplierId))
            {
                try
                {
                    SupplierService.DeactivateSupplier(supplierId);
                    LoadSuppliers();
                    ShowErrorDialog("Success", "Supplier deactivated successfully!");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error", $"Failed to deactivate supplier: {ex.Message}");
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
                LoadSuppliers();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadSuppliers();
            }
        }

        private void FilterChange(object sender, RoutedEventArgs e)
        {
            // Only process filter changes if the page is fully initialized
            if (!_isInitialized)
                return;

            CurrentPage = 1;
            LoadSuppliers();
        }
    }

    public class SortDirectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                // Handle converter parameter for inverse logic
                if (parameter?.ToString() == "True")
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                else if (parameter?.ToString() == "False")
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                else
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}