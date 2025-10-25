using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhoneStoreAdmin
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Gets the current application window
        /// </summary>
        public Window? CurrentWindow
        {
            get => _window;
            set => _window = value;
        }


        /// <summary>
        /// Sets the current application window (used when transitioning from LoginWindow to MainWindow)
        /// </summary>
        public void SetCurrentWindow(Window window)
        {
            _window = window;
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>        

        public App()
        {
            this.InitializeComponent();


            // Initialize DI container
            ServiceContainer.Initialize();
            this.RequestedTheme = ApplicationTheme.Light;
            this.UnhandledException += (s, e) =>
            {
                var msg = $"Unhandled:\n{e.Exception}\n{e.Message}\n{e.Exception?.StackTrace}";
                System.Diagnostics.Debug.WriteLine(msg);
                e.Handled = false;
            };
            LoadLanguageSettings();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Start with the login window instead of main window
            _window = new LoginWindow();
            _window.Activate();
        }

        /// <summary>
        /// Load language settings from local storage
        /// </summary>
        private async void LoadLanguageSettings()
        {
            try
            {
                var localStorageService = ServiceContainer.GetService<ILocalStorageService>();
                var savedLanguage = await localStorageService.GetItemAsync<string>("app_language");
                
                if (!string.IsNullOrEmpty(savedLanguage))
                {
                    ApplicationLanguages.PrimaryLanguageOverride = savedLanguage;
                }
                else
                {
                    // Default to English
                    ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                }
            }
            catch (Exception)
            {
                // If there's an error, default to English
                ApplicationLanguages.PrimaryLanguageOverride = "en-US";
            }
        }

        public static T GetService<T>()
        {
            return ServiceContainer.GetService<T>();
        }
    }
}
