using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Models;
using Windows.Globalization;

namespace PhoneStoreAdmin.View
{
    public sealed partial class SettingsPage : Page
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly ResourceLoader _resourceLoader;
        private bool _isLoadingSettings = false;

        public SettingsPage()
        {
            this.InitializeComponent();
            _localStorageService = ServiceContainer.GetService<ILocalStorageService>();
            _resourceLoader = new ResourceLoader();
            LoadUserInfo();
            LoadSettings();
        }

        private void LoadUserInfo()
        {
            try
            {
                var userSession = UserSession.Instance;
                
                if (userSession.IsLoggedIn)
                {
                    // Set user display name
                    if (UserNameTextBlock != null)
                    {
                        UserNameTextBlock.Text = userSession.GetDisplayName();
                    }

                    // Set user email
                    if (UserEmailTextBlock != null)
                    {
                        var email = userSession.Person?.Email;
                        if (!string.IsNullOrEmpty(email))
                        {
                            UserEmailTextBlock.Text = email;
                        }
                        else
                        {
                            UserEmailTextBlock.Text = _resourceLoader.GetString("NoEmailAvailable/Text") ?? "No email available";
                        }
                    }

                    // Set user role
                    if (UserRoleTextBlock != null)
                    {
                        var roleName = userSession.GetRoleName();
                        UserRoleTextBlock.Text = roleName;
                    }

                    // Set user permissions info
                    if (UserPermissionsTextBlock != null)
                    {
                        var permissionCount = userSession.GetPermissionCount();
                        UserPermissionsTextBlock.Text = $"Permissions: {permissionCount}";
                    }

                    // Set additional user info if available
                    if (userSession.Account != null)
                    {
                        // You can add more user info here like:
                        // - Last login time
                        // - Account status
                        // - Permission count
                        // UserStatusTextBlock.Text = $"Permissions: {userSession.GetPermissionCount()}";
                    }
                }
                else
                {
                    // User not logged in - show default values
                    if (UserNameTextBlock != null)
                        UserNameTextBlock.Text = _resourceLoader.GetString("NotLoggedIn/Text") ?? "Not logged in";
                    
                    if (UserEmailTextBlock != null)
                        UserEmailTextBlock.Text = "";
                    
                    if (UserRoleTextBlock != null)
                        UserRoleTextBlock.Text = "";
                    
                    if (UserPermissionsTextBlock != null)
                        UserPermissionsTextBlock.Text = "";
                }
            }
            catch (Exception)
            {
                // Error loading user info - set fallback values
                if (UserNameTextBlock != null)
                    UserNameTextBlock.Text = _resourceLoader.GetString("ErrorLoadingUser/Text") ?? "Error loading user info";
                
                if (UserEmailTextBlock != null)
                    UserEmailTextBlock.Text = "";
                
                if (UserRoleTextBlock != null)
                    UserRoleTextBlock.Text = "";
                
                if (UserPermissionsTextBlock != null)
                    UserPermissionsTextBlock.Text = "";
            }
        }

        /// <summary>
        /// Refresh user information from current session
        /// </summary>
        public void RefreshUserInfo()
        {
            LoadUserInfo();
        }

