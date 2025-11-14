using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStore.Services.Helpers;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;

namespace PhoneStoreAdmin
{
    public sealed partial class LoginWindow : Window
    {
        private AppWindow _appWindow;
        private bool _isDialogOpen = false;
        private bool _isProcessingLogin = false;

        public LoginWindow()
        {
            this.InitializeComponent();

            // Set window properties for a modern look
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null); // Hide default title bar for cleaner look
            // Lấy AppWindow từ WinUI Window
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Maximize khi khởi động
            Maximize();
        }
        private void Maximize()
        {
            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (RootGrid.Resources.TryGetValue("PageLoadStoryboard", out var resource) && resource is Storyboard storyboard)
            {
                storyboard.Begin();
            }
        }
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformLoginAsync();
        }
        private async System.Threading.Tasks.Task PerformLoginAsync()
        {
            // Prevent multiple simultaneous login attempts
            if (_isProcessingLogin)
            {
                Logger.Info("Login already in progress, ignoring duplicate request");
                return;
            }

            _isProcessingLogin = true;

            try
            {
                await PerformLoginInternalAsync();
            }
            finally
            {
                _isProcessingLogin = false;
            }
        }

        private async System.Threading.Tasks.Task PerformLoginInternalAsync()
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                try
                {
                    ShowErrorOnField(UsernameTextBox, LocalizationHelper.GetString("ErrorUsernameRequired/Text"));
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error showing error dialog: {ex.Message}");
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordTextBox.Password))
            {
                ShowErrorOnField(PasswordTextBox, LocalizationHelper.GetString("ErrorPasswordRequired/Text"));
                return;
            }

            // Show loading state
            SetLoadingState(true);

            try
            {
                if (await AuthenticateUserAsync(UsernameTextBox.Text, PasswordTextBox.Password))
                {
                    Logger.LogAuth(UsernameTextBox.Text, true);
                    Logger.Info("Authentication successful, redirecting to main window");

                    await ShowSuccessDialog();

                    var mainWindow = new MainWindow();
        
   // Register MainWindow in ServiceContainer for dependency injection
 ServiceContainer.RegisterSingleton<MainWindow>(mainWindow);
     
               // Update App.CurrentWindow to point to MainWindow
       (Application.Current as App)?.SetCurrentWindow(mainWindow);
  
 mainWindow.Activate();
         this.Close();
    }
                else
                {
                    Logger.LogAuth(UsernameTextBox.Text, false);
                    Logger.Warning("Authentication failed - invalid credentials");

                    ShowErrorOnField(UsernameTextBox, LocalizationHelper.GetString("ErrorInvalidCredentials/Text"));
                    PasswordTextBox.Password = string.Empty;

                    await AnimateLoginFailure();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Login failed for user {UsernameTextBox.Text}", ex);
                ShowErrorOnField(UsernameTextBox, string.Format(LocalizationHelper.GetString("ErrorLoginFailed/Text"), ex.Message));
                await AnimateLoginFailure();
            }
            finally
            {
                SetLoadingState(false);
                // nếu cần, trả focus về password để người dùng thử lại:
                PasswordTextBox.Focus(FocusState.Keyboard);
            }
        }
        private void LoginButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Scale down animation on press
            var button = sender as Button;
            var scaleTransform = button?.RenderTransform as ScaleTransform;
            if (scaleTransform != null)
            {
                var scaleAnimation = new DoubleAnimation
                {
                    To = 0.95,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                Storyboard.SetTarget(scaleAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleAnimation, "ScaleX");

                var scaleAnimationY = new DoubleAnimation
                {
                    To = 0.95,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                Storyboard.SetTarget(scaleAnimationY, scaleTransform);
                Storyboard.SetTargetProperty(scaleAnimationY, "ScaleY");

                var storyboard = new Storyboard();
                storyboard.Children.Add(scaleAnimation);
                storyboard.Children.Add(scaleAnimationY);
                storyboard.Begin();
            }
        }

        private void LoginButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // Scale back up animation on release
            var button = sender as Button;
            var scaleTransform = button?.RenderTransform as ScaleTransform;
            if (scaleTransform != null)
            {
                var scaleAnimation = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };

                Storyboard.SetTarget(scaleAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleAnimation, "ScaleX");

                var scaleAnimationY = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };

                Storyboard.SetTarget(scaleAnimationY, scaleTransform);
                Storyboard.SetTargetProperty(scaleAnimationY, "ScaleY");

                var storyboard = new Storyboard();
                storyboard.Children.Add(scaleAnimation);
                storyboard.Children.Add(scaleAnimationY);
                storyboard.Begin();
            }
        }

        private async System.Threading.Tasks.Task ShowSuccessDialog()
        {
            // Prevent multiple dialogs from opening
            if (_isDialogOpen)
            {
                Logger.Warning("Dialog already open, skipping success dialog");
                return;
            }

            _isDialogOpen = true;

            try
            {
                await ShowSuccessDialogInternal();
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async System.Threading.Tasks.Task ShowSuccessDialogInternal()
        {
            var contentGrid = new Grid();
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // Add scale transform for animation
            var scaleTransform = new ScaleTransform { ScaleX = 0.8, ScaleY = 0.8 };
            contentGrid.RenderTransform = scaleTransform;
            contentGrid.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

            // Success icon with background
            var iconGrid = new Grid
            {
                Width = 80,
                Height = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            var ellipse = new Ellipse
            {
                Fill = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen),
                Width = 80,
                Height = 80,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var checkIcon = new FontIcon
            {
                Glyph = "\uE73E", // CheckMark symbol
                FontSize = 60, // lớn hơn
                FontWeight = Microsoft.UI.Text.FontWeights.Bold, // đậm hơn
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconGrid.Children.Add(ellipse);
            iconGrid.Children.Add(checkIcon);
            Grid.SetRow(iconGrid, 0);

            // Title text
            var titleText = new TextBlock
            {
                Text = LocalizationHelper.GetString("SuccessTitle/Text"),
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(titleText, 1);

            // Description text
            var descText = new TextBlock
            {
                Text = LocalizationHelper.GetString("SuccessDescription/Text"),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                MaxWidth = 350,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(descText, 2);

            contentGrid.Children.Add(iconGrid);
            contentGrid.Children.Add(titleText);
            contentGrid.Children.Add(descText);

            var dialog = new ContentDialog()
            {
                Content = contentGrid,
                PrimaryButtonText = LocalizationHelper.GetString("ContinueButton/Content"),
                XamlRoot = this.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            // Start scale animation when dialog is opened
            dialog.Opened += (s, e) =>
            {
                var storyboard = new Storyboard();

                var scaleXAnimation = new DoubleAnimation
                {
                    From = 0.8,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };
                Storyboard.SetTarget(scaleXAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleXAnimation, "ScaleX");

                var scaleYAnimation = new DoubleAnimation
                {
                    From = 0.8,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };
                Storyboard.SetTarget(scaleYAnimation, scaleTransform);
                Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");

                var opacityAnimation = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(opacityAnimation, contentGrid);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

                storyboard.Children.Add(scaleXAnimation);
                storyboard.Children.Add(scaleYAnimation);
                storyboard.Children.Add(opacityAnimation);
                storyboard.Begin();
            };

            await dialog.ShowAsync();
        }

        private async System.Threading.Tasks.Task AnimateLoginFailure()
        {
            // Shake animation for login failure
            var shakeStoryboard = new Storyboard();
            var translateTransform = FormPanel.RenderTransform as TranslateTransform;

            if (translateTransform != null)
            {
                var shakeAnimation = new DoubleAnimationUsingKeyFrames();
                Storyboard.SetTarget(shakeAnimation, translateTransform);
                Storyboard.SetTargetProperty(shakeAnimation, "X");

                // Create shake keyframes
                shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(0), Value = 0 });
                shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(100), Value = -10 });
                shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(200), Value = 10 });
                shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(300), Value = -8 });
                shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(400), Value = 8 });
                shakeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(500), Value = 0 });

                shakeStoryboard.Children.Add(shakeAnimation);
                shakeStoryboard.Begin();

                await System.Threading.Tasks.Task.Delay(500);
            }
        }

        private async System.Threading.Tasks.Task<bool> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                var sessionService = ServiceContainer.GetService<ISessionService>();
                return await sessionService.LoginAsync(username, password);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to authenticate user: {username}", ex);
                return false;
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            // TODO: Fix after XAML controls are generated
            LoginButton.IsEnabled = !isLoading;
            // UsernameTextBox.IsEnabled = !isLoading;
            // PasswordTextBox.IsEnabled = !isLoading;

            // Update loading animation and button text
            if (isLoading)
            {
                LoginProgressRing.IsActive = true;
                LoginProgressRing.Visibility = Visibility.Visible;
                LoginButtonText.Text = LocalizationHelper.GetString("SigningIn/Text");
            }
            else
            {
                LoginProgressRing.IsActive = false;
                LoginProgressRing.Visibility = Visibility.Collapsed;
                LoginButtonText.Text = LocalizationHelper.GetString("SignIn/Content");
            }

        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Control control)
                ClearFieldError(control);
        }

        private void PasswordTextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is Control control)
                ClearFieldError(control);
        }

        private void ClearFieldError(Control field)
        {
            if (field != null)
            {
                // Reset border styling
                field.ClearValue(Control.BorderBrushProperty);
                field.ClearValue(Control.BorderThicknessProperty);
            }
        }

        private async void ShowErrorOnField(Control field, string message)
        {
            // Prevent multiple dialogs from opening
            if (_isDialogOpen)
            {
                Logger.Warning("Dialog already open, skipping error dialog");
                return;
            }

            // Focus the field
            field.Focus(FocusState.Keyboard);

            // Add red border to indicate error
            field.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.IndianRed);
            field.BorderThickness = new Thickness(2);

            _isDialogOpen = true;

            try
            {
                // Show error dialog
                var dialog = new ContentDialog()
                {
                    Title = LocalizationHelper.GetString("ErrorInputTitle/Text"),
                    Content = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = message,
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 14
                            }
                        }
                    },
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            // Prevent multiple dialogs from opening
            if (_isDialogOpen)
            {
                Logger.Warning("Dialog already open, skipping forgot password dialog");
                return;
            }

            _isDialogOpen = true;

            try
            {
                var dialog = new ContentDialog()
                {
                    Title = LocalizationHelper.GetString("ResetPasswordTitle/Text"),
                    Content = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = LocalizationHelper.GetString("ResetPasswordInstruction/Text"),
                                TextWrapping = TextWrapping.Wrap
                            },
                            new TextBox
                            {
                                PlaceholderText = LocalizationHelper.GetString("EmailPlaceholder/PlaceholderText"),
                                Header = LocalizationHelper.GetString("EmailHeader/Text")
                            }
                        }
                    },
                    PrimaryButtonText = LocalizationHelper.GetString("ResetLinkButton/Content"),
                    CloseButtonText = LocalizationHelper.GetString("CancelButton/Content"),
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // TODO: Implement password reset logic
                    var infoDialog = new ContentDialog()
                    {
                        Title = "Feature Coming Soon",
                        Content = LocalizationHelper.GetString("FeatureComingSoon/Text"),
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await infoDialog.ShowAsync();
                }
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async void SignUp_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            var dialog = new ContentDialog()
            {
                Title = LocalizationHelper.GetString("SignUpTitle/Text"),
                Content = new StackPanel
                {
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = LocalizationHelper.GetString("SignUpMessage1/Text"),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = LocalizationHelper.GetString("SignUpMessage2/Text"),
                            TextWrapping = TextWrapping.Wrap,
                            FontWeight = Microsoft.UI.Text.FontWeights.Medium
                        },
                        new TextBlock
                        {
                            Text = LocalizationHelper.GetString("SignUpMessage3/Text"),
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                        }
                    }
                },
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
            // TODO: Implement self-service registration if needed
        }

        private void UsernameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                PasswordTextBox.Focus(FocusState.Keyboard);
                e.Handled = true;
            }
        }
                private async void PasswordTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true; // ngăn focus mặc định / behaviour khác
                await PerformLoginAsync();
            }
        }
    }
}
