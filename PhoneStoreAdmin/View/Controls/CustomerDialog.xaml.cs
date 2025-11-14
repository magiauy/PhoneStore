using System;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class CustomerDialog : ContentControl
    {
        private readonly ICustomerService _customerService;
        private readonly ResourceLoader _resourceLoader;

        private bool _isNameValid;
        private bool _isPhoneValid;
        private bool _isEmailValid;
        private bool _isAddressValid;

        private readonly Regex _emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly Regex _phoneRegex = new Regex(@"^(\+84|0)[3|5|7|8|9][0-9]{8}$", RegexOptions.Compiled);

        public event EventHandler<CustomerViewModel>? CustomerSaved;
        public event EventHandler? DialogClosed;

        public enum DialogMode
        {
            Add,
            Edit,
            View
        }

        private DialogMode _currentMode = DialogMode.Add;

        public CustomerDialog()
        {
            this.InitializeComponent();
            _customerService = ServiceContainer.GetService<ICustomerService>();
            _resourceLoader = new ResourceLoader();
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            ResetValidationStates();
            HideAllErrors();
            UpdateValidationUI();
        }

        #region Validation Events

        private void CustomerNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateName(CustomerNameTextBox.Text ?? string.Empty);
            UpdateValidationUI();
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePhone(PhoneTextBox.Text ?? string.Empty);
            UpdateValidationUI();
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateEmail(EmailTextBox.Text ?? string.Empty);
            UpdateValidationUI();
        }

        private void AddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAddress(AddressTextBox.Text ?? string.Empty);
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
            _isNameValid = !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2;

            if (!_isNameValid)
            {
                ShowError(CustomerNameError, _resourceLoader.GetString("NameRequired/Text") ?? "Vui lòng nhập tên hợp lệ (tối thiểu 2 ký tự)");
            }
            else
            {
                HideError(CustomerNameError);
            }
        }

        private void ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                _isPhoneValid = false;
                ShowError(PhoneError, "Vui lòng nhập số điện thoại hợp lệ");
                return;
            }

            _isPhoneValid = _phoneRegex.IsMatch(phone);

            if (!_isPhoneValid)
            {
                ShowError(PhoneError, "Số điện thoại không hợp lệ");
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
                _isEmailValid = true;
                HideError(EmailError);
                return;
            }

            _isEmailValid = _emailRegex.IsMatch(email);

            if (!_isEmailValid)
            {
                ShowError(EmailError, _resourceLoader.GetString("InvalidEmail/Text") ?? "Định dạng email không hợp lệ");
            }
            else
            {
                HideError(EmailError);
            }
        }

        private void ValidateAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                _isAddressValid = true;
                HideError(AddressError);
                return;
            }

            _isAddressValid = address.Trim().Length >= 5;

            if (!_isAddressValid)
            {
                ShowError(AddressError, _resourceLoader.GetString("AddressTooShort/Text") ?? "Địa chỉ cần ít nhất 5 ký tự");
            }
            else
            {
                HideError(AddressError);
            }
        }

        private void UpdateValidationUI()
        {
            bool isFormValid = _isNameValid && _isPhoneValid && _isEmailValid && _isAddressValid;

            if (_currentMode == DialogMode.View)
            {
                SuccessInfoBar.IsOpen = false;
                return;
            }

            SuccessInfoBar.IsOpen = false;
            ErrorInfoBar.IsOpen = !isFormValid && (CustomerNameError.Visibility == Visibility.Visible
                                                   || PhoneError.Visibility == Visibility.Visible
                                                   || EmailError.Visibility == Visibility.Visible
                                                   || AddressError.Visibility == Visibility.Visible);
        }

        private void ValidateAllFields()
        {
            ValidateName(CustomerNameTextBox.Text ?? string.Empty);
            ValidatePhone(PhoneTextBox.Text ?? string.Empty);
            ValidateEmail(EmailTextBox.Text ?? string.Empty);
            ValidateAddress(AddressTextBox.Text ?? string.Empty);
        }

        #endregion

        #region UI Helpers

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
            HideError(CustomerNameError);
            HideError(PhoneError);
            HideError(EmailError);
            HideError(AddressError);
            ErrorInfoBar.IsOpen = false;
            SuccessInfoBar.IsOpen = false;
        }

        private void ResetValidationStates()
        {
            _isNameValid = false;
            _isPhoneValid = false;
            _isEmailValid = true;
            _isAddressValid = true;
        }

        private void ShowLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

            CustomerNameTextBox.IsEnabled = !isLoading && _currentMode != DialogMode.View;
            PhoneTextBox.IsEnabled = !isLoading && _currentMode != DialogMode.View;
            EmailTextBox.IsEnabled = !isLoading && _currentMode != DialogMode.View;
            AddressTextBox.IsEnabled = !isLoading && _currentMode != DialogMode.View;
            IsActiveToggleSwitch.IsEnabled = !isLoading && _currentMode != DialogMode.View;
        }

        public void ShowErrorMessage(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
            SuccessInfoBar.IsOpen = false;
        }

        public void ShowSuccessMessage(string message)
        {
            SuccessInfoBar.Message = message;
            SuccessInfoBar.IsOpen = true;
            ErrorInfoBar.IsOpen = false;
        }

        #endregion

        #region Public API

        public void Reset()
        {
            SetMode(DialogMode.Add);
            ResetValidationStates();
            HideAllErrors();
            ShowLoading(false);
        }

        public void SetMode(DialogMode mode, CustomerViewModel? customer = null)
        {
            _currentMode = mode;
            LoadData(customer);
            UpdateUIForMode();
        }

        private void LoadData(CustomerViewModel? customer)
        {
            if (customer == null || _currentMode == DialogMode.Add)
            {
                CustomerIdTextBox.Text = string.Empty;
                CustomerNameTextBox.Text = string.Empty;
                PhoneTextBox.Text = string.Empty;
                EmailTextBox.Text = string.Empty;
                AddressTextBox.Text = string.Empty;
                IsActiveToggleSwitch.IsOn = true;

                ResetValidationStates();
                HideAllErrors();
                return;
            }

            CustomerIdTextBox.Text = customer.Id.ToString();
            CustomerNameTextBox.Text = customer.FullName;
            PhoneTextBox.Text = customer.Phone;
            EmailTextBox.Text = customer.Email;
            AddressTextBox.Text = customer.Address;
            IsActiveToggleSwitch.IsOn = customer.IsActive;

            ValidateAllFields();
        }

        private void UpdateUIForMode()
        {
            bool isEditable = _currentMode != DialogMode.View;

            CustomerIdTextBox.IsReadOnly = true;
            CustomerNameTextBox.IsReadOnly = !isEditable;
            PhoneTextBox.IsReadOnly = !isEditable;
            EmailTextBox.IsReadOnly = !isEditable;
            AddressTextBox.IsReadOnly = !isEditable;
            IsActiveToggleSwitch.IsEnabled = isEditable;
        }

        public void LoadById(int customerId)
        {
            try
            {
                ShowLoading(true);
                var customer = _customerService.GetCustomerById(customerId);
                if (customer != null)
                {
                    SetMode(DialogMode.Edit, new CustomerViewModel(customer));
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(_resourceLoader.GetString("LoadError/Text") ?? $"Không thể tải khách hàng: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        public bool ValidateForm()
        {
            ValidateAllFields();
            return _isNameValid && _isPhoneValid && _isEmailValid && _isAddressValid;
        }

        public Customer BuildCustomer()
        {
            return new Customer
            {
                Id = int.TryParse(CustomerIdTextBox.Text, out var id) ? id : 0,
                FullName = CustomerNameTextBox.Text?.Trim() ?? string.Empty,
                Phone = PhoneTextBox.Text?.Trim() ?? string.Empty,
                Email = EmailTextBox.Text?.Trim() ?? string.Empty,
                Address = AddressTextBox.Text?.Trim() ?? string.Empty,
                IsActive = IsActiveToggleSwitch.IsOn,
                PersonType = PersonType.CUSTOMER
            };
        }

        public void SetLoadingState(bool isLoading) => ShowLoading(isLoading);

        public void Close()
        {
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

        public void NotifySaved(Customer customer)
        {
            CustomerSaved?.Invoke(this, new CustomerViewModel(customer));
        }

        #endregion
    }
}
