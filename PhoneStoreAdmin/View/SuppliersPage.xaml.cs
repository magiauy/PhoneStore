using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Implementations;
using System;
using System.Collections.ObjectModel;

namespace PhoneStoreAdmin.View
{
    public sealed partial class SuppliersPage : Page
    {
        private readonly SupplierRepository _repository;
        public ObservableCollection<Supplier> Suppliers { get; set; } = new ObservableCollection<Supplier>();

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;

        public SuppliersPage()
        {
            this.InitializeComponent();
            _repository = new SupplierRepository(App.DataSource);
            this.Loaded += SuppliersPage_Loaded;
        }

        private void SuppliersPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            Suppliers.Clear();

            bool? active = null;
            if (FilterActiveBoxSupplier.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag.ToString() == "1") active = true;
                else if (item.Tag.ToString() == "0") active = false;
            }

            // Tính tổng trang
            TotalPages = _repository.GetTotalPages(
                FilterNameBoxSupplier.Text,
                FilterPhoneBoxSupplier.Text,
                FilterEmailBoxSupplier.Text,
                FilterAddressBoxSupplier.Text,
                FilterTaxBoxSupplier.Text,
                active,
                PageSize
            );

            var list = _repository.GetSuppliersFiltered(
                FilterNameBoxSupplier.Text,
                FilterPhoneBoxSupplier.Text,
                FilterEmailBoxSupplier.Text,
                FilterAddressBoxSupplier.Text,
                FilterTaxBoxSupplier.Text,
                active,
                CurrentPage,
                PageSize
            );

            foreach (var supplier in list)
                Suppliers.Add(supplier);
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            LoadSuppliers();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterNameBoxSupplier.Text = "";
            FilterPhoneBoxSupplier.Text = "";
            FilterEmailBoxSupplier.Text = "";
            FilterAddressBoxSupplier.Text = "";
            FilterTaxBoxSupplier.Text = "";
            FilterActiveBoxSupplier.SelectedIndex = 0;

            CurrentPage = 1;
            LoadSuppliers();
        }

        private void BtnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            LoadSuppliers();
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadSuppliers();
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
            CurrentPage = TotalPages;
            LoadSuppliers();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int supplierId)
            {
                // TODO: mở form sửa nhà cung cấp
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int supplierId)
            {
                _repository.Delete(supplierId);
                LoadSuppliers();
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int supplierId)
            {
                // TODO: mở form chi tiết nhà cung cấp
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
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
}
