using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoneStoreAdmin.View
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            this.InitializeComponent();
        }

        private void NewProductButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to Product creation page or open dialog
            // Example: Frame.Navigate(typeof(ProductCreatePage));
        }
    }
}