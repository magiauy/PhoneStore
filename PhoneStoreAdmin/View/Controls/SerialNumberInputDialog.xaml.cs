using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class SerialNumberInputDialog : ContentDialog, INotifyPropertyChanged
    {
        private string _productName = string.Empty;
        private int _quantity = 0;
        private bool _showValidationError = false;
        private string _validationMessage = string.Empty;
        private bool _isRangeInputMode = false;
        private string _startSerial = string.Empty;
        private string _generatedSerialsPreview = string.Empty;
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

        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        public bool IsRangeInputMode
        {
            get => _isRangeInputMode;
            set
            {
                if (SetProperty(ref _isRangeInputMode, value))
                {
                    OnPropertyChanged(nameof(RangeInputVisibility));
                    OnPropertyChanged(nameof(ManualInputVisibility));
                }
            }
        }

        public string StartSerial
        {
            get => _startSerial;
            set => SetProperty(ref _startSerial, value);
        }

        public string GeneratedSerialsPreview
        {
            get => _generatedSerialsPreview;
            set
            {
                if (SetProperty(ref _generatedSerialsPreview, value))
                {
                    OnPropertyChanged(nameof(HasGeneratedPreview));
                }
            }
        }

        public Visibility RangeInputVisibility => IsRangeInputMode ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ManualInputVisibility => IsRangeInputMode ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasGeneratedPreview => !string.IsNullOrEmpty(GeneratedSerialsPreview) ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<SerialEntry> SerialEntries { get; set; }

        // Constructor for creating new serial entries
        public SerialNumberInputDialog(string productName, int quantity)
        {
            _resourceLoader = new ResourceLoader();

            ProductName = productName;
            Quantity = quantity;

            SerialEntries = new ObservableCollection<SerialEntry>();
            for (int i = 0; i < quantity; i++)
            {
                var entry = new SerialEntry { Index = i + 1, ResourceLoader = _resourceLoader };
                entry.PropertyChanged += Entry_PropertyChanged;
                SerialEntries.Add(entry);
            }

            this.InitializeComponent();
            this.Title = _resourceLoader.GetString("SerialNumberDialogTitle");
        }

        // Constructor for editing existing serial entries
        public SerialNumberInputDialog(string productName, int quantity, List<SerialEntry> existingEntries)
        {
            _resourceLoader = new ResourceLoader();

            ProductName = productName;
            Quantity = quantity;

            SerialEntries = new ObservableCollection<SerialEntry>();
            
            // Load existing entries
            for (int i = 0; i < Math.Min(quantity, existingEntries.Count); i++)
            {
                var entry = new SerialEntry 
                { 
                    Index = i + 1,
                    SerialNumber = existingEntries[i].SerialNumber,
                    ResourceLoader = _resourceLoader
                };
                entry.PropertyChanged += Entry_PropertyChanged;
                SerialEntries.Add(entry);
            }

            // If quantity increased, add new empty entries
            for (int i = existingEntries.Count; i < quantity; i++)
            {
                var entry = new SerialEntry { Index = i + 1, ResourceLoader = _resourceLoader };
                entry.PropertyChanged += Entry_PropertyChanged;
                SerialEntries.Add(entry);
            }

            this.InitializeComponent();
            this.Title = _resourceLoader.GetString("SerialNumberDialogTitle");
        }

        private void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SerialEntry.SerialNumber))
                ValidateDuplicates();
        }

        private void ValidateDuplicates()
        {
            var duplicates = SerialEntries
                .GroupBy(s => s.SerialNumber?.Trim()?.ToLower())
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
                .Select(g => g.Key?.ToUpper())
                .ToList();

            if (duplicates.Any())
            {
                ValidationMessage = $"Duplicate serials: {string.Join(", ", duplicates)}";
                ShowValidationError = true;
            }
            else
            {
                ShowValidationError = false;
                ValidationMessage = string.Empty;
            }
        }

        public string GetQuantityText()
        {
            return string.Format(_resourceLoader.GetString("SerialNumberDialogQuantityText"), Quantity);
        }

        private void OnInputModeToggled(object sender, RoutedEventArgs e)
        {
            // Clear validation when switching modes
            ShowValidationError = false;
            ValidationMessage = string.Empty;
        }

        private void OnStartSerialChanged(object sender, TextChangedEventArgs e)
        {
            // Clear preview when start serial changes
            GeneratedSerialsPreview = string.Empty;
        }

        private void OnGenerateSerials(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StartSerial))
            {
                ValidationMessage = "Please enter starting serial";
                ShowValidationError = true;
                return;
            }

            ShowValidationError = false;

            // Parse the start serial to extract prefix and number
            var (prefix, startNumber, numberLength) = ParseSerial(StartSerial.Trim());

            if (startNumber < 0)
            {
                // No number found, just use StartSerial for all (not ideal but handles edge case)
                for (int i = 0; i < SerialEntries.Count; i++)
                {
                    SerialEntries[i].SerialNumber = $"{StartSerial.Trim()}-{i + 1}";
                }
                GeneratedSerialsPreview = $"Đã tạo: {SerialEntries[0].SerialNumber} → {SerialEntries[^1].SerialNumber}";
                return;
            }

            // Generate sequential serials
            for (int i = 0; i < SerialEntries.Count; i++)
            {
                var newNumber = startNumber + i;
                var formattedNumber = newNumber.ToString().PadLeft(numberLength, '0');
                SerialEntries[i].SerialNumber = $"{prefix}{formattedNumber}";
            }

            // Show preview
            if (SerialEntries.Count > 1)
            {
                GeneratedSerialsPreview = $"Generated: {SerialEntries[0].SerialNumber} → {SerialEntries[^1].SerialNumber}";
            }
            else
            {
                GeneratedSerialsPreview = $"Generated: {SerialEntries[0].SerialNumber}";
            }
        }

        /// <summary>
        /// Parse serial to extract prefix, starting number, and number length
        /// Example: "SN001" returns ("SN", 1, 3)
        /// Example: "PHONE-0050" returns ("PHONE-", 50, 4)
        /// </summary>
        private (string prefix, long startNumber, int numberLength) ParseSerial(string serial)
        {
            // Find the last sequence of digits in the string
            var match = Regex.Match(serial, @"^(.*?)(\d+)$");
            
            if (match.Success)
            {
                var prefix = match.Groups[1].Value;
                var numberStr = match.Groups[2].Value;
                var startNumber = long.Parse(numberStr);
                return (prefix, startNumber, numberStr.Length);
            }

            // No trailing number found
            return (serial, -1, 0);
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Validate: check for empty serial numbers
            var empty = SerialEntries.Where(e => string.IsNullOrWhiteSpace(e.SerialNumber)).ToList();

            if (empty.Any())
            {
                args.Cancel = true;
                ValidationMessage = $"{empty.Count} product(s) missing Serial Number";
                ShowValidationError = true;
                return;
            }

            // Validate: check for duplicates
            ValidateDuplicates();
            if (ShowValidationError)
            {
                args.Cancel = true;
                return;
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

        public string GetMachineTitle()
        {
            var template = ResourceLoader?.GetString("MachineNumberLabel") ?? "Unit {0}";
            return string.Format(template, Index);
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