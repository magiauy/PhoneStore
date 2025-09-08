using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PhoneStoreAdminApp.Data;
using System.IO;

namespace PhoneStoreAdminApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;
        public ServiceProvider ServiceProvider => serviceProvider;

        public App()
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Cấu hình IConfiguration để đọc từ appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Đăng ký DataSource với scope Singleton
            services.AddSingleton<DataSource>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Test kết nối database khi khởi động
            TestDatabaseConnection();

        }

        private void TestDatabaseConnection()
        {
            try
            {
                var dataSource = serviceProvider.GetRequiredService<DataSource>();
                using var connection = dataSource.GetConnection();
                Console.WriteLine("DB Connection opened successfully");
                MessageBox.Show("DB Connection opened successfully", "Database Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
                MessageBox.Show($"Database connection failed: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
