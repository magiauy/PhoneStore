using System;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class EditProfileDialog : UserControl
    {
        public event EventHandler<bool> ProfileUpdated;
        public event EventHandler DialogClosed;

        private readonly ResourceLoader _resourceLoader;
        private readonly IPersonService _personService;
        private string _originalEmail = "";
        private string _originalFullName = "";
        private string _originalPhone = "";
        private bool _hasChanges = false;

        public EditProfileDialog()
        {
            this.InitializeComponent();
            _resourceLoader = new ResourceLoader();
            _personService = ServiceContainer.GetService<IPersonService>();
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                var userSession = UserSession.Instance;
                if (userSession.IsLoggedIn && userSession.Person != null)
                {
                    var person = userSession.Person;
                    
                    // Set username (read-only)
                    UsernameTextBox.Text = userSession.Account?.Username ?? "";
                    
                    // Set editable fields
                    EmailTextBox.Text = person.Email ?? "";
                    FullNameTextBox.Text = person.FullName ?? "";
                    PhoneTextBox.Text = person.Phone ?? "";
                    
                    // Store original values
                    _originalEmail = EmailTextBox.Text;
                    _originalFullName = FullNameTextBox.Text;
                    _originalPhone = PhoneTextBox.Text;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading user data: {ex.Message}");
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateEmail();
            CheckForChanges();
        }

        private void FullNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFullName();
            CheckForChanges();
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePhone();
            CheckForChanges();
        }

        private bool ValidateEmail()
        {
            var email = EmailTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(email))
            {
                ShowEmailError(_resourceLoader.GetString("EmailRequired/Text") ?? "Email is required");
                return false;
            }

            // Email regex pattern
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                ShowEmailError(_resourceLoader.GetString("EmailInvalid/Text") ?? "Please enter a valid email address");
                return false;
            }

            HideEmailError();
            return true;
        }

        private bool ValidateFullName()
        {
            var fullName = FullNameTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(fullName))
            {
                ShowFullNameError(_resourceLoader.GetString("FullNameRequired/Text") ?? "Full name is required");
                return false;
            }

            if (fullName.Length < 2)
            {
                ShowFullNameError(_resourceLoader.GetString("FullNameTooShort/Text") ?? "Full name must be at least 2 characters");
                return false;
            }

            HideFullNameError();
            return true;
        }

        private bool ValidatePhone()
        {
            var phone = PhoneTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(phone))
            {
                HidePhoneError();
                return true; // Phone is optional
            }

            // Phone number pattern (international format)
            var phonePattern = @"^[\+]?[\d\s\-\(\)]{10,15}$";
            if (!Regex.IsMatch(phone, phonePattern))
            {
                ShowPhoneError(_resourceLoader.GetString("PhoneInvalid/Text") ?? "Please enter a valid phone number");
                return false;
            }

            HidePhoneError();
            return true;
        }

        private void CheckForChanges()
        {
            _hasChanges = EmailTextBox.Text != _originalEmail ||
                         FullNameTextBox.Text != _originalFullName ||
                         PhoneTextBox.Text != _originalPhone;

            SaveButton.IsEnabled = _hasChanges && ValidateEmail() && ValidateFullName() && ValidatePhone();
        }

        private void ShowEmailError(string message)
        {
            EmailError.Text = message;
            EmailError.Visibility = Visibility.Visible;
        }

        private void HideEmailError()
        {
            EmailError.Visibility = Visibility.Collapsed;
        }

        private void ShowFullNameError(string message)
        {
            FullNameError.Text = message;
            FullNameError.Visibility = Visibility.Visible;
        }

        private void HideFullNameError()
        {
            FullNameError.Visibility = Visibility.Collapsed;
        }

        private void ShowPhoneError(string message)
        {
            PhoneError.Text = message;
            PhoneError.Visibility = Visibility.Visible;
        }

        private void HidePhoneError()
        {
            PhoneError.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
            SuccessInfoBar.IsOpen = false;
        }

        private void ShowSuccess(string message)
        {
            SuccessInfoBar.Message = message;
            SuccessInfoBar.IsOpen = true;
            ErrorInfoBar.IsOpen = false;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateEmail() || !ValidateFullName() || !ValidatePhone())
                {
                    return;
                }

                LoadingOverlay.Visibility = Visibility.Visible;
                SaveButton.IsEnabled = false;

                var userSession = UserSession.Instance;
                if (userSession.Person == null)
                {
                    ShowError(_resourceLoader.GetString("UserNotLoggedIn/Text") ?? "User not logged in");
                    return;
                }

                // Update person data
                var updatedPerson = new Person
                {
                    Id = userSession.Person.Id,
                    Email = EmailTextBox.Text.Trim(),
                    FullName = FullNameTextBox.Text.Trim(),
                    Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                    // Keep other fields unchanged
                    CreatedAt = userSession.Person.CreatedAt,
                };

                // Call service to update person
                var success = await _personService.UpdatePersonAsync(updatedPerson);

                if (success)
                {
                    // Update session with new data
                    userSession.UpdatePersonInfo(updatedPerson);
                    
                    // Update original values
                    _originalEmail = EmailTextBox.Text;
                    _originalFullName = FullNameTextBox.Text;
                    _originalPhone = PhoneTextBox.Text;
                    
                    _hasChanges = false;
                    SaveButton.IsEnabled = false;

                    ShowSuccess(_resourceLoader.GetString("ProfileUpdateSuccessful/Text") ?? "Profile updated successfully!");
                    
                    // Notify parent
                    ProfileUpdated?.Invoke(this, true);
                    
                    // Auto-close after success (optional, can be removed if you want manual close)
                }
                else
                {
                    ShowError(_resourceLoader.GetString("ProfileUpdateError/Text") ?? "Failed to update profile. Please try again.");
                    ProfileUpdated?.Invoke(this, false);
                }
            }
            catch (Exception ex)
            {
                ShowError($"{_resourceLoader.GetString("UnexpectedError/Text") ?? "An unexpected error occurred"}: {ex.Message}");
                ProfileUpdated?.Invoke(this, false);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                if (_hasChanges)
                {
                    SaveButton.IsEnabled = true;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Focus on the email field when dialog opens
        /// </summary>
        public void FocusEmailField()
        {
            EmailTextBox.Focus(FocusState.Programmatic);
        }
    }
}