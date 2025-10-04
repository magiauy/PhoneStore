using System;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class SupplierDialog : ContentControl
    {
        private readonly ISupplierService _supplierService;
        private readonly ResourceLoader _resourceLoader;
        private bool _isNameValid = false;
        private bool _isPhoneValid = false;
        private bool _isEmailValid = false;
        private bool _isTaxNumberValid = false;

        // Validation regex patterns
        private readonly Regex _emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private readonly Regex _phoneRegex = new Regex(@"^(\+84|0)[3|5|7|8|9][0-9]{8}$"); // Vietnamese phone
        private readonly Regex _taxNumberRegex = new Regex(@"^\d{10}$|^\d{13}$"); // Vietnamese tax

        public event EventHandler<SupplierViewModel>? SupplierSaved;
        public event EventHandler? DialogClosed;

        // Enum for dialog mode
        public enum DialogMode { Add, Edit, View }

        private DialogMode _currentMode = DialogMode.Add;

        // Property for resource access in XAML
        public string GetString(string key) => _resourceLoader.GetString(key);

        public SupplierDialog()
        {
            this.InitializeComponent();
            _supplierService = ServiceContainer.GetService<ISupplierService>();
            _resourceLoader = new ResourceLoader();
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // Reset all validation states
            ResetValidationStates();

            // Hide all error messages
            HideAllErrors();

            // Update validation UI
            UpdateValidationUI();
        }

        #region Field Validation Events

        private void SupplierNameTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            var text = SupplierNameTextBox.Text ?? string.Empty;

            ValidateName(text);
            UpdateValidationUI();
        }

        private void PhoneTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            var text = PhoneTextBox.Text ?? string.Empty;

            ValidatePhone(text);
            UpdateValidationUI();
        }

        private void EmailTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            var text = EmailTextBox.Text ?? string.Empty;

            ValidateEmail(text);
            UpdateValidationUI();
        }

        private void AddressTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            var text = AddressTextBox.Text ?? string.Empty;

            ValidateAddress(text);
            UpdateValidationUI();
        }

        private void TaxNumberTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            var text = TaxNumberTextBox.Text ?? string.Empty;

            ValidateTaxNumber(text);
            UpdateValidationUI();
        }

        private void IsActiveToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateValidationUI();
        }

        #endregion

        #region Validation Logic

        private void ValidateName(string name)
        {
            _isNameValid = !string.IsNullOrWhiteSpace(name) && name.Length >= 2;

            if (!_isNameValid && !string.IsNullOrEmpty(name))
            {
                ShowError(SupplierNameError, GetString("NameRequired/Text") ?? "Name is required (min 2 characters)");
            }
            else
            {
                HideError(SupplierNameError);
            }
        }

        private void ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                _isPhoneValid = true; // Optional
                HideError(PhoneError);
                return;
            }

            _isPhoneValid = _phoneRegex.IsMatch(phone);

            if (!_isPhoneValid && !string.IsNullOrEmpty(phone))
            {
                ShowError(PhoneError, GetString("InvalidPhone/Text") ?? "Invalid phone format");
            }
            else
            {
                HideError(PhoneError);
            }
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _isEmailValid = true; // Optional
                HideError(EmailError);
                return;
            }

            _isEmailValid = _emailRegex.IsMatch(email);

            if (!_isEmailValid && !string.IsNullOrEmpty(email))
            {
                ShowError(EmailError, GetString("InvalidEmail/Text") ?? "Invalid email format");
            }
            else
            {
                HideError(EmailError);
            }
        }

        private void ValidateAddress(string address)
        {
            // Optional field, always valid
            HideError(AddressError);
        }

        private void ValidateTaxNumber(string taxNumber)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
            {
                _isTaxNumberValid = true; // Optional
                HideError(TaxNumberError);
                return;
            }

            _isTaxNumberValid = _taxNumberRegex.IsMatch(taxNumber);

            if (!_isTaxNumberValid && !string.IsNullOrEmpty(taxNumber))
            {
                ShowError(TaxNumberError, GetString("InvalidTaxNumber/Text") ?? "Invalid tax number (10 or 13 digits)");
            }
            else
            {
                HideError(TaxNumberError);
            }
        }

        private void UpdateValidationUI()
        {
            bool isFormValid = _isNameValid && _isPhoneValid && _isEmailValid && _isTaxNumberValid;
        }

        #endregion

        #region UI Helper Methods

        private void ShowError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private void HideError(TextBlock errorTextBlock)
        {
            errorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void HideAllErrors()
        {
            HideError(SupplierNameError);
            HideError(PhoneError);
            HideError(EmailError);
            HideError(AddressError);
            HideError(TaxNumberError);
            ErrorInfoBar.IsOpen = false;
        }

        private void ResetValidationStates()
        {
            _isNameValid = false;
            _isPhoneValid = false;
            _isEmailValid = false;
            _isTaxNumberValid = false;
        }

        private void ShowLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

            // Disable inputs during loading
            SupplierNameTextBox.IsEnabled = !isLoading;
            PhoneTextBox.IsEnabled = !isLoading;
            EmailTextBox.IsEnabled = !isLoading;
            AddressTextBox.IsEnabled = !isLoading;
            TaxNumberTextBox.IsEnabled = !isLoading;
            IsActiveToggleSwitch.IsEnabled = !isLoading && _currentMode != DialogMode.View;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reset the dialog to initial state
        /// </summary>
        public void Reset()
        {
            SetMode(DialogMode.Add);
            ResetValidationStates();
            HideAllErrors();
            UpdateValidationUI();
            ShowLoading(false);
        }

        /// <summary>
        /// Set mode and load data
        /// </summary>
        public void SetMode(DialogMode mode, SupplierViewModel? supplier = null)
        {
            _currentMode = mode;
            LoadData(supplier);
            UpdateUIForMode();
        }

        private void LoadData(SupplierViewModel? supplier)
        {
            if (supplier == null || _currentMode == DialogMode.Add)
            {
                SupplierIdTextBox.Text = "";
                SupplierNameTextBox.Text = "";
                PhoneTextBox.Text = "";
                EmailTextBox.Text = "";
                AddressTextBox.Text = "";
                TaxNumberTextBox.Text = "";
                IsActiveToggleSwitch.IsOn = true;
                return;
            }

            SupplierIdTextBox.Text = supplier.Id.ToString();
            SupplierNameTextBox.Text = supplier.Name;
            PhoneTextBox.Text = supplier.Phone;
            EmailTextBox.Text = supplier.Email;
            AddressTextBox.Text = supplier.Address;
            TaxNumberTextBox.Text = supplier.TaxNumber;
            IsActiveToggleSwitch.IsOn = supplier.IsActive;

            // Re-validate loaded data
            ValidateName(supplier.Name);
            ValidatePhone(supplier.Phone);
            ValidateEmail(supplier.Email);
            ValidateTaxNumber(supplier.TaxNumber);
            UpdateValidationUI();
        }

        private void UpdateUIForMode()
        {
            bool isEditable = _currentMode != DialogMode.View;

            SupplierIdTextBox.IsReadOnly = true;
            SupplierNameTextBox.IsReadOnly = !isEditable;
            PhoneTextBox.IsReadOnly = !isEditable;
            EmailTextBox.IsReadOnly = !isEditable;
            AddressTextBox.IsReadOnly = !isEditable;
            TaxNumberTextBox.IsReadOnly = !isEditable;
            IsActiveToggleSwitch.IsEnabled = isEditable;
        }

        /// <summary>
        /// Load supplier by ID
        /// </summary>
        public void LoadById(int supplierId)
        {
            try
            {
                ShowLoading(true);
                var supplier = _supplierService.GetSupplierById(supplierId);
                SetMode(supplier == null ? DialogMode.Add : DialogMode.Edit, new SupplierViewModel(supplier));
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = GetString("LoadError/Text") ?? $"Load error: {ex.Message}";
                ErrorInfoBar.IsOpen = true;
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Focus on name field
        /// </summary>
        public void FocusNameField()
        {
            SupplierNameTextBox.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Save the supplier (call from parent ContentDialog PrimaryButtonClick)
        /// </summary>
        public void Save()
        {
            if (_currentMode == DialogMode.View)
            {
                DialogClosed?.Invoke(this, EventArgs.Empty);
                return;
            }

            bool isFormValid = _isNameValid && _isPhoneValid && _isEmailValid && _isTaxNumberValid;
            if (!isFormValid)
            {
                // Show validation errors if any
                ValidateAllFields();
                return;
            }

            try
            {
                ShowLoading(true);
                HideAllErrors();

                var supplier = new Supplier
                {
                    Id = int.TryParse(SupplierIdTextBox.Text, out int id) ? id : 0,
                    Name = SupplierNameTextBox.Text ?? string.Empty,
                    Phone = PhoneTextBox.Text ?? string.Empty,
                    Email = EmailTextBox.Text ?? string.Empty,
                    Address = AddressTextBox.Text ?? string.Empty,
                    TaxNumber = TaxNumberTextBox.Text ?? string.Empty,
                    IsActive = IsActiveToggleSwitch.IsOn
                };

                if (_currentMode == DialogMode.Add)
                {
                    _supplierService.Insert(supplier);
                }
                else
                {
                    _supplierService.Update(supplier);
                }

                // Clear fields for add
                if (_currentMode == DialogMode.Add)
                {
                    Reset();
                }

                // Notify parent
                var viewModel = new SupplierViewModel(supplier);
                SupplierSaved?.Invoke(this, viewModel);

                DialogClosed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // Handle exceptions
                ErrorInfoBar.Message = GetString("UnexpectedError/Text") ?? $"An unexpected error occurred: {ex.Message}";
                ErrorInfoBar.IsOpen = true;

                SupplierSaved?.Invoke(this, null);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Cancel the dialog (call from parent ContentDialog CloseButtonClick)
        /// </summary>
        public void Cancel()
        {
            ResetValidationStates();
            HideAllErrors();
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

        private void ValidateAllFields()
        {
            ValidateName(SupplierNameTextBox.Text ?? string.Empty);
            ValidatePhone(PhoneTextBox.Text ?? string.Empty);
            ValidateEmail(EmailTextBox.Text ?? string.Empty);
            ValidateTaxNumber(TaxNumberTextBox.Text ?? string.Empty);
        }

        #endregion
    }
}