        private async void LoadSettings()
        {
            try
            {
                _isLoadingSettings = true;
                
                // Load language setting
                var savedLanguage = await _localStorageService.GetItemAsync<string>("app_language");
                if (!string.IsNullOrEmpty(savedLanguage))
                {
                    // Set combobox selection based on saved language
                    foreach (ComboBoxItem item in LanguageComboBox.Items)
                    {
                        if (item.Tag?.ToString() == savedLanguage)
                        {
                            LanguageComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                else
                {
                    // Default to English if no language is saved
                    LanguageComboBox.SelectedIndex = 0;
                }

                // Load other settings here in the future
                // Example: Theme, Notifications, etc.
            }
            catch (Exception)
            {
                // Log error - for now just set defaults
                LanguageComboBox.SelectedIndex = 0;
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Skip if we're loading settings to avoid showing dialog during initialization
            if (_isLoadingSettings)
                return;

            try
            {
                if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var languageCode = selectedItem.Tag?.ToString();
                    if (!string.IsNullOrEmpty(languageCode))
                    {
                        // Check if this is actually a change from current language
                        var currentLanguage = ApplicationLanguages.PrimaryLanguageOverride;
                        if (currentLanguage == languageCode)
                        {
                            // No change, don't show dialog
                            return;
                        }

                        // Save language preference
                        await _localStorageService.SetItemAsync("app_language", languageCode);

                        // Apply language immediately
                        ApplicationLanguages.PrimaryLanguageOverride = languageCode;

                        // Show confirmation dialog
                        var dialog = new ContentDialog()
                        {
                            Title = _resourceLoader.GetString("LanguageChangedTitle/Text"),
                            Content = _resourceLoader.GetString("LanguageChangedMessage/Text"),
                            CloseButtonText = _resourceLoader.GetString("OK/Content"),
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                    }
                }
            }
            catch (Exception)
            {
                // Show error dialog
                var errorDialog = new ContentDialog()
                {
                    Title = _resourceLoader.GetString("ErrorTitle/Text"),
                    Content = _resourceLoader.GetString("LanguageErrorMessage/Text"),
                    CloseButtonText = _resourceLoader.GetString("OK/Content"),
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and configure the edit profile dialog
                var editProfileDialog = new Controls.EditProfileDialog();
                
                // Create ContentDialog to host the UserControl
                var dialog = new ContentDialog()
                {
                    Content = editProfileDialog,
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = ElementTheme.Default
                };

                // Handle dialog events
                bool dialogResult = false;
                editProfileDialog.ProfileUpdated += (s, success) =>
                {
                    dialogResult = success;
                };

                editProfileDialog.DialogClosed += (s, args) =>
                {
                    dialog.Hide();
                };

                // Focus on email field when dialog opens
                editProfileDialog.FocusEmailField();

                // Show dialog
                await dialog.ShowAsync();

                // Handle result
                if (dialogResult)
                {
                    // Profile updated successfully
                    // Refresh user info to show updated data
                    RefreshUserInfo();
                }
            }
            catch (Exception ex)
            {
                // Show error dialog
                var errorDialog = new ContentDialog()
                {
                    Title = _resourceLoader.GetString("ErrorTitle/Text") ?? "Error",
                    Content = $"{_resourceLoader.GetString("ProfileUpdateError/Text") ?? "An error occurred while trying to update your profile"}: {ex.Message}",
                    CloseButtonText = _resourceLoader.GetString("OK/Content") ?? "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and configure the change password dialog
                var changePasswordDialog = new Controls.ChangePasswordDialog();
                
                // Create ContentDialog to host the UserControl
                var dialog = new ContentDialog()
                {
                    Content = changePasswordDialog,
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = ElementTheme.Default
                };

                // Handle dialog events
                bool dialogResult = false;
                changePasswordDialog.PasswordChanged += (s, success) =>
                {
                    dialogResult = success;
                };

                changePasswordDialog.DialogClosed += (s, args) =>
                {
                    dialog.Hide();
                };

                // Focus on current password field when dialog opens
                changePasswordDialog.FocusCurrentPassword();

                // Show dialog
                await dialog.ShowAsync();

                // Handle result
                if (dialogResult)
                {
                    // Password changed successfully
                    // Refresh user info in case anything changed
                    RefreshUserInfo();
                    
                    // Show success message
                    var successDialog = new ContentDialog()
                    {
                        Title = _resourceLoader.GetString("PasswordChangeSuccessTitle/Text") ?? "Success",
                        Content = _resourceLoader.GetString("PasswordChangeSuccessMessage/Text") ?? "Your password has been changed successfully!",
                        CloseButtonText = _resourceLoader.GetString("OK/Content") ?? "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                // Show error dialog
                var errorDialog = new ContentDialog()
                {
                    Title = _resourceLoader.GetString("ErrorTitle/Text") ?? "Error",
                    Content = _resourceLoader.GetString("ChangePasswordErrorMessage/Text") ?? $"An error occurred while trying to change your password: {ex.Message}",
                    CloseButtonText = _resourceLoader.GetString("OK/Content") ?? "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutContent = new StackPanel()
            {
                Spacing = 12
            };

            aboutContent.Children.Add(new TextBlock()
            {
                Text = _resourceLoader.GetString("AboutAppName/Text"),
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });

            aboutContent.Children.Add(new TextBlock()
            {
                Text = _resourceLoader.GetString("AboutVersion/Text"),
                FontSize = 14
            });

            aboutContent.Children.Add(new TextBlock()
            {
                Text = _resourceLoader.GetString("AboutDescription/Text"),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            });

            aboutContent.Children.Add(new TextBlock()
            {
                Text = _resourceLoader.GetString("AboutCopyright/Text"),
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0)
            });

            var dialog = new ContentDialog()
            {
                Title = _resourceLoader.GetString("AboutTitle/Text"),
                Content = aboutContent,
                CloseButtonText = _resourceLoader.GetString("OK/Content"),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ContentDialog()
                {
                    Title = _resourceLoader.GetString("SignOutConfirmTitle/Text"),
                    Content = _resourceLoader.GetString("SignOutConfirmMessage/Text"),
                    PrimaryButtonText = _resourceLoader.GetString("SignOutConfirmButton/Content"),
                    CloseButtonText = _resourceLoader.GetString("CancelButton/Content"),
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Use SessionService to logout
                    var sessionService = ServiceContainer.GetService<ISessionService>();
                    await sessionService.LogoutAsync();

                    // Navigate to login window
                    var loginWindow = new LoginWindow();
                    loginWindow.Activate();

                    (App.Current as App)?.CurrentWindow?.Close(); // Đóng cửa sổ hiện tại (MainWindow)


                }
            }
            catch (Exception)
            {
                var errorDialog = new ContentDialog()
                {
                    Title = _resourceLoader.GetString("ErrorTitle/Text"),
                    Content = _resourceLoader.GetString("SignOutErrorMessage/Text"),
                    CloseButtonText = _resourceLoader.GetString("OK/Content"),
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}