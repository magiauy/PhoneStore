using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
namespace PhoneStoreAdmin.View
{
    public sealed partial class SuppliersPage : Page
    {
        private readonly ISupplierRepository _supplierRepository;
        public ObservableCollection<SupplierViewModel> Suppliers { get; } = new ObservableCollection<SupplierViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;

        public int TotalRecords { get; set; } = 0;

        public SuppliersPage()
        {
            this.InitializeComponent();
            _supplierRepository = App.GetService<ISupplierRepository>();
            this.Loaded += SuppliersPage_Loaded;
        }
        private async void SuppliersPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSuppliersAsync();
        }
        private async Task LoadSuppliersAsync()
        {
            try
            {
                Suppliers.Clear();

                if (FilterActiveBoxSupplier == null ||
                    FilterNameBoxSupplier == null ||
                    FilterPhoneBoxSupplier == null ||
                    FilterEmailBoxSupplier == null ||
                    FilterAddressBoxSupplier == null ||
                    FilterTaxBoxSupplier == null)
                {
                    return;
                }

                bool? isActive = null;
                if (FilterActiveBoxSupplier.SelectedItem is ComboBoxItem item)
                {
                    isActive = item.Tag?.ToString() switch
                    {
                        "1" => true,
                        "0" => false,
                        _ => null
                    };
                }

                var nameFilter = FilterNameBoxSupplier.Text ?? string.Empty;
                var phoneFilter = FilterPhoneBoxSupplier.Text ?? string.Empty;
                var emailFilter = FilterEmailBoxSupplier.Text ?? string.Empty;
                var addressFilter = FilterAddressBoxSupplier.Text ?? string.Empty;
                var taxFilter = FilterTaxBoxSupplier.Text ?? string.Empty;

                // Gọi repo mới
                var result = await _supplierRepository.GetSuppliersFiltered(
                    nameFilter,
                    phoneFilter,
                    emailFilter,
                    addressFilter,
                    taxFilter,
                    isActive,
                    CurrentPage,
                    PageSize);

                TotalPages = result.Info.totalPages;
                TotalRecords = result.Info.totalRecords;

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

                // Update UI
                PageInfoText.Text = $"{CurrentPage} / {TotalPages}";
                PreviousPageButton.IsEnabled = CurrentPage > 1;
                NextPageButton.IsEnabled = CurrentPage < TotalPages;

                RecordCountText.Text = $"{Suppliers.Count} / {TotalRecords}";
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Failed to load suppliers", ex.Message);
            }
        }
        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            await LoadSuppliersAsync();
        }
        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilterPanel.Visibility == Visibility.Collapsed)
            {
                FilterPanel.Visibility = Visibility.Visible;
            }
            else
            {
                FilterPanel.Visibility = Visibility.Collapsed;
            }
        }
        private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            CurrentPage = 1;
            await LoadSuppliersAsync();
        }
        private async void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Safely clear filter values with null checks
                if (FilterNameBoxSupplier != null)
                    FilterNameBoxSupplier.Text = string.Empty;
                if (FilterPhoneBoxSupplier != null)
                    FilterPhoneBoxSupplier.Text = string.Empty;
                if (FilterEmailBoxSupplier != null)
                    FilterEmailBoxSupplier.Text = string.Empty;
                if (FilterAddressBoxSupplier != null)
                    FilterAddressBoxSupplier.Text = string.Empty;
                if (FilterTaxBoxSupplier != null)
                    FilterTaxBoxSupplier.Text = string.Empty;
                if (FilterActiveBoxSupplier != null)
                    FilterActiveBoxSupplier.SelectedIndex = 0;
                CurrentPage = 1;
                await LoadSuppliersAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Error clearing filters", ex.Message);
            }
        }
        private async Task ShowErrorDialogAsync(string title, string message)
        {
            try
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
            catch (Exception ex)
            {
                // Fallback: just log the error if dialog fails
                System.Diagnostics.Debug.WriteLine($"Dialog error: {ex.Message}");
            }
        }
        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            await ShowErrorDialogAsync("Info", "Create supplier functionality - Coming Soon!");
        }
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            if (int.TryParse(id, out int supplierId))
            {
                await ShowErrorDialogAsync("Info", $"Edit supplier ID: {supplierId} - Coming Soon!");
            }
        }
        private async void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            if (int.TryParse(id, out int supplierId))
            {
                var supplier = await _supplierRepository.GetByIdAsync(supplierId);
                if (supplier != null)
                {
                    var details = $"ID: {supplier.Id}\n" +
                    $"Name: {supplier.Name}\n" +
                    $"Phone: {supplier.Phone ?? "N/A"}\n" +
                    $"Email: {supplier.Email ?? "N/A"}\n" +
                    $"Address: {supplier.Address ?? "N/A"}\n" +
                    $"Tax Number: {supplier.TaxNumber ?? "N/A"}\n" +
                    $"Status: {(supplier.IsActive ? "Active" : "Inactive")}";
                    await ShowErrorDialogAsync("Supplier Details", details);
                }
            }
        }
        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            if (int.TryParse(id, out int supplierId))
            {
                try
                {
                    var supplier = await _supplierRepository.GetByIdAsync(supplierId);
                    if (supplier != null)
                    {
                        supplier.IsActive = true;
                        await _supplierRepository.UpdateAsync(supplier);
                        await LoadSuppliersAsync(); // Refresh the list
                        await ShowErrorDialogAsync("Success", "Supplier activated successfully!");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("Error", $"Failed to activate supplier: {ex.Message}");
                }
            }
        }
        private async void BtnDeActivate_Click(object sender, RoutedEventArgs e)
        {
            var id = (sender as Button)?.Tag?.ToString();
            if (int.TryParse(id, out int supplierId))
            {
                try
                {
                    var supplier = await _supplierRepository.GetByIdAsync(supplierId);
                    if (supplier != null)
                    {
                        supplier.IsActive = false;
                        await _supplierRepository.UpdateAsync(supplier);
                        await LoadSuppliersAsync(); // Refresh the list
                        await ShowErrorDialogAsync("Success", "Supplier deactivated successfully!");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("Error", $"Failed to deactivate supplier: {ex.Message}");
                }
            }
        }
        
        private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadSuppliersAsync();
            }
        }
        private async void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage >= 1)
            {
                CurrentPage--;
                await LoadSuppliersAsync();
            }
        }

        private async void FilterChange(object sender, RoutedEventArgs e)
        {
            try
            {
                CurrentPage = 1;
                await LoadSuppliersAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("Error clearing filters", ex.Message);
            }
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
    public class SupplierViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string StatusColor => IsActive ? "#28a745" : "#dc3545";
    }
}