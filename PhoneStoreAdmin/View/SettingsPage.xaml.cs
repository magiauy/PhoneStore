using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Models;
using Windows.Globalization;
using PhoneStoreAdmin.Helpers;
using Windows.ApplicationModel.Core;
using PhoneStoreAdmin.Utils;


namespace PhoneStoreAdmin.View
{
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private readonly ILocalStorageService _localStorageService;
        private bool _isLoadingSettings = false;
        private bool _isDarkThemeEnabled;
        private bool _areNotificationsEnabled;
        private bool _isRestartPromptVisible;

        public SettingsPage()
        {
            this.InitializeComponent();
            _localStorageService = ServiceContainer.GetService<ILocalStorageService>();
            LoadUserInfo();
            LoadSettings();
        }

        public bool IsDarkThemeEnabled
        {
            get => _isDarkThemeEnabled;
            set
            {
                if (SetProperty(ref _isDarkThemeEnabled, value) && !_isLoadingSettings)
                {
                    ApplyTheme(value);
                    _ = SaveThemePreferenceAsync(value);
                }
            }
        }

        public bool AreNotificationsEnabled
        {
            get => _areNotificationsEnabled;
            set
            {
                if (SetProperty(ref _areNotificationsEnabled, value) && !_isLoadingSettings)
                {
                    ApplyNotificationPreference(value);
                    _ = SaveNotificationPreferenceAsync(value);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
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
                            UserEmailTextBlock.Text = LocalizationHelper.GetString("NoEmailAvailable/Text") ?? "No email available";
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
                        UserNameTextBlock.Text = LocalizationHelper.GetString("NotLoggedIn/Text") ?? "Not logged in";
                    
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
                    UserNameTextBlock.Text = LocalizationHelper.GetString("ErrorLoadingUser/Text") ?? "Error loading user info";
                
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

                var savedTheme = await _localStorageService.GetItemAsync<string>("app_theme");
                if (!string.IsNullOrEmpty(savedTheme))
                {
                    IsDarkThemeEnabled = string.Equals(savedTheme, "Dark", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    IsDarkThemeEnabled = false;
                }

                var savedNotificationPreference = await _localStorageService.GetItemAsync<bool?>("notifications_enabled");
                AreNotificationsEnabled = savedNotificationPreference ?? true;
            }
            catch (Exception)
            {
                // Log error - for now just set defaults
                LanguageComboBox.SelectedIndex = 0;
                IsDarkThemeEnabled = false;
                AreNotificationsEnabled = true;
            }
            finally
            {
                _isLoadingSettings = false;
            }

            ApplyTheme(IsDarkThemeEnabled);
            ApplyNotificationPreference(AreNotificationsEnabled);
            LockThemeToggle(); // Lock theme toggle - changes require app restart
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

                        // Prompt for restart (language changes require restart)
                        await PromptForRestartAsync(
                            LocalizationHelper.GetString("LanguageChangedTitle/Text") ?? "Restart required",
                            LocalizationHelper.GetString("LanguageChangedMessage/Text") ?? 
                            "Language changes require restarting the application. Would you like to restart now?");
                    }
                }
            }
            catch (Exception)
            {
                // Show error dialog
                var errorDialog = new ContentDialog()
                {
                    Title = LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                    Content = LocalizationHelper.GetString("LanguageErrorMessage/Text") ?? "An error occurred while changing language",
                    CloseButtonText = LocalizationHelper.GetString("OK/Content") ?? "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private void ApplyTheme(bool isDarkTheme)
        {
            try
            {
                if (Application.Current is App app)
                {
                    // WinUI3: Theme must be set during app initialization
                    // We can only try to apply it, but it may not work at runtime
                    // The theme is typically controlled by system settings or App.xaml
                    app.RequestedTheme = isDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Cannot apply theme at runtime: {ex.Message}");
                // Theme change requires app restart - this is expected behavior
            }
        }

        /// <summary>
        /// Disable theme toggle (locked - theme changes require app restart)
        /// </summary>
        private void LockThemeToggle()
        {
            if (Application.Current.Resources.TryGetValue("ThemeToggleSwitch", out var themeToggleObj))
            {
                if (themeToggleObj is ToggleSwitch themeToggle)
                {
                    themeToggle.IsEnabled = false;
                }
            }
        }

        private void ApplyNotificationPreference(bool notificationsEnabled)
        {
            if (Application.Current is App app)
            {
                app.Resources["NotificationsEnabled"] = notificationsEnabled;
            }
        }

        private async Task SaveThemePreferenceAsync(bool isDarkTheme)
        {
            try
            {
                var themeValue = isDarkTheme ? "Dark" : "Light";
                await _localStorageService.SetItemAsync("app_theme", themeValue);

                await PromptForRestartAsync(
                    LocalizationHelper.GetString("ThemeRestartTitle/Text") ?? "Restart recommended",
                    LocalizationHelper.GetString("ThemeRestartMessage/Text") ??
                    "Theme changes are applied immediately, but restarting ensures all windows use the new appearance.");
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync(
                    LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                    string.Format(
                        LocalizationHelper.GetString("ThemeSaveError/Text") ?? "Unable to save theme preference: {0}",
                        ex.Message));
            }
        }

        private async Task SaveNotificationPreferenceAsync(bool notificationsEnabled)
        {
            try
            {
                await _localStorageService.SetItemAsync("notifications_enabled", notificationsEnabled);

                await PromptForRestartAsync(
                    LocalizationHelper.GetString("NotificationRestartTitle/Text") ?? "Restart may be required",
                    LocalizationHelper.GetString("NotificationRestartMessage/Text") ??
                    "Notification preferences have been updated. Restart to refresh background listeners if necessary.");
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync(
                    LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                    string.Format(
                        LocalizationHelper.GetString("NotificationSaveError/Text") ?? "Unable to save notification preference: {0}",
                        ex.Message));
            }
        }

        private async Task PromptForRestartAsync(string title, string message)
        {
            if (_isRestartPromptVisible)
            {
                return;
            }

            _isRestartPromptVisible = true;
            var dialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                PrimaryButtonText = LocalizationHelper.GetString("RestartNow/Text") ?? "Restart now",
                CloseButtonText = LocalizationHelper.GetString("RestartLater/Text") ?? "Later",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            try
            {
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var restartResult = AppInstance.Restart(string.Empty);
                    if (restartResult != AppRestartFailureReason.RestartPending)
                    {
                        await ShowErrorDialogAsync(
                            LocalizationHelper.GetString("RestartFailedTitle/Text") ?? "Unable to restart",
                            string.Format(
                                LocalizationHelper.GetString("RestartFailedMessage/Text") ??
                                "The application could not restart automatically ({0}). Please restart manually.",
                                restartResult));
                    }
                }
            }
            finally
            {
                _isRestartPromptVisible = false;
            }
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            var errorDialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = LocalizationHelper.GetString("OK/Content") ?? "OK",
                XamlRoot = this.XamlRoot
            };

            await errorDialog.ShowAsync();
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
                    Title = LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                    Content = $"{LocalizationHelper.GetString("ProfileUpdateError/Text") ?? "An error occurred while trying to update your profile"}: {ex.Message}",
                    CloseButtonText = LocalizationHelper.GetString("OK/Content") ?? "OK",
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
                        Title = LocalizationHelper.GetString("PasswordChangeSuccessTitle/Text") ?? "Success",
                        Content = LocalizationHelper.GetString("PasswordChangeSuccessMessage/Text") ?? "Your password has been changed successfully!",
                        CloseButtonText = LocalizationHelper.GetString("OK/Content") ?? "OK",
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
                    Title = LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                    Content = LocalizationHelper.GetString("ChangePasswordErrorMessage/Text") ?? $"An error occurred while trying to change your password: {ex.Message}",
                    CloseButtonText = LocalizationHelper.GetString("OK/Content") ?? "OK",
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
                Text = LocalizationHelper.GetString("AboutAppName/Text"),
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });

