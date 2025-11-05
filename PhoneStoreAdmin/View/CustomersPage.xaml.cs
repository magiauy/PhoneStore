using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.View.Controls;
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

        private CustomerFilterCriteria _currentFilterCriteria = new CustomerFilterCriteria();
        private readonly ResourceLoader _resourceLoader = new ResourceLoader();

        private bool _isPageUnloaded;

        public CustomersPage()
        {
            this.InitializeComponent();
            Items = new ObservableCollection<CustomerViewModel>();
            
            // Get service from container
            _customerService = ServiceContainer.GetService<ICustomerService>()
                ?? throw new InvalidOperationException("CustomerService not registered");

            UpdateFilterBadge();

            // Load data asynchronously without blocking UI
            _ = LoadDataAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _isPageUnloaded = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _isPageUnloaded = true;

            _searchTimer?.Dispose();
            _searchTimer = null;

            if (_cachingCts != null)
            {
                if (!_cachingCts.IsCancellationRequested)
                {
                    _cachingCts.Cancel();
                }

                _cachingCts.Dispose();
                _cachingCts = null;
            }
        }

        private async Task LoadDataAsync()
        {
            // Capture the current search text at the start
            var searchTextSnapshot = _currentSearchText;
            var filterSnapshot = CloneFilterCriteria(_currentFilterCriteria);
            var filterKey = filterSnapshot?.GetCacheKey() ?? string.Empty;
            var cacheKey = GetCacheKey(_currentPage, searchTextSnapshot, filterKey);
            
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
                    _ = StartBackgroundCachingAsync(searchTextSnapshot, filterSnapshot);
                    return;
                }

                // Not in cache, fetch from service
                var result = await _customerService.GetCustomersFilteredAsync(searchTextSnapshot, filterSnapshot, _currentPage, _pageSize);

                // Check if search text has changed during the query
                if (searchTextSnapshot != _currentSearchText || !AreFiltersEqual(filterSnapshot, _currentFilterCriteria))
                {
                    Logger.Info("Search text or filter changed during load, reloading...");
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
                    _ = StartBackgroundCachingAsync(searchTextSnapshot, filterSnapshot);
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
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialogContent = new CustomerDialog();
            dialogContent.SetMode(CustomerDialog.DialogMode.Add);

            var dialog = new ContentDialog
            {
                Title = "Thêm khách hàng",
                Content = dialogContent,
                PrimaryButtonText = _resourceLoader.GetString("DialogAdd"),
                CloseButtonText = _resourceLoader.GetString("DialogCancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (string.IsNullOrWhiteSpace(dialog.PrimaryButtonText))
            {
                dialog.PrimaryButtonText = "Thêm";
            }

            if (string.IsNullOrWhiteSpace(dialog.CloseButtonText))
            {
                dialog.CloseButtonText = "Hủy";
            }

            dialog.PrimaryButtonClick += async (s, args) =>
            {
                if (dialog.Content is not CustomerDialog ctrl)
                {
                    return;
                }

                if (!ctrl.ValidateForm())
                {
                    args.Cancel = true;
                    return;
                }

                args.Cancel = true;

                var customer = ctrl.BuildCustomer();

                try
                {
                    ctrl.SetLoadingState(true);
                    LoadingOverlay?.Show("Đang thêm khách hàng...");

                    var result = await _customerService.AddCustomerAsync(customer);
                    if (result != null)
                    {
                        ctrl.NotifySaved(result);
                        dialog.Hide();

                        _currentPage = 1;
                        ClearCache();
                        _cachingCts?.Cancel();

                        await LoadDataAsync();
                    }
                    else
                    {
                        ctrl.ShowErrorMessage("Không thể thêm khách hàng. Vui lòng thử lại.");
                    }
                }
                catch (Exception ex)
                {
                    ctrl.ShowErrorMessage($"Lỗi khi thêm khách hàng: {ex.Message}");
                }
                finally
                {
                    ctrl.SetLoadingState(false);
                    LoadingOverlay?.Hide();
                }
            };

            await dialog.ShowAsync();
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
                        if (_isPageUnloaded)
                        {
                            return;
                        }

                        var dispatcher = DispatcherQueue;
                        if (dispatcher == null)
                        {
                            return;
                        }

                        if (!dispatcher.TryEnqueue(async () =>
                        {
                            if (_isPageUnloaded)
                            {
                                return;
                            }

                            await LoadDataAsync();
                        }))
                        {
                            return;
                        }
                    },
                    null,
                    SearchDelayMs,
                    Timeout.Infinite
                );
            }
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerFilterDialog
            {
                XamlRoot = this.XamlRoot
            };

            dialog.SetCurrentFilters(CloneFilterCriteria(_currentFilterCriteria));

            await dialog.ShowAsync();

            if (dialog.IsApplied)
            {
                _currentFilterCriteria = dialog.FilterCriteria;
                _currentPage = 1;

                ClearCache();
                _cachingCts?.Cancel();

                UpdateFilterBadge();

                await LoadDataAsync();
            }
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

            _currentFilterCriteria = new CustomerFilterCriteria();
            UpdateFilterBadge();

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
                var flyout = FlyoutBase.GetAttachedFlyout(button);
                if (flyout != null)
                {
                    flyout.ShowAt(button);
                }
            }
        }

        private async void ViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem &&
                menuItem.Tag is CustomerViewModel customer)
            {
                var dialogContent = new CustomerDialog();
                dialogContent.SetMode(CustomerDialog.DialogMode.View, CloneCustomerViewModel(customer));

                var dialog = new ContentDialog
                {
                    Title = "Thông tin khách hàng",
                    Content = dialogContent,
                    PrimaryButtonText = "Đóng",
                    CloseButtonText = string.Empty,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }

        private async void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem &&
                menuItem.Tag is CustomerViewModel customer)
            {
                var dialogContent = new CustomerDialog();
                dialogContent.SetMode(CustomerDialog.DialogMode.Edit, CloneCustomerViewModel(customer));

                var dialog = new ContentDialog
                {
                    Title = "Chỉnh sửa khách hàng",
                    Content = dialogContent,
                    PrimaryButtonText = _resourceLoader.GetString("DialogUpdate"),
                    CloseButtonText = _resourceLoader.GetString("DialogCancel"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                if (string.IsNullOrWhiteSpace(dialog.PrimaryButtonText))
                {
                    dialog.PrimaryButtonText = "Cập nhật";
                }

                if (string.IsNullOrWhiteSpace(dialog.CloseButtonText))
                {
                    dialog.CloseButtonText = "Hủy";
                }

                dialog.PrimaryButtonClick += async (s, args) =>
                {
                    if (dialog.Content is not CustomerDialog ctrl)
                    {
                        return;
                    }

                    if (!ctrl.ValidateForm())
                    {
                        args.Cancel = true;
                        return;
                    }

                    args.Cancel = true;

                    var updatedCustomer = ctrl.BuildCustomer();

                    try
                    {
                        ctrl.SetLoadingState(true);
                        LoadingOverlay?.Show("Đang cập nhật khách hàng...");

                        var success = await _customerService.UpdateCustomerAsync(updatedCustomer);
                        if (success)
                        {
                            dialog.Hide();

                            ClearCache();
                            _cachingCts?.Cancel();

                            await LoadDataAsync();
                        }
                        else
                        {
                            ctrl.ShowErrorMessage("Không thể cập nhật khách hàng. Vui lòng thử lại.");
                        }
                    }
                    catch (Exception ex)
                    {
                        ctrl.ShowErrorMessage($"Lỗi khi cập nhật khách hàng: {ex.Message}");
                    }
                    finally
                    {
                        ctrl.SetLoadingState(false);
                        LoadingOverlay?.Hide();
                    }
                };

                await dialog.ShowAsync();
            }
        }

        private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem &&
                menuItem.Tag is CustomerViewModel customer)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Xóa khách hàng",
                    Content = $"Bạn có chắc chắn muốn xóa khách hàng '{customer.FullName}'?",
                    PrimaryButtonText = "Xóa",
                    CloseButtonText = _resourceLoader.GetString("DialogCancel"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                if (string.IsNullOrWhiteSpace(confirmDialog.CloseButtonText))
                {
                    confirmDialog.CloseButtonText = "Hủy";
                }

                var result = await confirmDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        LoadingOverlay?.Show("Đang xóa khách hàng...");

                        var success = await _customerService.DeleteCustomerAsync(customer.Id);
                        if (success)
                        {
                            ClearCache();
                            _cachingCts?.Cancel();
                            await LoadDataAsync();
                        }
                        else
                        {
                            await ShowMessageAsync("Không thể xóa", "Không thể xóa khách hàng. Vui lòng thử lại.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowMessageAsync("Lỗi", $"Xảy ra lỗi khi xóa khách hàng: {ex.Message}");
                    }
                    finally
                    {
                        LoadingOverlay?.Hide();
                    }
                }
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

        private async Task StartBackgroundCachingAsync(string searchText, CustomerFilterCriteria? filterCriteria)
        {
            // Cancel any existing caching operation
            _cachingCts?.Cancel();
            _cachingCts = new CancellationTokenSource();
            var token = _cachingCts.Token;

            var filterSnapshot = CloneFilterCriteria(filterCriteria);
            var filterKey = filterSnapshot?.GetCacheKey() ?? string.Empty;

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
                    {
                        break;
                    }

                    await CachePageAsync(pageIndex, searchText, filterSnapshot, filterKey, token);

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

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

        private async Task CachePageAsync(int pageIndex, string searchText, CustomerFilterCriteria? filterCriteria, string filterKey, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var cacheKey = GetCacheKey(pageIndex, searchText, filterKey);

                // Skip if already cached
                if (_pageCache.ContainsKey(cacheKey))
                {
                    Logger.Info($"Page {pageIndex} already cached, skipping");
                    return;
                }

                // Fetch page data
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var result = await _customerService.GetCustomersFilteredAsync(searchText, filterCriteria, pageIndex, _pageSize);

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
                        IsActive = vm.IsActive,
                        CreatedAt = vm.CreatedAt
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

        private string GetCacheKey(int pageIndex, string searchText, string filterKey)
        {
            return $"{pageIndex}_{searchText ?? ""}_{filterKey}".ToLowerInvariant();
        }

        private static CustomerViewModel CloneCustomerViewModel(CustomerViewModel source)
        {
            return new CustomerViewModel
            {
                Id = source.Id,
                Code = source.Code,
                FullName = source.FullName,
                Phone = source.Phone,
                Email = source.Email,
                Address = source.Address,
                IsActive = source.IsActive,
                CreatedAt = source.CreatedAt
            };
        }

        private void UpdateFilterBadge()
        {
            if (FilterStatusBadge == null || FilterStatusText == null)
            {
                return;
            }

            if (_currentFilterCriteria != null && _currentFilterCriteria.HasAnyFilter())
            {
                FilterStatusBadge.Visibility = Visibility.Visible;
                FilterStatusText.Text = BuildFilterSummary(_currentFilterCriteria);
            }
            else
            {
                FilterStatusBadge.Visibility = Visibility.Collapsed;
                FilterStatusText.Text = "Không có bộ lọc";
            }
        }

        private string BuildFilterSummary(CustomerFilterCriteria criteria)
        {
            var summaries = new List<string>();

            switch (criteria.Status)
            {
                case "Active":
                    summaries.Add("Đang hoạt động");
                    break;
                case "Inactive":
                    summaries.Add("Ngưng hoạt động");
                    break;
            }

            if (criteria.CreatedFrom.HasValue || criteria.CreatedTo.HasValue)
            {
                var from = criteria.CreatedFrom?.ToString("dd/MM/yyyy") ?? "...";
                var to = criteria.CreatedTo?.ToString("dd/MM/yyyy") ?? "...";
                summaries.Add($"Ngày tạo: {from} - {to}");
            }

            if (!string.IsNullOrWhiteSpace(criteria.PhonePrefix))
            {
                summaries.Add($"Đầu số: {criteria.PhonePrefix}");
            }

            if (!string.IsNullOrWhiteSpace(criteria.City))
            {
                summaries.Add($"Thành phố: {criteria.City}");
            }

            if (criteria.HasEmail == true)
            {
                summaries.Add("Có email");
            }

            if (criteria.HasAddress == true)
            {
                summaries.Add("Có địa chỉ");
            }

            return summaries.Count > 0
                ? string.Join(" • ", summaries)
                : "Đã áp dụng bộ lọc";
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private static CustomerFilterCriteria? CloneFilterCriteria(CustomerFilterCriteria? criteria)
        {
            if (criteria == null)
            {
                return null;
            }

            return new CustomerFilterCriteria
            {
                Status = criteria.Status,
                CreatedFrom = criteria.CreatedFrom,
                CreatedTo = criteria.CreatedTo,
                PhonePrefix = criteria.PhonePrefix,
                City = criteria.City,
                HasEmail = criteria.HasEmail,
                HasAddress = criteria.HasAddress
            };
        }

        private static bool AreFiltersEqual(CustomerFilterCriteria? first, CustomerFilterCriteria? second)
        {
            if (first == null && second == null)
            {
                return true;
            }

            if (first == null || second == null)
            {
                return false;
            }

            return string.Equals(first.GetCacheKey(), second.GetCacheKey(), StringComparison.Ordinal);
        }

        private void ClearCache()
        {
            _pageCache.Clear();
            Logger.Info("Page cache cleared");
        }

        #endregion
    }
}
