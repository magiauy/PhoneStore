using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhoneStoreAdmin
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Set window icon using logo
            AppWindow.SetIcon("Assets/logo.png");
        }

        private async void NewButton_Click(object sender, RoutedEventArgs e)
        {
            // Simple demo action: show a ContentDialog
            var dlg = new ContentDialog()
            {
                Title = "New item",
                Content = "This would open the new item flow.",
                CloseButtonText = "Close",
                XamlRoot = this.Content.XamlRoot
            };

            await dlg.ShowAsync();
        }
    }
}
