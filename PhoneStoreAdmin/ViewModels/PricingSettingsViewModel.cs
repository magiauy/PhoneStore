using PhoneStore.Services.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PhoneStoreAdmin.ViewModels
{
    /// <summary>
    /// ViewModel for Pricing Settings Page
    /// Binds configuration values, provides validation logic and save command
    /// Requirements: 9.1, 9.2, 9.3
    /// </summary>
    public class PricingSettingsViewModel : INotifyPropertyChanged
    {
        private double _desiredMarginPercent = 10;
        private double _minimumMarginPercent = 5;
        private double _varianceThresholdPercent = -10;
        private bool _hasChanges;
        private string? _validationError;

        /// <summary>
        /// Biên lợi nhuận mong muốn (%) - Default: 10%
        /// </summary>
        public double DesiredMarginPercent
        {
            get => _desiredMarginPercent;
            set
            {
                if (_desiredMarginPercent != value)
                {
                    _desiredMarginPercent = value;
                    OnPropertyChanged();
                    HasChanges = true;
                    Validate();
                }
            }
        }

        /// <summary>
        /// Biên lợi nhuận tối thiểu (%) - Default: 5%
        /// </summary>
        public double MinimumMarginPercent
        {
            get => _minimumMarginPercent;
            set
            {
                if (_minimumMarginPercent != value)
                {
                    _minimumMarginPercent = value;
                    OnPropertyChanged();
                    HasChanges = true;
                    Validate();
                }
            }
        }

        /// <summary>
        /// Ngưỡng chênh lệch giá (%) - Default: -10%
        /// </summary>
        public double VarianceThresholdPercent
        {
            get => _varianceThresholdPercent;
            set
            {
                if (_varianceThresholdPercent != value)
                {
                    _varianceThresholdPercent = value;
                    OnPropertyChanged();
                    HasChanges = true;
                    Validate();
                }
            }
        }

        /// <summary>
        /// Indicates if there are unsaved changes
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (_hasChanges != value)
                {
                    _hasChanges = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Validation error message, null if valid
        /// </summary>
        public string? ValidationError
        {
            get => _validationError;
            set
            {
                if (_validationError != value)
                {
                    _validationError = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        /// <summary>
        /// Indicates if all values are valid
        /// </summary>
        public bool IsValid => string.IsNullOrEmpty(ValidationError);

        /// <summary>
        /// Load values from PricingConfiguration
        /// </summary>
        public void LoadFromConfiguration(PricingConfiguration config)
        {
            _desiredMarginPercent = (double)(config.DesiredMargin * 100);
            _minimumMarginPercent = (double)(config.MinimumMargin * 100);
            _varianceThresholdPercent = (double)(config.VarianceThreshold * 100);
            
            OnPropertyChanged(nameof(DesiredMarginPercent));
            OnPropertyChanged(nameof(MinimumMarginPercent));
            OnPropertyChanged(nameof(VarianceThresholdPercent));
            
            HasChanges = false;
            Validate();
        }

        /// <summary>
        /// Convert ViewModel values to PricingConfiguration
        /// </summary>
        public PricingConfiguration ToConfiguration()
        {
            return new PricingConfiguration
            {
                DesiredMargin = (decimal)DesiredMarginPercent / 100m,
                MinimumMargin = (decimal)MinimumMarginPercent / 100m,
                VarianceThreshold = (decimal)VarianceThresholdPercent / 100m,
                StableRangeMax = 0m,
                StableRangeMin = -0.05m
            };
        }

        /// <summary>
        /// Validate all input values
        /// </summary>
        public bool Validate()
        {
            // Validate Desired Margin
            if (double.IsNaN(DesiredMarginPercent) || DesiredMarginPercent < 0 || DesiredMarginPercent > 100)
            {
                ValidationError = "Biên lợi nhuận mong muốn phải từ 0% đến 100%";
                return false;
            }

            // Validate Minimum Margin
            if (double.IsNaN(MinimumMarginPercent) || MinimumMarginPercent < 0 || MinimumMarginPercent > 100)
            {
                ValidationError = "Biên lợi nhuận tối thiểu phải từ 0% đến 100%";
                return false;
            }

            // Validate Variance Threshold
            if (double.IsNaN(VarianceThresholdPercent) || VarianceThresholdPercent < -100 || VarianceThresholdPercent > 0)
            {
                ValidationError = "Ngưỡng chênh lệch giá phải từ -100% đến 0%";
                return false;
            }

            // Validate Minimum Margin <= Desired Margin
            if (MinimumMarginPercent > DesiredMarginPercent)
            {
                ValidationError = "Biên lợi nhuận tối thiểu không được lớn hơn biên lợi nhuận mong muốn";
                return false;
            }

            ValidationError = null;
            return true;
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        public void ResetToDefaults()
        {
            DesiredMarginPercent = 10;
            MinimumMarginPercent = 5;
            VarianceThresholdPercent = -10;
            HasChanges = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
