using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Models;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStoreAdmin.View.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class AccountEditPage : Page
    {
        private readonly IAccountService _accountService;
        private readonly IPersonService _personService;
        private readonly IRoleService _roleService;
        private readonly ResourceLoader _resourceLoader = new();

        private Account? _currentAccount;
        private Person? _currentPerson;
        private List<Role> _allRoles = new();
        private List<int> _selectedRoleIds = new();
        private int _accountId;

        public AccountEditPage()
        {
            InitializeComponent();
            InitializeLocalizedStrings();

            _accountService = ServiceContainer.GetService<IAccountService>()
                ?? throw new InvalidOperationException("AccountService is not registered.");
            _personService = ServiceContainer.GetService<IPersonService>()
                ?? throw new InvalidOperationException("PersonService is not registered.");
            _roleService = ServiceContainer.GetService<IRoleService>()
                ?? throw new InvalidOperationException("RoleService is not registered.");
        }

        private void InitializeLocalizedStrings()
        {
            // Set button contents
            SaveButton.Content = _resourceLoader.GetString("AccountEdit_SaveButton");
            CancelButton.Content = _resourceLoader.GetString("Common/Cancel");
            SelectRolesButton.Content = _resourceLoader.GetString("AccountEdit_SelectRolesButton");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is AccountViewModel vm)
            {
                _accountId = vm.Id;
                UpdateHeaderTexts(vm.Username, vm.FullName);
            }
            else if (e.Parameter is int id)
            {
                _accountId = id;
                UpdateHeaderTexts(null, null);
            }
            else
            {
                ShowError(_resourceLoader.GetString("AccountEdit_Error_UnableToDetermineAccount"));
                SaveButton.IsEnabled = false;
                return;
            }

            await LoadAccountAsync();
        }

        private async Task LoadAccountAsync()
        {
            try
            {
                SaveButton.IsEnabled = false;
                ErrorInfoBar.IsOpen = false;

                _currentAccount = await _accountService.GetAccountWithRolesAsync(_accountId);
                if (_currentAccount == null)
                {
                    ShowError(_resourceLoader.GetString("AccountEdit_Error_LoadFailed"));
                    return;
                }

                _currentPerson = _currentAccount.Person ?? await _personService.GetPersonByIdAsync(_currentAccount.PersonId);
                if (_currentPerson != null)
                {
                    _currentAccount.Person = _currentPerson;
                }

                _allRoles = await _roleService.GetAllRolesAsync() ?? new List<Role>();
                _selectedRoleIds = _currentAccount.AccountRoles?.Select(ar => ar.RoleId).ToList() ?? new List<int>();

                UsernameTextBox.Text = _currentAccount.Username;
                FullNameTextBox.Text = _currentPerson?.FullName ?? string.Empty;
                EmailTextBox.Text = _currentPerson?.Email ?? string.Empty;
                PhoneTextBox.Text = _currentPerson?.Phone ?? string.Empty;
                IsActiveToggle.IsOn = _currentAccount.IsActive;

                UpdateHeaderTexts(_currentAccount.Username, _currentPerson?.FullName);
                UpdateSelectedRolesText();
                SaveButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load account for editing", ex);
                ShowError(string.Format(_resourceLoader.GetString("AccountEdit_Error_LoadException"), ex.Message));
            }
        }

        private void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
        }

        private void UpdateSelectedRolesText()
        {
            if (_selectedRoleIds.Count == 0)
            {
                SelectedRolesTextBlock.Text = _resourceLoader.GetString("AccountEdit_NoRolesSelected");
                return;
            }

            var roleNames = _allRoles
                .Where(r => _selectedRoleIds.Contains(r.Id))
                .Select(r => r.Name ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            if (roleNames.Count == 0 && _currentAccount?.AccountRoles != null)
            {
                roleNames = _currentAccount.AccountRoles
                    .Select(ar => ar.Role?.Name ?? string.Empty)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
            }

            SelectedRolesTextBlock.Text = roleNames.Count > 0
                ? string.Join(", ", roleNames)
                : string.Format(_resourceLoader.GetString("AccountEdit_RolesSelectedCount"), _selectedRoleIds.Count);
        }

        private async void SelectRolesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new RoleMultiSelectorDialog(_roleService, _selectedRoleIds)
                {
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    _selectedRoleIds = dialog.SelectedRoleIds;
                    UpdateSelectedRolesText();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening role selector dialog", ex);
                ShowError(_resourceLoader.GetString("AccountEdit_Error_RoleSelectorFailed"));
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentAccount == null || _currentPerson == null)
            {
                ShowError(_resourceLoader.GetString("AccountEdit_Error_DataNotLoaded"));
                return;
            }

            ErrorInfoBar.IsOpen = false;
            var username = UsernameTextBox.Text?.Trim() ?? string.Empty;
            var fullName = FullNameTextBox.Text?.Trim() ?? string.Empty;
            var email = EmailTextBox.Text?.Trim();
            var phone = PhoneTextBox.Text?.Trim();
            var password = PasswordBox.Password?.Trim();
            var confirmPassword = ConfirmPasswordBox.Password?.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError(_resourceLoader.GetString("AccountEdit_Error_UsernameEmpty"));
                UsernameTextBox.Focus(FocusState.Programmatic);
                return;
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError(_resourceLoader.GetString("AccountEdit_Error_FullNameEmpty"));
                FullNameTextBox.Focus(FocusState.Programmatic);
                return;
            }

            if (!string.IsNullOrEmpty(password))
            {
                if (password.Length < 6)
                {
                    ShowError(_resourceLoader.GetString("AccountEdit_Error_PasswordTooShort"));
                    PasswordBox.Focus(FocusState.Programmatic);
                    return;
                }

                if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
                {
                    ShowError(_resourceLoader.GetString("AccountEdit_Error_PasswordMismatch"));
                    ConfirmPasswordBox.Focus(FocusState.Programmatic);
                    return;
                }
            }

            try
            {
                SaveButton.IsEnabled = false;

                _currentAccount.Username = username;
                _currentAccount.IsActive = IsActiveToggle.IsOn;

                if (!string.IsNullOrEmpty(password))
                {
                    _currentAccount.PasswordHash = PasswordHasher.HashPassword(password);
                }

                _currentPerson.FullName = fullName;
                _currentPerson.Email = string.IsNullOrWhiteSpace(email) ? null : email;
                _currentPerson.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
                _currentPerson.IsActive = _currentAccount.IsActive;

                var personUpdated = await _personService.UpdatePersonAsync(_currentPerson);
                var accountUpdated = await _accountService.UpdateAccountAsync(_currentAccount);
                var rolesUpdated = await _accountService.UpdateAccountRolesAsync(_currentAccount.Id, _selectedRoleIds);

                if (!personUpdated || !accountUpdated || !rolesUpdated)
                {
                    ShowError(_resourceLoader.GetString("AccountEdit_Error_SaveFailed"));
                    SaveButton.IsEnabled = true;
                    return;
                }

                await ShowSuccessDialog(_resourceLoader.GetString("AccountEdit_Success_Updated"));

                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update account", ex);
                ShowError(string.Format(_resourceLoader.GetString("AccountEdit_Error_Exception"), ex.Message));
                SaveButton.IsEnabled = true;
            }
        }

        private async Task ShowSuccessDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("Common/Success"),
                Content = message,
                CloseButtonText = _resourceLoader.GetString("Common/Close"),
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                Frame.Navigate(typeof(AccountsPage));
            }
        }

        private void UpdateHeaderTexts(string? username, string? fullName)
        {
            var displayName = !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : !string.IsNullOrWhiteSpace(username)
                    ? username
                    : $"#{_accountId}";

            // Update RichTextBlock - dynamic content set in code-behind
            BreadcrumbHomeText.Text = _resourceLoader.GetString("Common/AccountManagement");
            BreadcrumbCurrentText.Text = string.Format(_resourceLoader.GetString("AccountEdit_BreadcrumbCurrent"), displayName);
            SubtitleTextBlock.Text = string.Format(_resourceLoader.GetString("AccountEdit_Subtitle"), displayName);
        }

        private void BreadcrumbHome_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AccountsPage));
        }
    }
}
