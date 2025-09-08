using PhoneStoreAdminApp.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace PhoneStoreAdminApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TestDbButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataSource = ((App)Application.Current).ServiceProvider.GetRequiredService<DataSource>();
                using var connection = dataSource.GetConnection();
                MessageBox.Show("Database connection successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
