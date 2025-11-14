using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreRepository.Models;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class ChangePasswordDialog : UserControl
    {
        private readonly IAuthService _authService;
        private readonly ResourceLoader _resourceLoader;
        private bool _isPasswordValid = false;
        private bool _isConfirmPasswordValid = false;
        private bool _isCurrentPasswordValid = false;

        // Password validation regex patterns
        private readonly Regex _upperCaseRegex = new Regex(@"[A-Z]");
        private readonly Regex _lowerCaseRegex = new Regex(@"[a-z]");
        private readonly Regex _numberRegex = new Regex(@"[0-9]");
        private readonly Regex _specialCharRegex = new Regex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");

        public event EventHandler<bool>? PasswordChanged;
        public event EventHandler? DialogClosed;

        // Property for resource access in XAML
        public string GetString(string key) => _resourceLoader.GetString(key);

        public ChangePasswordDialog()
        {
            this.InitializeComponent();
            _authService = ServiceContainer.GetService<IAuthService>();
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

        #region Password Validation Events

        private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            var password = passwordBox?.Password ?? string.Empty;
            
            ValidateCurrentPassword(password);
            UpdateValidationUI();
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            var password = passwordBox?.Password ?? string.Empty;
            
            ValidateNewPassword(password);
            ValidatePasswordMatch(); // Revalidate confirm password
            UpdateValidationUI();
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidatePasswordMatch();
            UpdateValidationUI();
        }

        #endregion

        #region Validation Logic

        private void ValidateCurrentPassword(string password)
        {
            _isCurrentPasswordValid = !string.IsNullOrWhiteSpace(password) && password.Length >= 1;
            
            if (!_isCurrentPasswordValid && !string.IsNullOrEmpty(password))
            {
                ShowError(CurrentPasswordError, _resourceLoader.GetString("CurrentPasswordRequired/Text") ?? "Current password is required");
            }
            else
            {
                HideError(CurrentPasswordError);
            }
        }

        private void ValidateNewPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                _isPasswordValid = false;
                UpdatePasswordStrength(0);
                //HideError(NewPasswordError);
                ResetPasswordRequirements();
                return;
            }

            // Check individual requirements
            bool hasLength = password.Length >= 8;
            bool hasUpper = _upperCaseRegex.IsMatch(password);
            bool hasLower = _lowerCaseRegex.IsMatch(password);
            bool hasNumber = _numberRegex.IsMatch(password);
            bool hasSpecial = _specialCharRegex.IsMatch(password);

            // Update requirement indicators
            UpdateRequirementIndicator(LengthIcon, hasLength);
            UpdateRequirementIndicator(UpperIcon, hasUpper);
            UpdateRequirementIndicator(LowerIcon, hasLower);
            UpdateRequirementIndicator(NumberIcon, hasNumber);
            UpdateRequirementIndicator(SpecialIcon, hasSpecial);

            UpdateRequirementLabel(LengthText, hasLength);
            UpdateRequirementLabel(UpperText, hasUpper);
            UpdateRequirementLabel(LowerText, hasLower);
            UpdateRequirementLabel(NumberText, hasNumber);
            UpdateRequirementLabel(SpecialText, hasSpecial);
            

            // Calculate strength
            int strength = 0;
            if (hasLength) strength += 20;
            if (hasUpper) strength += 20;
            if (hasLower) strength += 20;
            if (hasNumber) strength += 20;
            if (hasSpecial) strength += 20;

            UpdatePasswordStrength(strength);

            // Overall validation
            _isPasswordValid = hasLength && hasUpper && hasLower && hasNumber && hasSpecial;

            // Check if same as current password
            bool isSameAsCurrent = string.Equals(password, CurrentPasswordBox.Password, StringComparison.Ordinal);

        }

        private void UpdateRequirementLabel(TextBlock label, bool isValid)
        {
            label.Foreground = isValid ? new SolidColorBrush(Microsoft.UI.Colors.Green) : new SolidColorBrush(Microsoft.UI.Colors.Red);
        }

        private void ValidatePasswordMatch()
        {
            var newPassword = NewPasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                _isConfirmPasswordValid = false;
                HideError(ConfirmPasswordError);
                return;
            }

            _isConfirmPasswordValid = string.Equals(newPassword, confirmPassword, StringComparison.Ordinal);
            
            if (!_isConfirmPasswordValid)
            {
                ShowError(ConfirmPasswordError, _resourceLoader.GetString("PasswordsDoNotMatch/Text") ?? "Passwords do not match");
            }
            else
            {
                HideError(ConfirmPasswordError);
            }
        }

        private void UpdatePasswordStrength(int strength)
        {
            PasswordStrengthBar.Value = strength;
            
            string strengthText;
            SolidColorBrush strengthColor;
            
            if (strength == 0)
            {
                strengthText = "";
                strengthColor = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            }
            else if (strength < 60)
            {
                strengthText = _resourceLoader.GetString("PasswordStrengthWeak/Text") ?? "Weak";
                strengthColor = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            else if (strength < 80)
            {
                strengthText = _resourceLoader.GetString("PasswordStrengthGood/Text") ?? "Good";
                strengthColor = new SolidColorBrush(Microsoft.UI.Colors.Orange);
            }
            else if (strength < 100)
            {
                strengthText = _resourceLoader.GetString("PasswordStrengthStrong/Text") ?? "Strong";
                strengthColor = new SolidColorBrush(Microsoft.UI.Colors.Blue);
            }
            else
            {
                strengthText = _resourceLoader.GetString("PasswordStrengthExcellent/Text") ?? "Excellent";
                strengthColor = new SolidColorBrush(Microsoft.UI.Colors.Green);
            }

            PasswordStrengthText.Text = strengthText;
            PasswordStrengthBar.Foreground = strengthColor;
        }

        private void UpdateRequirementIndicator(FontIcon icon, bool isValid)
        {
            if (isValid)
            {
                icon.Glyph = "\uE73E"; // CheckMark
                icon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
                //In đậm
            }
            else
            {
                icon.Glyph = "\uE711"; // Cancel
                icon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            }
        }

        private void ResetPasswordRequirements()
        {
            UpdateRequirementIndicator(LengthIcon, false);
            UpdateRequirementIndicator(UpperIcon, false);
            UpdateRequirementIndicator(LowerIcon, false);
            UpdateRequirementIndicator(NumberIcon, false);
            UpdateRequirementIndicator(SpecialIcon, false);
            UpdateRequirementLabel(LengthText, false);
            UpdateRequirementLabel(UpperText, false);
            UpdateRequirementLabel(LowerText, false);
            UpdateRequirementLabel(NumberText, false);
            UpdateRequirementLabel(SpecialText, false);
        }

        private void UpdateValidationUI()
        {
            // Enable/disable the change password button
            bool isFormValid = _isCurrentPasswordValid && _isPasswordValid && _isConfirmPasswordValid;
            ChangePasswordButton.IsEnabled = isFormValid;
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
            HideError(CurrentPasswordError);
            //HideError(NewPasswordError);
            HideError(ConfirmPasswordError);
            ErrorInfoBar.IsOpen = false;
        }

        private void ResetValidationStates()
        {
            _isCurrentPasswordValid = false;
            _isPasswordValid = false;
            _isConfirmPasswordValid = false;
        }

        private void ShowLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            ChangePasswordButton.IsEnabled = !isLoading && _isCurrentPasswordValid && _isPasswordValid && _isConfirmPasswordValid;
            CancelButton.IsEnabled = !isLoading;
            
            // Disable all input fields during loading
            CurrentPasswordBox.IsEnabled = !isLoading;
            NewPasswordBox.IsEnabled = !isLoading;
            ConfirmPasswordBox.IsEnabled = !isLoading;
        }

        #endregion

        #region Button Events

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoading(true);
                HideAllErrors();

                var currentPassword = CurrentPasswordBox.Password;
                var newPassword = NewPasswordBox.Password;

                // Validate current user session
                var userSession = UserSession.Instance;
                if (!userSession.IsLoggedIn || userSession.Account == null)
                {
                    ErrorInfoBar.Message = _resourceLoader.GetString("UserNotLoggedIn/Text") ?? "User not logged in";
                    ErrorInfoBar.IsOpen = true;
                    return;
                }

                // Verify current password first
                var isCurrentPasswordValid = await _authService.VerifyPasswordAsync(userSession.Account.Username, currentPassword);
                if (!isCurrentPasswordValid)
                {
                    ShowError(CurrentPasswordError, _resourceLoader.GetString("CurrentPasswordIncorrect/Text") ?? "Current password is incorrect");
                    return;
                }

                // Change password
                var result = await _authService.ChangePasswordAsync(userSession.Account.Username, currentPassword, newPassword);
                if (result != null)
                {
                    
                    // Clear all fields
                    CurrentPasswordBox.Password = "";
                    NewPasswordBox.Password = "";
                    ConfirmPasswordBox.Password = "";
                    
                    // Reset validation states
                    ResetValidationStates();
                    ResetPasswordRequirements();
                    UpdatePasswordStrength(0);
                    UpdateValidationUI();
                    
                    // Notify parent
                    PasswordChanged?.Invoke(this, true);
                    
                    DialogClosed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Failed
                    ErrorInfoBar.Message = _resourceLoader.GetString("PasswordChangeError/Text") ?? "Failed to change password. Please try again.";
                    ErrorInfoBar.IsOpen = true;
                    
                    // Notify parent
                    PasswordChanged?.Invoke(this, false);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                ErrorInfoBar.Message = _resourceLoader.GetString("UnexpectedError/Text") ?? $"An unexpected error occurred: {ex.Message}";
                ErrorInfoBar.IsOpen = true;
                
                // Notify parent
                PasswordChanged?.Invoke(this, false);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear all fields
            CurrentPasswordBox.Password = "";
            NewPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
            
            // Reset validation states
            ResetValidationStates();
            ResetPasswordRequirements();
            UpdatePasswordStrength(0);
            HideAllErrors();
            UpdateValidationUI();
            
            // Close dialog
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reset the dialog to initial state
        /// </summary>
        public void Reset()
        {
            CurrentPasswordBox.Password = "";
            NewPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
            
            ResetValidationStates();
            ResetPasswordRequirements();
            UpdatePasswordStrength(0);
            HideAllErrors();
            UpdateValidationUI();
            ShowLoading(false);
        }

        /// <summary>
        /// Focus on the current password field
        /// </summary>
        public void FocusCurrentPassword()
        {
            CurrentPasswordBox.Focus(FocusState.Programmatic);
        }

        #endregion
    }
}
