using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreRepository.Models;
using PhoneStoreAdmin.View;
using System;
using System.Linq;

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
            (Application.Current as App)!.CurrentWindow = this;

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

        public void NavigateToPage(string pageTag)
        {
            Type pageType = pageTag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Products" => typeof(ProductsPage),
                "ProductManagement" => typeof(ProductManagementPage),
                "ProductAttributes" => typeof(ProductAttributesPage),
                "Suppliers" => typeof(SuppliersPage),
                "Brands" => typeof(BrandPage),
                "Promotions" => typeof(PromotionsPage),
                "PurchaseOrders" => typeof(PurchaseOrdersPage),
                "Batches" => typeof(BatchesPage),
                "Orders" => typeof(SalesPage),
                "Customers" => typeof(CustomersPage),
                "Employees" => typeof(EmployeesPage),
                "Accounts" => typeof(AccountsPage),
                "Roles" => typeof(RolesPage),
                "Reports" => typeof(ReportsPage),
                "Settings" => typeof(SettingsPage),
                _ => typeof(DashboardPage)
            };

            if (!string.Equals(pageTag, "Settings", StringComparison.OrdinalIgnoreCase))
            {
                var navItem = FindNavigationViewItem(pageTag);
                if (navItem != null && !Equals(MainNavView.SelectedItem, navItem))
                {
                    MainNavView.SelectedItem = navItem;
                }
            }

            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }

        private NavigationViewItem? FindNavigationViewItem(string pageTag)
        {
            return MainNavView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), pageTag, StringComparison.OrdinalIgnoreCase));
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
