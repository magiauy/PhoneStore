using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class EmployeeDialog : ContentControl
    {
        private readonly IEmployeeService _employeeService;

        private bool _isCodeValid;
        private bool _isNameValid;
        private bool _isEmailValid = true;
        private bool _isPhoneValid = true;
        private bool _isHireDateValid;

        private readonly Regex _emailRegex = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$");
        private readonly Regex _phoneRegex = new("^(\\+84|0)[3|5|7|8|9][0-9]{8}$");

        public enum DialogMode
        {
            Add,
            Edit,
            View
        }

        private DialogMode _currentMode = DialogMode.Add;

        public EmployeeDialog()
        {
            InitializeComponent();

            _employeeService = ServiceContainer.GetService<IEmployeeService>()
                ?? throw new InvalidOperationException("EmployeeService not registered");
        }

        public async Task SetModeAsync(DialogMode mode, EmployeeViewModel? employee = null)
        {
            _currentMode = mode;
            await LoadDataAsync(employee);
            UpdateUIForMode();
        }

        public void SetMode(DialogMode mode, EmployeeViewModel? employee = null)
        {
            _currentMode = mode;
            // For sync usage, load without fetching latest from DB
            LoadDataSync(employee);
            UpdateUIForMode();
        }

        public bool ValidateInputs()
        {
            ValidateCode(EmployeeCodeTextBox.Text ?? string.Empty);
            ValidateName(FullNameTextBox.Text ?? string.Empty);
            ValidateEmail(EmailTextBox.Text ?? string.Empty);
            ValidatePhone(PhoneTextBox.Text ?? string.Empty);
            ValidateHireDate(HireDatePicker.SelectedDate);

            return _isCodeValid && _isNameValid && _isEmailValid && _isPhoneValid && _isHireDateValid;
        }

        public Employee BuildEmployee()
        {
            var employee = new Employee
            {
                Id = int.TryParse(EmployeeIdTextBox.Text, out var id) ? id : 0,
                Code = EmployeeCodeTextBox.Text?.Trim(),
                FullName = FullNameTextBox.Text?.Trim() ?? string.Empty,
                Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                PersonType = PersonType.EMPLOYEE,
                IsActive = IsActiveToggle.IsOn
            };

            if (HireDatePicker.SelectedDate.HasValue)
            {
                employee.HireDate = HireDatePicker.SelectedDate.Value.DateTime;
            }

            return employee;
        }

        public void SetLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

            var isEnabled = !isLoading && _currentMode != DialogMode.View;
            EmployeeCodeTextBox.IsEnabled = isEnabled;
            FullNameTextBox.IsEnabled = isEnabled;
            EmailTextBox.IsEnabled = isEnabled;
            PhoneTextBox.IsEnabled = isEnabled;
            HireDatePicker.IsEnabled = isEnabled;
            IsActiveToggle.IsEnabled = isEnabled;
        }

        public void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
        }

        public void HideError()
        {
            ErrorInfoBar.IsOpen = false;
        }

        private async Task LoadDataAsync(EmployeeViewModel? employee)
        {
            EmployeeViewModel? source = employee;

            if (employee != null && _currentMode != DialogMode.Add)
            {
                var latest = await _employeeService.GetEmployeeByIdAsync(employee.Id);
                if (latest != null)
                {
                    source = new EmployeeViewModel(latest);
                }
            }

            PopulateFields(source);
        }

        private void LoadDataSync(EmployeeViewModel? employee)
        {
            // Just use the passed employee without fetching from DB
            PopulateFields(employee);
        }

        private void PopulateFields(EmployeeViewModel? source)
        {
            if (source == null)
            {
                EmployeeIdTextBox.Text = string.Empty;
                EmployeeCodeTextBox.Text = string.Empty;
                FullNameTextBox.Text = string.Empty;
                EmailTextBox.Text = string.Empty;
                PhoneTextBox.Text = string.Empty;
                HireDatePicker.SelectedDate = null;
                IsActiveToggle.IsOn = true;
                return;
            }

            EmployeeIdTextBox.Text = source.Id.ToString();
            EmployeeCodeTextBox.Text = source.Code;
            FullNameTextBox.Text = source.FullName;
            EmailTextBox.Text = source.Email;
            PhoneTextBox.Text = source.Phone;

            if (DateTime.TryParse(source.HireDate, out var parsedDate))
            {
                HireDatePicker.SelectedDate = new DateTimeOffset(parsedDate);
            }
            else
            {
                HireDatePicker.SelectedDate = null;
            }

            IsActiveToggle.IsOn = source.IsActive;

            ValidateInputs();
        }

        private void UpdateUIForMode()
        {
            var isEditable = _currentMode != DialogMode.View;

            // Hide ID field in Add mode
            EmployeeIdPanel.Visibility = _currentMode == DialogMode.Add 
                ? Visibility.Collapsed 
                : Visibility.Visible;

            EmployeeCodeTextBox.IsReadOnly = !isEditable;
            FullNameTextBox.IsReadOnly = !isEditable;
            EmailTextBox.IsReadOnly = !isEditable;
            PhoneTextBox.IsReadOnly = !isEditable;
            HireDatePicker.IsEnabled = isEditable;
            IsActiveToggle.IsEnabled = isEditable;
        }

        private void EmployeeCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentMode == DialogMode.View)
            {
                return;
            }

            ValidateCode(EmployeeCodeTextBox.Text ?? string.Empty);
        }

        private void FullNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentMode == DialogMode.View)
            {
                return;
            }

            ValidateName(FullNameTextBox.Text ?? string.Empty);
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentMode == DialogMode.View)
            {
                return;
            }

            ValidateEmail(EmailTextBox.Text ?? string.Empty);
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentMode == DialogMode.View)
            {
                return;
            }

            ValidatePhone(PhoneTextBox.Text ?? string.Empty);
        }

        private void HireDatePicker_SelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
        {
            if (_currentMode == DialogMode.View)
            {
                return;
            }

            ValidateHireDate(args.NewDate);
        }

        private void ValidateCode(string code)
        {
            _isCodeValid = !string.IsNullOrWhiteSpace(code);
            if (_isCodeValid)
            {
                HideValidationError(EmployeeCodeError);
            }
            else
            {
                ShowValidationError(EmployeeCodeError, "Employee code is required");
            }
        }

        private void ValidateName(string name)
        {
            _isNameValid = !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2;
            if (_isNameValid)
            {
                HideValidationError(FullNameError);
            }
            else
            {
                ShowValidationError(FullNameError, "Full name must be at least 2 characters");
            }
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _isEmailValid = true;
                HideValidationError(EmailError);
                return;
            }

            _isEmailValid = _emailRegex.IsMatch(email);
            if (_isEmailValid)
            {
                HideValidationError(EmailError);
            }
            else
            {
                ShowValidationError(EmailError, "Invalid email format");
            }
        }

        private void ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                _isPhoneValid = true;
                HideValidationError(PhoneError);
                return;
            }

            _isPhoneValid = _phoneRegex.IsMatch(phone);
            if (_isPhoneValid)
            {
                HideValidationError(PhoneError);
            }
            else
            {
                ShowValidationError(PhoneError, "Invalid phone number format");
            }
        }

        private void ValidateHireDate(DateTimeOffset? hireDate)
        {
            if (!hireDate.HasValue)
            {
                _isHireDateValid = false;
                ShowValidationError(HireDateError, "Please select a hire date");
                return;
            }

            var value = hireDate.Value.Date;
            if (value > DateTimeOffset.Now.Date)
            {
                _isHireDateValid = false;
                ShowValidationError(HireDateError, "Hire date cannot be in the future");
                return;
            }

            _isHireDateValid = true;
            HideValidationError(HireDateError);
        }

        private void ShowValidationError(TextBlock target, string message)
        {
            target.Text = message;
            target.Visibility = Visibility.Visible;
        }

        private void HideValidationError(TextBlock target)
        {
            target.Visibility = Visibility.Collapsed;
        }
    }
}
