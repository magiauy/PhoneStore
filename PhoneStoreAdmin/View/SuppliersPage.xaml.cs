using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class SuppliersPage : Page
    {
        private readonly ISupplierRepository _supplierRepository;

        public ObservableCollection<Supplier> Suppliers { get; } = new ObservableCollection<Supplier>();

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;

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

                // Ensure UI elements are initialized before accessing them
                if (FilterActiveBoxSupplier == null || 
                    FilterNameBoxSupplier == null || 
                    FilterPhoneBoxSupplier == null || 
                    FilterEmailBoxSupplier == null || 
                    FilterAddressBoxSupplier == null || 
                    FilterTaxBoxSupplier == null)
                {
                    return; // Exit early if UI elements aren't ready
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

                // Get filter values safely
                var nameFilter = FilterNameBoxSupplier.Text ?? string.Empty;
                var phoneFilter = FilterPhoneBoxSupplier.Text ?? string.Empty;
                var emailFilter = FilterEmailBoxSupplier.Text ?? string.Empty;
                var addressFilter = FilterAddressBoxSupplier.Text ?? string.Empty;
                var taxFilter = FilterTaxBoxSupplier.Text ?? string.Empty;

                // Execute repository calls on background thread
                var totalPagesTask = Task.Run(() => _supplierRepository.GetTotalPages(
                    nameFilter,
                    phoneFilter,
                    emailFilter,
                    addressFilter,
                    taxFilter,
                    isActive,
                    PageSize));

                var suppliersTask = Task.Run(() => _supplierRepository.GetSuppliersFiltered(
                    nameFilter,
                    phoneFilter,
                    emailFilter,
                    addressFilter,
                    taxFilter,
                    isActive,
                    CurrentPage,
                    PageSize));

                // Wait for both tasks to complete
                await Task.WhenAll(totalPagesTask, suppliersTask);

                TotalPages = await totalPagesTask;
                var suppliers = await suppliersTask;

                // Update UI on the main thread
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }
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
            // TODO: Logic mở form tạo nhà cung cấp
            await ShowErrorDialogAsync("Info", "Create button clicked");
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Logic chỉnh sửa nhà cung cấp
            await ShowErrorDialogAsync("Info", "Edit button clicked");
        }

        private async void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Logic xem chi tiết nhà cung cấp
            await ShowErrorDialogAsync("Info", "Detail button clicked");
        }

        private async void BtnDeActivate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Logic kích hoạt/hủy kích hoạt nhà cung cấp
            await ShowErrorDialogAsync("Info", "Deactivate button clicked");
        }

        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Logic kích hoạt/hủy kích hoạt nhà cung cấp
            await ShowErrorDialogAsync("Info", "Activate button clicked");
        }

        private async void BtnFirstPage_Click(object sender, RoutedEventArgs e) 
        { 
            CurrentPage = 1; 
            await LoadSuppliersAsync(); 
        }
        
        private async void BtnPrevPage_Click(object sender, RoutedEventArgs e) 
        { 
            if (CurrentPage > 1) 
            { 
                CurrentPage--; 
                await LoadSuppliersAsync(); 
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
            CurrentPage = TotalPages; 
            await LoadSuppliersAsync(); 
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
