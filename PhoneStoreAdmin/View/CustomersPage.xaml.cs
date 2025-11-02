using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class CustomersPage : Page
    {
        // Data collection
        public ObservableCollection<CustomerViewModel> Items { get; set; }
        
        // Services
        private readonly ICustomerService _customerService;
        
        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;
        private int _totalCount = 0;
        private string _currentSearchText = string.Empty;

        // Search debounce
        private Timer? _searchTimer;
        private const int SearchDelayMs = 300;

        // Page caching
        private readonly Dictionary<string, (List<Customer> Customers, int TotalCount)> _pageCache = new();
        private const int CachePagesAround = 3; // Cache 3 pages before and after
        private CancellationTokenSource? _cachingCts;

        public CustomersPage()
        {
            this.InitializeComponent();
            Items = new ObservableCollection<CustomerViewModel>();
            
            // Get service from container
            _customerService = ServiceContainer.GetService<ICustomerService>()
                ?? throw new InvalidOperationException("CustomerService not registered");
            
            // Load data asynchronously without blocking UI
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // Capture the current search text at the start
            var searchTextSnapshot = _currentSearchText;
            var cacheKey = GetCacheKey(_currentPage, searchTextSnapshot);
            
            try
            {
                // Show loading overlay (if available)
                LoadingOverlay?.Show("Đang tải dữ liệu...");

                // Check cache first
                if (_pageCache.TryGetValue(cacheKey, out var cachedData))
                {
                    Logger.Info($"Loading page {_currentPage} from cache");
                    var (cachedCustomers, cachedTotalCount) = cachedData;
                    
                    _totalCount = cachedTotalCount;
                    Items.Clear();
                    foreach (var customer in cachedCustomers.Select(MapToViewModel))
                    {
                        Items.Add(customer);
                    }

                    UpdateUI();
                    
                    Logger.Info($"Loaded {Items.Count} customers from cache");
                    
                    // Still trigger background caching for surrounding pages
                    _ = StartBackgroundCachingAsync(searchTextSnapshot);
                    return;
                }

                // Not in cache, fetch from service
                var result = await _customerService.GetCustomersFilteredAsync(searchTextSnapshot, _currentPage, _pageSize);
                
                // Check if search text has changed during the query
                if (searchTextSnapshot != _currentSearchText)
                {
                    // Search text changed, need to reload with new search text
                    Logger.Info($"Search text changed from '{searchTextSnapshot}' to '{_currentSearchText}', reloading...");
                    await LoadDataAsync();
                    return;
                }
                
                if (result != null)
                {
                    _totalCount = result.Info.TotalRecords;
                    
                    // Cache the result - need to get raw Customer objects
                    var rawCustomers = result.Customers.Select(vm => new Customer
                    {
                        Id = vm.Id,
                        Code = vm.Code,
                        FullName = vm.FullName,
                        Phone = vm.Phone,
                        Email = vm.Email,
                        Address = vm.Address,
                        IsActive = vm.IsActive,
                        CreatedAt = vm.CreatedAt
                    }).ToList();
                    _pageCache[cacheKey] = (rawCustomers, _totalCount);
                    
                    // Update Items collection
                    Items.Clear();
                    foreach (var customer in result.Customers)
                    {
                        Items.Add(customer);
                    }
                    
                    _totalPages = result.Info.TotalPages;

                    UpdateUI();

                    Logger.Info($"Loaded {Items.Count} customers (page {_currentPage}/{_totalPages}, total: {_totalCount})");
                    
                    // Start background caching after successful load
                    _ = StartBackgroundCachingAsync(searchTextSnapshot);
                }
                else
                {
                    Logger.Error("Failed to load customers from database.");
                    Items.Clear();
                    _totalCount = 0;
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading customer data", ex);
                Items.Clear();
                _totalCount = 0;
                UpdateUI();
            }
            finally
            {
                // Hide loading overlay
                LoadingOverlay?.Hide();
            }
        }

        private CustomerViewModel MapToViewModel(Customer customer)
        {
            return new CustomerViewModel
            {
                Id = customer.Id,
                Code = customer.Code,
                FullName = customer.FullName ?? "N/A",
                Phone = customer.Phone ?? "N/A",
                Email = customer.Email ?? "N/A",
                Address = customer.Address ?? "N/A",
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt
            };
        }

        private void UpdateUI()
        {
            // Update record count with proper Vietnamese grammar
            var count = Items.Count;
            RecordCountText.Text = count == 0 
                ? "Không có khách hàng"
                : count == 1 
                    ? "Hiển thị 1 khách hàng" 
                    : $"Hiển thị {count} khách hàng";
            
            // Update pagination
            PageInfoText.Text = $"Trang {_currentPage} / {Math.Max(1, _totalPages)}";
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            
            // Show/hide empty state
            if (Items.Count == 0)
            {
                DataListView.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
            else
            {
                DataListView.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
            }
        }

        // Header Button
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open add customer dialog or navigate to add page
        }

        // Toolbar Actions
        private void SearchBox_TextChanged(AutoSuggestBox sender, 
            AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var previousSearchText = _currentSearchText;
                _currentSearchText = sender.Text ?? string.Empty;
                _currentPage = 1; // Reset to first page when searching
                
                // Clear cache if search text changed
                if (previousSearchText != _currentSearchText)
                {
                    ClearCache();
                    _cachingCts?.Cancel(); // Cancel any ongoing caching
                }
                
                // Debounce: Cancel previous timer and create new one
                _searchTimer?.Dispose();
                _searchTimer = new Timer(
                    async _ =>
                    {
                        // Execute search on UI thread
                        DispatcherQueue.TryEnqueue(async () =>
                        {
                            await LoadDataAsync();
                        });
                    },
                    null,
                    SearchDelayMs,
                    Timeout.Infinite
                );
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show filter dialog (by status, created date, etc.)
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear search box
            if (SearchBox != null)
            {
                SearchBox.Text = string.Empty;
            }
            
            // Clear search text and reset to first page
            _currentSearchText = string.Empty;
            _currentPage = 1;
            
            // Clear cache and cancel any ongoing caching
            ClearCache();
            _cachingCts?.Cancel();
            
            // Reload data
            await LoadDataAsync();
        }

        // Table Actions
        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                FlyoutBase.ShowAttachedFlyout(button);
            }
        }

        private void ViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && 
                menuItem.Tag is CustomerViewModel customer)
            {
                // TODO: Navigate to customer detail page or show detail dialog
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && 
                menuItem.Tag is CustomerViewModel customer)
            {
                // TODO: Open edit customer dialog or navigate to edit page
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && 
                menuItem.Tag is CustomerViewModel customer)
            {
                // TODO: Show confirmation dialog and delete customer
            }
        }

        // Pagination
        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                _ = LoadDataAsync();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                _ = LoadDataAsync();
            }
        }

        #region Cache and Helper Methods

        private async Task StartBackgroundCachingAsync(string searchText)
        {
            // Cancel any existing caching operation
            _cachingCts?.Cancel();
            _cachingCts = new CancellationTokenSource();
            var token = _cachingCts.Token;

            try
            {
                // Determine pages to cache (3 before and 3 after current page)
                var pagesToCache = new List<int>();
                for (int i = Math.Max(1, _currentPage - CachePagesAround); 
                     i <= Math.Min(_totalPages, _currentPage + CachePagesAround); 
                     i++)
                {
                    if (i != _currentPage) // Don't cache current page (already loaded)
                    {
                        pagesToCache.Add(i);
                    }
                }

                Logger.Info($"Starting background cache for {pagesToCache.Count} pages: [{string.Join(", ", pagesToCache)}]");

                // Cache pages in background
                foreach (var pageIndex in pagesToCache)
                {
                    if (token.IsCancellationRequested)
                        break;

                    await CachePageAsync(pageIndex, searchText, token);
                    
                    // Small delay between caching to avoid overloading
                    await Task.Delay(100, token);
                }

                Logger.Info($"Background caching completed. Cache size: {_pageCache.Count}");
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Background caching cancelled");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during background caching", ex);
            }
        }

        private async Task CachePageAsync(int pageIndex, string searchText, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = GetCacheKey(pageIndex, searchText);
                
                // Skip if already cached
                if (_pageCache.ContainsKey(cacheKey))
                {
                    Logger.Info($"Page {pageIndex} already cached, skipping");
                    return;
                }

                // Fetch page data
                var result = await _customerService.GetCustomersFilteredAsync(searchText, pageIndex, _pageSize);
                
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (result != null)
                {
                    var rawCustomers = result.Customers.Select(vm => new Customer
                    {
                        Id = vm.Id,
                        Code = vm.Code,
                        FullName = vm.FullName,
                        Phone = vm.Phone,
                        Email = vm.Email,
                        Address = vm.Address,
                        IsActive = vm.IsActive
                    }).ToList();
                    
                    _pageCache[cacheKey] = (rawCustomers, result.Info.TotalRecords);
                    Logger.Info($"Cached page {pageIndex} with {rawCustomers.Count} customers");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to cache page {pageIndex}", ex);
            }
        }

        private string GetCacheKey(int pageIndex, string searchText)
        {
            return $"{pageIndex}_{searchText ?? ""}".ToLowerInvariant();
        }

        private void ClearCache()
        {
            _pageCache.Clear();
            Logger.Info("Page cache cleared");
        }

        #endregion
    }
}
