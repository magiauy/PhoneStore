using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PhoneStore.Services.Helpers;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStore.Services.ViewModels.ProductAttributes
{
    public class ProductAttributeViewModel : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private AttributeDataType _dataType = AttributeDataType.TEXT;
        private string? _note;
        private int _optionCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        public AttributeDataType DataType
        {
            get => _dataType;
            set
            {
                if (SetProperty(ref _dataType, value))
                {
                    OnPropertyChanged(nameof(DataTypeDisplay));
                }
            }
        }

        public string? Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        public int OptionCount
        {
            get => _optionCount;
            set => SetProperty(ref _optionCount, value);
        }

        public string DataTypeDisplay
        {
            get
            {
                var key = DataType switch
                {
                    AttributeDataType.TEXT => "AttributeDataType_Text/Text",
                    AttributeDataType.NUMBER => "AttributeDataType_Number/Text",
                    AttributeDataType.DATE => "AttributeDataType_Date/Text",
                    AttributeDataType.BOOLEAN => "AttributeDataType_Boolean/Text",
                    _ => "AttributeDataType_Text/Text"
                };

                return LocalizationHelper.GetString(key) ?? DataType.ToString();
            }
        }

        public ProductAttributeViewModel()
        {
        }

        public ProductAttributeViewModel(ProductAttribute attribute, int optionCount)
        {
            UpdateFromModel(attribute, optionCount);
        }

        public void UpdateFromModel(ProductAttribute attribute, int optionCount)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            Id = attribute.Id;
            Name = attribute.Name;
            DataType = attribute.DataType;
            Note = attribute.Note;
            OptionCount = optionCount;
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProductAttributeOptionViewModel : INotifyPropertyChanged
    {
        private int _id;
        private string _displayValue = string.Empty;
        private string? _normalizedValue;
        private int _sortOrder;
        private bool _isActive = true;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string DisplayValue
        {
            get => _displayValue;
            set => SetProperty(ref _displayValue, value ?? string.Empty);
        }

        public string? NormalizedValue
        {
            get => _normalizedValue;
            set => SetProperty(ref _normalizedValue, value);
        }

        public int SortOrder
        {
            get => _sortOrder;
            set => SetProperty(ref _sortOrder, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        public string StatusText
        {
            get
            {
                var key = IsActive ? "Status_Active" : "Status_Inactive";
                return LocalizationHelper.GetString(key) ?? (IsActive ? "Active" : "Inactive");
            }
        }

        public ProductAttributeOptionViewModel()
        {
        }

        public ProductAttributeOptionViewModel(ProductAttributeOption option)
        {
            UpdateFromModel(option);
        }

        public void UpdateFromModel(ProductAttributeOption option)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            Id = option.Id;
            DisplayValue = option.DisplayValue;
            NormalizedValue = option.NormalizedValue;
            SortOrder = option.SortOrder;
            IsActive = option.IsActive;
            CreatedAt = option.CreatedAt;
            UpdatedAt = option.UpdatedAt;
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class DataTypeFilterOption
    {
        public DataTypeFilterOption(AttributeDataType? value, string label)
        {
            Value = value;
            Label = label;
        }

        public AttributeDataType? Value { get; }

        public string Label { get; }
    }
}
