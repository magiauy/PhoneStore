using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Models;
using PhoneStoreAdmin.View;
using PhoneStore.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin
{
    /// <summary>
    /// Main application window with navigation support
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private AppWindow _appWindow;

        /// <summary>
        /// Mapping of page tags to required permissions.
        /// Pages not in this dictionary don't require permission (e.g., Dashboard).
        /// </summary>
        private static readonly Dictionary<string, string> PagePermissions = new()
        {
            // Product related
            { "Products", "PRODUCT_VIEW" },
            { "ProductAttributes", "PRODUCT_ATTRIBUTE_VIEW" },
            { "ProductManagement", "PRODUCT_VIEW" },
            { "Brands", "BRAND_VIEW" },
            
            // Orders & Sales
            { "Orders", "INVOICE_VIEW" },
            { "Invoice", "INVOICE_VIEW" },
            
            // Inventory & Supply Chain
            { "Suppliers", "SUPPLIERS_VIEW" },
            { "PurchaseOrders", "PURCHASE_ORDERS_VIEW" },
            { "Batches", "BATCH_VIEW" },
            
            // Marketing
            { "Promotions", "PROMOTIONS_VIEW" },
            
            // People Management
            { "Customers", "CUSTOMER_VIEW" },
            { "Employees", "EMPLOYEE_VIEW" },
            
            // Administration
            { "Accounts", "ACCOUNT_VIEW" },
            { "Roles", "ROLE_VIEW" },
            
            // Reports
            { "Reports", "REPORT_VIEW" },
            
            // Settings - optional, can be accessed by users with specific permissions
            { "Settings", "SETTING_VIEW" }
        };

        public MainWindow()
        {
            InitializeComponent();
            (Application.Current as App)!.CurrentWindow = this;

            // Register MainWindow as a service so other components can access it
            ServiceContainer.RegisterSingleton<MainWindow>(this);

            // Set window icon using logo
            AppWindow.SetIcon("Assets/logo.png");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Maximize khi khởi động
            Maximize();

            // Configure navigation based on user permissions
            ConfigureNavigationPermissions();

            // Navigate to Dashboard by default
            ContentFrame.Navigate(typeof(DashboardPage));
            MainNavView.SelectedItem = DashboardNavItem;
        }

        /// <summary>
        /// Configure navigation menu items visibility based on user permissions.
        /// Items without permission will be hidden.
        /// </summary>
        private void ConfigureNavigationPermissions()
        {
            var session = UserSession.Instance;
            
            // Map NavigationViewItem names to their tags for permission checking
            var navItems = new Dictionary<string, NavigationViewItem>
            {
                { "Products", ProductsNavItem },
                { "ProductAttributes", ProductAttributesNavItem },
                { "Brands", BrandsNavItem },
                { "Orders", OrdersNavItem },
                { "Invoice", InvoiceNavItem },
                { "Suppliers", SuppliersNavItem },
                { "PurchaseOrders", PurchaseOrdersNavItem },
                { "Batches", BatchesNavItem },
                { "Promotions", PromotionsNavItem },
                { "Customers", CustomersNavItem },
                { "Employees", EmployeesNavItem },
                { "Accounts", AccountsNavItem },
                { "Roles", RolesNavItem },
                { "Reports", ReportNavItem }
            };

            foreach (var kvp in navItems)
            {
                var pageTag = kvp.Key;
                var navItem = kvp.Value;
                
                if (navItem == null) continue;
                
                // Check if page requires permission
                if (PagePermissions.TryGetValue(pageTag, out var requiredPermission))
                {
                    // Hide menu item if user doesn't have permission
                    navItem.Visibility = session.HasPermission(requiredPermission) 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Check if user has permission to access a specific page
        /// </summary>
        private bool HasPagePermission(string pageTag)
        {
            // Dashboard doesn't require permission
            if (string.Equals(pageTag, "Dashboard", StringComparison.OrdinalIgnoreCase))
                return true;
            
            // Settings page - allow if user has any meaningful permission
            if (string.Equals(pageTag, "Settings", StringComparison.OrdinalIgnoreCase))
                return true; // Settings accessible to all logged-in users
            
            // Check specific page permission
            if (PagePermissions.TryGetValue(pageTag, out var requiredPermission))
            {
                return UserSession.Instance.HasPermission(requiredPermission);
            }
            
            // Pages not in the dictionary are accessible
            return true;
        }

        /// <summary>
        /// Get the required permission for a page
        /// </summary>
        private string? GetRequiredPermission(string pageTag)
        {
            return PagePermissions.TryGetValue(pageTag, out var permission) ? permission : null;
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
                NavigateToPage(tag, null);
            }
        }

        public void NavigateToPage(string pageTag, object? parameter = null)
        {
            // Check permission before navigation
            if (!HasPagePermission(pageTag))
            {
                // Navigate to Access Denied page with required permission info
                var requiredPermission = GetRequiredPermission(pageTag);
                ContentFrame.Navigate(typeof(AccessDeniedPage), requiredPermission);
                return;
            }

            Type pageType = pageTag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Products" => typeof(ProductsPage),
                "ProductAttributes" => typeof(ProductAttributesPage),
                "ProductManagement" => typeof(ProductManagementPage),
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
                "Invoice" => typeof(InvoicePage),
                "AccessDenied" => typeof(AccessDeniedPage),
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
                ContentFrame.Navigate(pageType, parameter);
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
            var resourceLoader = new ResourceLoader();
            var dlg = new ContentDialog()
            {
                Title = resourceLoader.GetString("MainWindow_NewItemTitle"),
                Content = resourceLoader.GetString("MainWindow_NewItemContent"),
                CloseButtonText = resourceLoader.GetString("DialogClose"),
                XamlRoot = this.Content.XamlRoot
            };

            await dlg.ShowAsync();
        }
    }
}
