using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Implementations;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class InvoicePage : Page
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;

        public InvoicePage()
        {
            this.InitializeComponent();
            this.Loaded += InvoicePage_Loaded;
        }

        private void InvoicePage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadPurchaseOrders();
        }

        private void LoadPurchaseOrders()
        {
            
        }

        private void UpdateEmptyStateVisibility()
        {
            
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            
        }

        private void FilterChangeDate(object sender, CalendarDatePickerDateChangedEventArgs args)
        {
           
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void ShowErrorDialog(string title, string message)
        {
           
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void BtnActions_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void FilterChange(object sender, RoutedEventArgs e)
        {
           
        }

        private async void SelectSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        public async void LogMessage(string message)
        {
            
        }
    }
}