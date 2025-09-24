using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.View;
using System;

namespace PhoneStoreAdmin
{
    /// <summary>
    /// Main application window with navigation support
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private AppWindow _appWindow;

        public MainWindow()
        {
            InitializeComponent();

            // Set window icon using logo
            AppWindow.SetIcon("Assets/logo.png");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Maximize khi khởi động
            Maximize();

            // Navigate to Dashboard by default
            ContentFrame.Navigate(typeof(DashboardPage));
            MainNavView.SelectedItem = DashboardNavItem;
        }
        private void Maximize()
        {
            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }


        private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag != null)
            {
                string tag = item.Tag.ToString() ?? "Dashboard";
                NavigateToPage(tag);
            }
        }

        private void NavigateToPage(string pageTag)
        {
            Type pageType = pageTag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Products" => typeof(ProductsPage),
                "Orders" => typeof(OrdersPage),
                "Suppliers" => typeof(SuppliersPage),
                "PurchaseOrders" => typeof(PurchaseOrdersPage),
                "Batches" => typeof(BatchesPage),
                "Settings" => typeof(SettingsPage),
                _ => typeof(DashboardPage)
            };

            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
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