            aboutContent.Children.Add(new TextBlock()
            {
                Text = LocalizationHelper.GetString("AboutVersion/Text"),
                FontSize = 14
            });

            aboutContent.Children.Add(new TextBlock()
            {
                Text = LocalizationHelper.GetString("AboutDescription/Text"),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            });

            aboutContent.Children.Add(new TextBlock()
            {
                Text = LocalizationHelper.GetString("AboutCopyright/Text"),
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0)
            });

            var dialog = new ContentDialog()
            {
                Title = LocalizationHelper.GetString("AboutTitle/Text"),
                Content = aboutContent,
                CloseButtonText = LocalizationHelper.GetString("OK/Content"),
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
                    Title = LocalizationHelper.GetString("SignOutConfirmTitle/Text"),
                    Content = LocalizationHelper.GetString("SignOutConfirmMessage/Text"),
                    PrimaryButtonText = LocalizationHelper.GetString("SignOutConfirmButton/Content"),
                    CloseButtonText = LocalizationHelper.GetString("CancelButton/Content"),
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
                    Title = LocalizationHelper.GetString("ErrorTitle/Text"),
                    Content = LocalizationHelper.GetString("SignOutErrorMessage/Text"),
                    CloseButtonText = LocalizationHelper.GetString("OK/Content"),
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}