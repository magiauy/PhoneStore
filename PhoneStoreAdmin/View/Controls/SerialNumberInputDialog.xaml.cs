using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class SerialNumberInputDialog : ContentDialog, INotifyPropertyChanged
    {
        private string _productName = string.Empty;
        private int _quantity = 0;
        private bool _showValidationError = false;
        private readonly ResourceLoader _resourceLoader;

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public bool ShowValidationError
        {
            get => _showValidationError;
            set => SetProperty(ref _showValidationError, value);
        }

        public ObservableCollection<SerialEntry> SerialEntries { get; set; }

        public SerialNumberInputDialog(string productName, int quantity)
        {
            _resourceLoader = new ResourceLoader();
            
            ProductName = productName;
            Quantity = quantity;
            
            SerialEntries = new ObservableCollection<SerialEntry>();
            
            // Create entries for each quantity
            for (int i = 0; i < quantity; i++)
            {
                SerialEntries.Add(new SerialEntry { Index = i + 1, ResourceLoader = _resourceLoader });
            }

            this.InitializeComponent();
            this.Title = _resourceLoader.GetString("SerialNumberDialogTitle");
        }

        public string GetQuantityText()
        {
            return string.Format(_resourceLoader.GetString("SerialNumberDialogQuantityText"), Quantity);
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Validate all entries have serial numbers
            var invalidEntries = SerialEntries.Where(e => string.IsNullOrWhiteSpace(e.SerialNumber) || string.IsNullOrWhiteSpace(e.Imei1)).ToList();
            
            if (invalidEntries.Any())
            {
                args.Cancel = true;
                ShowValidationError = true;
            }
        }

        private void ValidationErrorInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            ShowValidationError = false;
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

        public ResourceLoader? ResourceLoader { get; set; }

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
            var template = ResourceLoader?.GetString("MachineNumberLabel") ?? "Machine {0}";
            return string.Format(template, Index);
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
