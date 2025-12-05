using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.Implementations;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreRepository.Repositories.Implementations;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;

namespace PhoneStore.Services
{
    /// <summary>
    /// Simple dependency injection container for managing services
    /// </summary>
    public static class ServiceContainer
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, Func<object>> _factories = new();
        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize the service container with all required services
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            // Build configuration
            var configuration = BuildConfiguration();

            // Register configuration
            RegisterSingleton<IConfiguration>(configuration);

            // Register data source
            RegisterSingleton<DataSource>(() => new DataSource(GetService<IConfiguration>()));

            // Register repositories
            RegisterSingleton<IAuthRepository>(() => new AuthRepository(GetService<DataSource>()));
            RegisterSingleton<IAccountRepository>(() => new AccountRepository(GetService<DataSource>()));
            RegisterSingleton<IPersonRepository>(() => new PersonRepository(GetService<DataSource>()));
            RegisterSingleton<IEmployeeRepository>(() => new EmployeeRepository(GetService<DataSource>()));
            RegisterSingleton<ICustomerRepository>(() => new CustomerRepository(GetService<DataSource>()));
            RegisterSingleton<ISupplierRepository>(() => new SupplierRepository(GetService<DataSource>()));
            RegisterSingleton<IPurchaseOrderLineRepository>(() => new PurchaseOrderLineRepository(GetService<DataSource>()));
            RegisterSingleton<IPurchaseOrderRepository>(() => new PurchaseOrderRepository(GetService<DataSource>()));
            RegisterSingleton<IBatchProductRepository>(() => new BatchProductRepository(GetService<DataSource>()));
            RegisterSingleton<IBatchesRepository>(() => new BatchesRepository(GetService<DataSource>()));
            RegisterSingleton<IProductCategoryRepository>(() => new ProductCategoryRepository(GetService<DataSource>()));
            RegisterSingleton<IBrandRepository>(() => new BrandRepository(GetService<DataSource>()));
            RegisterSingleton<IProductAttributeRepository>(() => new ProductAttributeRepository(GetService<DataSource>()));
            RegisterSingleton<IProductAttributeOptionRepository>(() => new ProductAttributeOptionRepository(GetService<DataSource>()));
            RegisterSingleton<IProductAttributeValueRepository>(() => new ProductAttributeValueRepository(GetService<DataSource>()));
            RegisterSingleton<IProductModelRepository>(() => new ProductModelRepository(GetService<DataSource>()));
            RegisterSingleton<IProductModelAttributeRepository>(() => new ProductModelAttributeRepository(GetService<DataSource>()));
            RegisterSingleton<IPromotionRepository>(() => new PromotionRepository(GetService<DataSource>()));
            RegisterSingleton<IPromotionCodeRepository>(() => new PromotionCodeRepository(GetService<DataSource>()));
            RegisterSingleton<IProductRepository>(() => new ProductRepository(GetService<DataSource>()));
            RegisterSingleton<IProductSerialRepository>(() => new ProductSerialRepository(GetService<DataSource>()));
            RegisterSingleton<IRoleRepository>(() => new RoleRepository(GetService<DataSource>()));
            RegisterSingleton<IPermissionRepository>(() => new PermissionRepository(GetService<DataSource>()));
            RegisterSingleton<IInvoiceRepository>(() => new InvoiceRepository(ServiceContainer.GetService<DataSource>()));
            RegisterSingleton<IInvoiceLineRepository>(() => new InvoiceLineRepository(ServiceContainer.GetService<DataSource>()));
            RegisterSingleton<IInvoiceLineSerialRepository>(() => new InvoiceLineSerialRepository(ServiceContainer.GetService<DataSource>()));
            RegisterSingleton<ISettingStringRepository>(() => new SettingStringRepository(GetService<DataSource>()));
            RegisterSingleton<IPricingAlertRepository>(() => new PricingAlertRepository(GetService<DataSource>()));
            RegisterSingleton<IPricingHistoryRepository>(() => new PricingHistoryRepository(GetService<DataSource>()));

            // Register services
            RegisterSingleton<IAuthService>(() => new AuthService(GetService<IAuthRepository>()));
            RegisterSingleton<IAccountService>(() => new AccountService(GetService<IAccountRepository>()));
            RegisterSingleton<IPersonService>(() => new PersonService(GetService<IPersonRepository>()));
            RegisterSingleton<IEmployeeService>(() => new EmployeeService(GetService<IEmployeeRepository>()));
            RegisterSingleton<ICustomerService>(() => new CustomerService(GetService<ICustomerRepository>()));
            RegisterSingleton<ISupplierService>(() => new SupplierService(GetService<ISupplierRepository>()));
            RegisterSingleton<IBrandService>(() => new BrandService(GetService<IBrandRepository>()));
            RegisterSingleton<IProductAttributeOptionService>(() => new ProductAttributeOptionService(GetService<IProductAttributeOptionRepository>()));
            RegisterSingleton<IProductService>(() => new ProductService(
                GetService<IProductRepository>(),
                GetService<IBrandRepository>(),
                GetService<IProductCategoryRepository>(),
                GetService<IProductAttributeRepository>(),
                GetService<IProductAttributeValueRepository>(),
                GetService<IProductSerialRepository>(),
                GetService<IBatchProductRepository>(),
                GetService<IProductModelRepository>(),
                GetService<IProductModelAttributeRepository>(),
                GetService<IProductAttributeOptionService>()));
            RegisterSingleton<IPromotionService>(() => new PromotionService(GetService<IPromotionRepository>()));
            // NOTE: SettingStringService must be registered BEFORE DynamicPricingService (dependency)
            RegisterSingleton<ISettingStringService>(() => new SettingStringService(GetService<ISettingStringRepository>()));
            
