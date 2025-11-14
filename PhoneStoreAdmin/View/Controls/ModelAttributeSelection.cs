using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class ModelAttributeSelection : INotifyPropertyChanged
    {
        private bool _isSelected;

        public ModelAttributeSelection(ProductAttributeDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ProductAttributeDefinition Definition { get; }
        public ProductAttribute Attribute => Definition.Attribute;
        public int AttributeId => Attribute.Id;
        public string DisplayName => Attribute.Name ?? string.Empty;
        public string Description => Attribute.Note ?? string.Empty;
        public Visibility DescriptionVisibility => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;
        public string DataTypeLabel => string.Format("Type: {0}", GetDataTypeDisplay(Attribute.DataType));

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        private static string GetDataTypeDisplay(AttributeDataType dataType)
        {
            return dataType switch
            {
                AttributeDataType.NUMBER => "Number",
                AttributeDataType.DATE => "Date",
                AttributeDataType.BOOLEAN => "Yes/No",
                _ => "Text"
            };
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
