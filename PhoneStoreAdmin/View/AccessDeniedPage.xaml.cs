using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhoneStore.Services;

namespace PhoneStoreAdmin.View
{
    /// <summary>
    /// Access Denied page displayed when user doesn't have permission to access a page
    /// </summary>
    public sealed partial class AccessDeniedPage : Page
    {
        public string RequiredPermission { get; private set; } = string.Empty;
        public Visibility RequiredPermissionVisibility => 
            string.IsNullOrEmpty(RequiredPermission) ? Visibility.Collapsed : Visibility.Visible;

        public AccessDeniedPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Parameter can be the required permission code
            if (e.Parameter is string permissionCode && !string.IsNullOrEmpty(permissionCode))
            {
                RequiredPermission = permissionCode;
            }
        }

        private void BackToDashboardButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = ServiceContainer.GetService<MainWindow>();
            mainWindow?.NavigateToPage("Dashboard");
        }
    }
}