            RegisterSingleton<IPromotionCodeService>(() => new PromotionCodeService(GetService<IPromotionCodeRepository>(), GetService<IPromotionRepository>()));
            // Register pricing services (must be before PurchaseOrderService and BatchesService which depend on IDynamicPricingService)
            RegisterSingleton<IDynamicPricingService>(() => new DynamicPricingService(
                GetService<IProductRepository>(),
                GetService<IPricingHistoryRepository>(),
                GetService<IPricingAlertRepository>(),
                GetService<ISettingStringService>(),
                GetService<IProductSerialRepository>()));
            RegisterSingleton<IPricingAlertService>(() => new PricingAlertService(
                GetService<IPricingAlertRepository>(),
                GetService<IProductRepository>(),
                GetService<IPricingHistoryRepository>(),
                GetService<IDynamicPricingService>()));

            RegisterSingleton<IPurchaseOrderService>(() => new PurchaseOrderService(
                GetService<IPurchaseOrderRepository>(), 
                GetService<IPurchaseOrderLineRepository>(),
                GetService<IBatchesRepository>(),
                GetService<IBatchProductRepository>(),
                GetService<IProductRepository>(),
                GetService<IProductSerialRepository>(),
                GetService<DataSource>(),
                GetService<IDynamicPricingService>()));

            RegisterSingleton<IBatchesService>(() => new BatchesService(
                GetService<IBatchesRepository>(), 
                GetService<IBatchProductRepository>(), 
                GetService<IPurchaseOrderRepository>(), 
                GetService<ISupplierRepository>(),
                GetService<DataSource>(),
                GetService<IDynamicPricingService>()));
            RegisterSingleton<ILocalStorageService>(() => new LocalStorageService());
            RegisterSingleton<IRoleService>(() => new RoleService(GetService<IRoleRepository>()));
            RegisterSingleton<IPermissionService>(() => new PermissionService(GetService<IPermissionRepository>()));
            RegisterSingleton<ICloudinaryService>(() => new CloudinaryService(GetService<IConfiguration>()));
            RegisterSingleton<IInvoiceService>(() => new InvoiceService(
                GetService<IInvoiceRepository>(),
                GetService<IPersonRepository>(),
                GetService<IEmployeeRepository>(),
                GetService<ISupplierRepository>(),
                GetService<IInvoiceLineRepository>(),
                GetService<IProductSerialRepository>(),
                GetService<IBatchProductRepository>(),
                GetService<IInvoiceLineSerialRepository>(),
                GetService<IProductRepository>(),
                GetService<DataSource>()
            ));

            // Register Dashboard Service
            RegisterSingleton<IDashboardService>(() => new DashboardService(
                GetService<IInvoiceRepository>(),
                GetService<IInvoiceLineRepository>(),
                GetService<ICustomerRepository>(),
                GetService<IProductRepository>()
            ));

            // Register UserSession singleton
            RegisterSingleton<UserSession>(UserSession.Instance);

            // Register SessionService (depends on other services and UserSession)
            RegisterSingleton<ISessionService>(() => new SessionService(
  GetService<IAuthService>(),
      GetService<IAccountService>(),
      GetService<IPersonService>(),
  GetService<UserSession>()));

            _isInitialized = true;
        }

        /// <summary>
        /// Build configuration from appsettings.json
        /// </summary>
        /// <returns>Configuration instance</returns>
        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            return builder.Build();
        }

        /// <summary>
        /// Register a singleton service with a factory method
        /// </summary>
        /// <typeparam name="T">Service type (interface)</typeparam>
        /// <param name="factory">Factory method to create the service</param>
        public static void RegisterSingleton<T>(Func<T> factory)
        {
            var serviceType = typeof(T);
            _factories[serviceType] = () => factory()!;
        }

        /// <summary>
        /// Register a singleton service instance
        /// </summary>
        /// <typeparam name="T">Service type (interface)</typeparam>
        /// <param name="instance">Service instance</param>
        public static void RegisterSingleton<T>(T instance)
        {
            var serviceType = typeof(T);
            _services[serviceType] = instance!;
        }

        /// <summary>
        /// Get a service from the container
        /// </summary>
        /// <typeparam name="T">Service type (interface)</typeparam>
        /// <returns>Service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is not registered</exception>
        public static T GetService<T>()
        {
            var serviceType = typeof(T);

            // Return existing instance if available
            if (_services.TryGetValue(serviceType, out var existingInstance))
            {
                return (T)existingInstance;
            }

            // Create new instance using factory
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                var newInstance = factory();
                _services[serviceType] = newInstance;
                return (T)newInstance;
            }

            throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>True if service is registered</returns>
        public static bool IsRegistered<T>()
        {
            var serviceType = typeof(T);
            return _services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType);
        }

        /// <summary>
        /// Clear all services (mainly for testing)
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            _factories.Clear();
            _isInitialized = false;
        }
    }
}
