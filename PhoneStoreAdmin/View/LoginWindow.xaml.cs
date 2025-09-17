using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;

namespace PhoneStoreAdmin
{
    public sealed partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            this.InitializeComponent();
            // Set window properties for a modern look
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null); // Hide default title bar for cleaner look

            // Set window icon using logo
            this.AppWindow.SetIcon("../Assets/logo.png");
        
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
            // Validate input

            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                ShowErrorOnField(UsernameTextBox, "Please enter your username.");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordTextBox.Password))
            {
                ShowErrorOnField(PasswordTextBox, "Please enter your password.");
                return;
            }

            // Show loading state
            SetLoadingState(true);
            
            // Log authentication attempt

            try
            {
                // Simulate authentication delay for better UX
                await System.Threading.Tasks.Task.Delay(1500);

                // TODO: Replace with actual authentication logic
                if (await AuthenticateUserAsync(UsernameTextBox.Text, PasswordTextBox.Password))
                {
                    Logger.LogAuth(UsernameTextBox.Text, true);
                    Logger.Info("Authentication successful, redirecting to main window");
                    
                    // Authentication successful - show success dialog
                    await ShowSuccessDialog();
                    
                    var mainWindow = new MainWindow();
                    mainWindow.Activate();
                    this.Close();
                }
                else
                {
                    Logger.LogAuth(UsernameTextBox.Text, false);
                    Logger.Warning("Authentication failed - invalid credentials");
                    
                    ShowErrorOnField(UsernameTextBox, "Invalid username or password. Please try again.");
                    PasswordTextBox.Password = string.Empty;
                    
                    // Shake animation for failed login
                    await AnimateLoginFailure();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Login failed for user {UsernameTextBox.Text}", ex);
                ShowErrorOnField(UsernameTextBox, $"Login failed: {ex.Message}");
                await AnimateLoginFailure();
            }
            finally
            {
                SetLoadingState(false);
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
                Text = "Welcome to GuZone!",
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
                Text = "You have successfully signed in to your account.",
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
                PrimaryButtonText = "Continue",
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
                var authService = ServiceContainer.GetService<IAuthService>();
                var account = await authService.LoginAsync(username, password);
                return account != null;
            }
            catch (Exception)
            {
                // Log error if needed
                return false;
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            // TODO: Fix after XAML controls are generated
            LoginButton.IsEnabled = !isLoading;
            UsernameTextBox.IsEnabled = !isLoading;
            PasswordTextBox.IsEnabled = !isLoading;
            
            // Update loading animation and button text
            if (isLoading)
            {
                LoginProgressRing.IsActive = true;
                LoginProgressRing.Visibility = Visibility.Visible;
                LoginButtonText.Text = "Signing In...";
            }
            else
            {
                LoginProgressRing.IsActive = false;
                LoginProgressRing.Visibility = Visibility.Collapsed;
                LoginButtonText.Text = "Sign In";
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
            // Focus the field
            field.Focus(FocusState.Keyboard);
            
            // Add red border to indicate error
            field.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.IndianRed);
            field.BorderThickness = new Thickness(2);
            
            // Show error dialog
            var dialog = new ContentDialog()
            {
                Title = "Input Error",
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

        private async void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = "Reset Password",
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock 
                        { 
                            Text = "Enter your email address and we'll send you a link to reset your password.",
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBox 
                        { 
                            PlaceholderText = "Enter your email address",
                            Header = "Email Address"
                        }
                    }
                },
                PrimaryButtonText = "Send Reset Link",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // TODO: Implement password reset logic
                var infoDialog = new ContentDialog()
                {
                    Title = "Feature Coming Soon",
                    Content = "Password reset functionality will be implemented soon.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await infoDialog.ShowAsync();
            }
        }

        private async void SignUp_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            var dialog = new ContentDialog()
            {
                Title = "Create New Account",
                Content = new StackPanel
                {
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock 
                        { 
                            Text = "Account registration is currently handled by system administrators.",
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock 
                        { 
                            Text = "Please contact your IT administrator to request a new account.",
                            TextWrapping = TextWrapping.Wrap,
                            FontWeight = Microsoft.UI.Text.FontWeights.Medium
                        },
                        new TextBlock 
                        { 
                            Text = "📧 Email: admin@phonestore.com\n📞 Phone: +1 (555) 123-4567",
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
    }
}
