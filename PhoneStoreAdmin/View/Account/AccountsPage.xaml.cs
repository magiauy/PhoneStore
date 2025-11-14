using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoneStoreRepository.Models;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreRepository.Utils;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Helpers;

namespace PhoneStoreAdmin.View
{
    public sealed partial class AccountsPage : Page
    {
        // ResourceLoader for localization
        private readonly ResourceLoader _resourceLoader = new();
        
        // ObservableCollection để bind với ListView
        public ObservableCollection<AccountViewModel> Accounts { get; set; }
        public ObservableCollection<AccountEmployeeViewModel> EmployeesWithoutAccount { get; set; }
        
        // Phân trang
        private int _currentPage = 1;
        private int _itemsPerPage = 10;
        private int _totalPages = 1;
        private int _totalCount = 0;
        private string _currentSearchText = string.Empty;

        // Search debounce
        private Timer? _searchTimer;
        private const int SearchDelayMs = 300;

        // Page caching
        private readonly Dictionary<string, (List<Account> Accounts, int TotalCount)> _pageCache = new();
        private const int CachePagesAround = 3; // Cache 3 pages before and after
        private CancellationTokenSource? _cachingCts;

        // Service
        private readonly IAccountService _accountService;
        private readonly IEmployeeService _employeeService;
        
        // Selected employee for account creation
        private Employee? _selectedEmployee;
        
        // Selected role IDs for account creation
        private List<int> _selectedRoleIds = new();
        
        // All employees for filtering
        private List<AccountEmployeeViewModel> _allEmployees = new();
        
        // Filter criteria
        private AccountFilterCriteria? _currentFilterCriteria;
        
        // Filter tags collection
        public ObservableCollection<FilterTag> FilterTags { get; set; }

        public AccountsPage()
        {
            this.InitializeComponent();
            Accounts = new ObservableCollection<AccountViewModel>();
            EmployeesWithoutAccount = new ObservableCollection<AccountEmployeeViewModel>();
            FilterTags = new ObservableCollection<FilterTag>();
            
            // Bind FilterTags to ItemsControl
            if (FilterTagsPanel != null)
            {
                FilterTagsPanel.ItemsSource = FilterTags;
            }
            
            // Gán DataContext cho binding
            this.DataContext = this;
            
            // Get service from ServiceContainer
            _accountService = ServiceContainer.GetService<IAccountService>() 
                ?? throw new InvalidOperationException("AccountService not registered");
            _employeeService = ServiceContainer.GetService<IEmployeeService>()
                ?? throw new InvalidOperationException("EmployeeService not registered");
            
            // Initialize localized strings
            InitializeLocalizedStrings();
        }
        
        private void InitializeLocalizedStrings()
        {
            // Set ToolTip content for pagination buttons
            ToolTipService.SetToolTip(PreviousPageButton, _resourceLoader.GetString("Common/PreviousPage"));
            ToolTipService.SetToolTip(NextPageButton, _resourceLoader.GetString("Common/NextPage"));
            
            // Set Button content
            CreateAccountButton.Content = _resourceLoader.GetString("Common/Create");
            CancelButton.Content = _resourceLoader.GetString("Common/Cancel");
            
            // Set initial text for dynamic elements
            RecordCountText.Text = string.Format(
                _resourceLoader.GetString("Accounts_RecordCount"), 
                0, 0);
            PageInfoText.Text = string.Format(
                _resourceLoader.GetString("Accounts_PageInfo"), 
                1, 1);
            SelectedRolesText.Text = _resourceLoader.GetString("Accounts_SelectRolesPlaceholder");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Load data from database when navigated to this page
            await LoadDataFromDatabaseAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Dispose search timer to prevent callbacks after navigation
            _searchTimer?.Dispose();
            _searchTimer = null;

            // Cancel background caching operations
            if (_cachingCts != null)
            {
                _cachingCts.Cancel();
                _cachingCts.Dispose();
                _cachingCts = null;
            }

            // Clear cached data
            ClearCache();

            // Reset selected employee and role selections to avoid stale data
            _selectedEmployee = null;
            _selectedRoleIds.Clear();

            if (EmployeesListView != null)
            {
                EmployeesListView.SelectedItem = null;
            }

            if (SelectedRolesText != null)
            {
                SelectedRolesText.Text = _resourceLoader.GetString("Accounts_SelectRolesPlaceholder");
            }
        }

        #region Event Handlers

        private void AccountsListTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Accounts List tab
            UpdateTabStyle(AccountsListTab, EmployeesWithoutAccountTab, true);
            
            // Show accounts list content
            AccountsListContent.Visibility = Visibility.Visible;
            EmployeesWithoutAccountContent.Visibility = Visibility.Collapsed;
        }

        private async void EmployeesWithoutAccountTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Employees Without Account tab
            UpdateTabStyle(EmployeesWithoutAccountTab, AccountsListTab, false);
            
            // Show employees without account content
            AccountsListContent.Visibility = Visibility.Collapsed;
            EmployeesWithoutAccountContent.Visibility = Visibility.Visible;
            
            // Load employees without account
            await LoadEmployeesWithoutAccountAsync();
        }

        private void UpdateTabStyle(Button activeTab, Button inactiveTab, bool isFirstTab)
        {
            // First, reset both tabs to Normal state by clearing hover
            Microsoft.UI.Xaml.VisualStateManager.GoToState(activeTab, "Normal", false);
            Microsoft.UI.Xaml.VisualStateManager.GoToState(inactiveTab, "Normal", false);
            
            // Disable active tab to prevent hover
            activeTab.IsEnabled = false;
            
            // Active tab - light blue background
            activeTab.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 219, 234, 254)); // #dbeafe
            activeTab.CornerRadius = isFirstTab ? new CornerRadius(12, 0, 0, 12) : new CornerRadius(0, 12, 12, 0);
            
            // Enable inactive tab for hover
            inactiveTab.IsEnabled = true;
            
            // Inactive tab - transparent background
            inactiveTab.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0, 0, 0, 0)); // Transparent
            inactiveTab.CornerRadius = isFirstTab ? new CornerRadius(0, 12, 12, 0) : new CornerRadius(12, 0, 0, 12);
            
            // Update text colors and weights
            UpdateButtonTextStyle(activeTab, true);
            UpdateButtonTextStyle(inactiveTab, false);
        }

        private void UpdateButtonTextStyle(Button button, bool isActive)
        {
            if (button.Content is TextBlock textBlock)
            {
                textBlock.Foreground = isActive 
                    ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrushTextPrimary"]
                    : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrushTextSecondary"];
                textBlock.FontWeight = isActive 
                    ? Microsoft.UI.Text.FontWeights.SemiBold 
                    : Microsoft.UI.Text.FontWeights.Normal;
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var previousSearchText = _currentSearchText;
                _currentSearchText = sender.Text;
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
                        var dispatcherQueue = DispatcherQueue;
                        if (dispatcherQueue == null)
                        {
                            return;
                        }

                        dispatcherQueue.TryEnqueue(async () =>
                        {
                            if (!IsLoaded)
                            {
                                return;
                            }

                            await LoadPageDataAsync();
                        });
                    },
                    null,
                    SearchDelayMs,
                    Timeout.Infinite
                );
            }
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Controls.AccountFilterDialog
            {
                XamlRoot = this.XamlRoot
            };

            // Set current filters if any
            if (_currentFilterCriteria != null)
            {
                dialog.SetCurrentFilters(_currentFilterCriteria);
            }

            var result = await dialog.ShowAsync();

            // If user clicked Apply button
            if (dialog.IsApplied)
            {
                _currentFilterCriteria = dialog.FilterCriteria;
                
                // Reset to first page when applying filters
                _currentPage = 1;
                
                // Update filter tags display
                UpdateFilterTags();
                
                // Clear cache and reload data with filters
                ClearCache();
                await LoadDataFromDatabaseAsync();
                
                Logger.Info($"Filters applied: {(_currentFilterCriteria.HasAnyFilter() ? "Yes" : "No")}");
            }
        }

        private void UpdateFilterTags()
        {
            FilterTags.Clear();
            
            if (_currentFilterCriteria == null || !_currentFilterCriteria.HasAnyFilter())
            {
                FilterTagsPanel.Visibility = Visibility.Collapsed;
                FilterActiveBadge.Visibility = Visibility.Collapsed;
                return;
            }

            // Status filter
            if (_currentFilterCriteria.Status != "All")
            {
                var statusLabel = _currentFilterCriteria.Status == "Activated" 
                    ? LocalizationHelper.GetString("Accounts_Status_Activated")
                    : LocalizationHelper.GetString("Accounts_Status_Deactivated");
                var filterLabel = string.Format(LocalizationHelper.GetString("Accounts_Filter_Status"), statusLabel);
                FilterTags.Add(new FilterTag 
                { 
                    Key = "Status", 
                    Label = filterLabel
                });
            }

            // Account Type filter
            if (_currentFilterCriteria.AccountType != "All")
            {
                var typeLabel = _currentFilterCriteria.AccountType == "Employee" 
                    ? LocalizationHelper.GetString("Accounts_Type_Employee")
                    : LocalizationHelper.GetString("Accounts_Type_Customer");
                var filterLabel = string.Format(LocalizationHelper.GetString("Accounts_Filter_Type"), typeLabel);
                FilterTags.Add(new FilterTag 
                { 
                    Key = "AccountType", 
                    Label = filterLabel
                });
            }

            // Created Date Range
            if (_currentFilterCriteria.CreatedFrom.HasValue || _currentFilterCriteria.CreatedTo.HasValue)
            {
                var from = _currentFilterCriteria.CreatedFrom?.ToString("dd/MM/yyyy") ?? "...";
                var to = _currentFilterCriteria.CreatedTo?.ToString("dd/MM/yyyy") ?? "...";
                var filterLabel = string.Format(LocalizationHelper.GetString("Accounts_Filter_CreatedDate"), from, to);
                FilterTags.Add(new FilterTag 
                { 
                    Key = "CreatedDate", 
                    Label = filterLabel
                });
            }

            // Last Login filter
            if (_currentFilterCriteria.NeverLoggedIn)
            {
                FilterTags.Add(new FilterTag 
                { 
                    Key = "NeverLoggedIn", 
                    Label = LocalizationHelper.GetString("Accounts_Filter_NeverLoggedIn")
                });
            }
            else if (_currentFilterCriteria.LastLoginFrom.HasValue || _currentFilterCriteria.LastLoginTo.HasValue)
            {
                var from = _currentFilterCriteria.LastLoginFrom?.ToString("dd/MM/yyyy") ?? "...";
                var to = _currentFilterCriteria.LastLoginTo?.ToString("dd/MM/yyyy") ?? "...";
                var filterLabel = string.Format(LocalizationHelper.GetString("Accounts_Filter_LastLogin"), from, to);
                FilterTags.Add(new FilterTag 
                { 
                    Key = "LastLogin", 
                    Label = filterLabel
                });
            }

            // Show filter tags panel and badge
            FilterTagsPanel.Visibility = FilterTags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            FilterActiveBadge.Visibility = FilterTags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void RemoveFilterTag_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var filterKey = button?.Tag?.ToString();
            
            if (string.IsNullOrEmpty(filterKey) || _currentFilterCriteria == null)
                return;

            // Remove specific filter based on key
            switch (filterKey)
            {
                case "Status":
                    _currentFilterCriteria.Status = "All";
                    break;
                case "AccountType":
                    _currentFilterCriteria.AccountType = "All";
                    break;
                case "CreatedDate":
                    _currentFilterCriteria.CreatedFrom = null;
                    _currentFilterCriteria.CreatedTo = null;
                    break;
                case "NeverLoggedIn":
                    _currentFilterCriteria.NeverLoggedIn = false;
                    break;
                case "LastLogin":
                    _currentFilterCriteria.LastLoginFrom = null;
                    _currentFilterCriteria.LastLoginTo = null;
                    break;
            }

            // Check if all filters are cleared
            if (!_currentFilterCriteria.HasAnyFilter())
            {
                _currentFilterCriteria = null;
            }

            // Update display and reload data
            _currentPage = 1;
            UpdateFilterTags();
            ClearCache();
            await LoadDataFromDatabaseAsync();
            
            Logger.Info($"Filter '{filterKey}' removed");
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
            
            // Clear filters
            _currentFilterCriteria = null;
            UpdateFilterTags();
            
            // Clear cache and cancel any ongoing caching
            ClearCache();
            _cachingCts?.Cancel();
            
            // Reload data
            await LoadDataFromDatabaseAsync();
        }

        private void AccountsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Not used anymore since SelectionMode="None"
        }

        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                FlyoutBase.ShowAttachedFlyout(button);
            }
        }

        private void EditAccountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is AccountViewModel account)
            {
                Frame.Navigate(typeof(AccountEditPage), account);
            }
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                _ = LoadPageDataAsync();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                _ = LoadPageDataAsync();
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadDataFromDatabaseAsync()
        {
            await LoadPageDataAsync();
        }

        private async Task LoadPageDataAsync()
        {
            // Capture the current search text at the start
            var searchTextSnapshot = _currentSearchText;
            var cacheKey = GetCacheKey(_currentPage, searchTextSnapshot);
            
            try
            {
                // Show loading overlay
                LoadingOverlay?.Show(LocalizationHelper.GetString("LoadingData"));

                // Check cache first
                if (_pageCache.TryGetValue(cacheKey, out var cachedData))
                {
                    Logger.Info($"Loading page {_currentPage} from cache");
                    var (cachedAccounts, cachedTotalCount) = cachedData;
                    
                    _totalCount = cachedTotalCount;
                    Accounts.Clear();
                    foreach (var account in cachedAccounts.Select(MapToViewModel))
                    {
                        Accounts.Add(account);
                    }

                    UpdatePagination();
                    UpdateRecordCount(Accounts.Count, _totalCount);
                    UpdateEmptyStateVisibility();
                    
                    Logger.Info($"Loaded {Accounts.Count} accounts from cache");
                    
                    // Still trigger background caching for surrounding pages
                    _ = StartBackgroundCachingAsync(searchTextSnapshot);
                    return;
                }

                // Not in cache, fetch from service
                var result = await _accountService.GetPagedAccountsAsync(_currentPage, _itemsPerPage, searchTextSnapshot, _currentFilterCriteria);
                
                // Check if search text has changed during the query
                if (searchTextSnapshot != _currentSearchText)
                {
                    // Search text changed, need to reload with new search text
                    Logger.Info($"Search text changed from '{searchTextSnapshot}' to '{_currentSearchText}', reloading...");
                    await LoadPageDataAsync();
                    return;
                }
                
                if (result.HasValue)
                {
                    var (accounts, totalCount) = result.Value;
                    _totalCount = totalCount;
                    
                    // Cache the result
                    _pageCache[cacheKey] = (accounts, totalCount);
                    
                    Accounts.Clear();
                    foreach (var account in accounts.Select(MapToViewModel))
                    {
                        Accounts.Add(account);
                    }

                    UpdatePagination();
                    UpdateRecordCount(Accounts.Count, _totalCount);
                    UpdateEmptyStateVisibility();

                    Logger.Info($"Đã tải {Accounts.Count} tài khoản (trang {_currentPage}/{_totalPages}, tổng: {_totalCount})");
                    
                    // Start background caching after successful load
                    _ = StartBackgroundCachingAsync(searchTextSnapshot);
                }
                else
                {
                    Logger.Error("Không thể tải danh sách tài khoản từ cơ sở dữ liệu.");
                    Accounts.Clear();
                    _totalCount = 0;
                    UpdatePagination();
                    UpdateEmptyStateVisibility();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Đã xảy ra lỗi khi tải dữ liệu tài khoản", ex);
                Accounts.Clear();
                _totalCount = 0;
                UpdatePagination();
                UpdateEmptyStateVisibility();
            }
            finally
            {
                // Hide loading overlay
                LoadingOverlay?.Hide();
            }
        }

        private AccountViewModel MapToViewModel(Account account)
        {
            return new AccountViewModel
            {
                Id = account.Id,
                Username = account.Username,
                FullName = account.Person?.FullName ?? _resourceLoader.GetString("Common/NotAvailable"),
                PersonType = account.Person?.PersonType == Models.Enums.PersonType.EMPLOYEE 
                    ? LocalizationHelper.GetString("Accounts_Type_Employee")
                    : LocalizationHelper.GetString("Accounts_Type_Customer"),
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt,
                LastLogin = account.LastLogin
            };
        }

        private void UpdatePagination()
        {
            _totalPages = (int)Math.Ceiling((double)_totalCount / _itemsPerPage);
            if (_totalPages == 0) _totalPages = 1;
            
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            
            PageInfoText.Text = string.Format(_resourceLoader.GetString("Accounts_PageInfo"), _currentPage, _totalPages);
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = Accounts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (AccountsListView != null)
                AccountsListView.Visibility = Accounts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateRecordCount(int displayed, int total)
        {
            RecordCountText.Text = string.Format(_resourceLoader.GetString("Accounts_RecordCount"), displayed, total);
        }

        private async Task ShowNotImplementedMessage(string feature)
        {
            var dialog = new ContentDialog
            {
                Title = "Thông báo",
                Content = $"Tính năng '{feature}' chưa được triển khai.",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

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
                var result = await _accountService.GetPagedAccountsAsync(pageIndex, _itemsPerPage, searchText, _currentFilterCriteria);
                
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (result.HasValue)
                {
                    var (accounts, totalCount) = result.Value;
                    _pageCache[cacheKey] = (accounts, totalCount);
                    Logger.Info($"Cached page {pageIndex} with {accounts.Count} accounts");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to cache page {pageIndex}", ex);
            }
        }

        private string GetCacheKey(int pageIndex, string searchText)
        {
            var filterKey = _currentFilterCriteria?.GetCacheKey() ?? "nofilter";
            return $"{pageIndex}_{searchText ?? ""}_{filterKey}".ToLowerInvariant();
        }

        private void ClearCache()
        {
            _pageCache.Clear();
            Logger.Info("Page cache cleared");
        }

        #endregion

        private void AddAccountButton_Holding(object sender, Microsoft.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            

        }

        #region Tab 2: Employees Without Account

        private async Task LoadEmployeesWithoutAccountAsync()
        {
            try
            {
                var employees = await _employeeService.GetEmployeesWithoutAccountAsync();
                
                _allEmployees.Clear();
                EmployeesWithoutAccount.Clear();
                
                if (employees != null && employees.Count > 0)
                {
                    foreach (var employee in employees)
                    {
                        var viewModel = MapToEmployeeViewModel(employee);
                        _allEmployees.Add(viewModel);
                        EmployeesWithoutAccount.Add(viewModel);
                    }
                    
                    EmptyEmployeeState.Visibility = Visibility.Collapsed;
                    EmployeesListView.Visibility = Visibility.Visible;
                    
                    Logger.Info($"Loaded {employees.Count} employees without account");
                }
                else
                {
                    EmptyEmployeeState.Visibility = Visibility.Visible;
                    EmployeesListView.Visibility = Visibility.Collapsed;
                    Logger.Info("No employees without account found");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading employees without account", ex);
                EmptyEmployeeState.Visibility = Visibility.Visible;
                EmployeesListView.Visibility = Visibility.Collapsed;
            }
        }

        private void EmployeeSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                FilterEmployees(sender.Text);
            }
        }

        private void FilterEmployees(string searchText)
        {
            EmployeesWithoutAccount.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show all employees
                foreach (var employee in _allEmployees)
                {
                    EmployeesWithoutAccount.Add(employee);
                }
            }
            else
            {
                // Filter employees by name, code, phone, or email
                var searchLower = searchText.ToLower();
                var filtered = _allEmployees.Where(e =>
                    e.FullName.ToLower().Contains(searchLower) ||
                    e.EmployeeCode.ToLower().Contains(searchLower) ||
                    e.Phone.ToLower().Contains(searchLower) ||
                    e.Email.ToLower().Contains(searchLower)
                );

                foreach (var employee in filtered)
                {
                    EmployeesWithoutAccount.Add(employee);
                }
            }

            // Update visibility
            if (EmployeesWithoutAccount.Count == 0)
            {
                EmptyEmployeeState.Visibility = Visibility.Visible;
                EmployeesListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyEmployeeState.Visibility = Visibility.Collapsed;
                EmployeesListView.Visibility = Visibility.Visible;
            }
        }

        private AccountEmployeeViewModel MapToEmployeeViewModel(Employee employee)
        {
            return new AccountEmployeeViewModel
            {
                Id = employee.Id,
                FullName = employee.FullName ?? _resourceLoader.GetString("Common/NotAvailable"),
                EmployeeCode = $"NV{employee.Id:D3}",
                Phone = employee.Phone ?? _resourceLoader.GetString("Common/NotAvailable"),
                Email = employee.Email ?? _resourceLoader.GetString("Common/NotAvailable")
            };
        }

        private void EmployeesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmployeesListView.SelectedItem is AccountEmployeeViewModel selectedEmployee)
            {
                // Store selected employee ID
                var employeeId = selectedEmployee.Id;
                _selectedEmployee = new Employee { Id = employeeId };
                
                // Show form panel
                NoEmployeeSelectedPanel.Visibility = Visibility.Collapsed;
                AccountFormPanel.Visibility = Visibility.Visible;
                
                // Clear form
                ClearAccountForm();
                
                Logger.Info($"Selected employee: {selectedEmployee.FullName}");
            }
            else
            {
                _selectedEmployee = null;
                NoEmployeeSelectedPanel.Visibility = Visibility.Visible;
                AccountFormPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void SelectRolesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var roleService = ServiceContainer.GetService<IRoleService>();
                if (roleService == null)
                {
                    ShowFormError(_resourceLoader.GetString("Accounts_Error_RoleServiceNotAvailable"));
                    return;
                }

                var dialog = new Controls.RoleMultiSelectorDialog(roleService, _selectedRoleIds)
                {
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    _selectedRoleIds = dialog.SelectedRoleIds;
                    UpdateSelectedRolesText();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error opening role selector: {ex.Message}", ex);
                ShowFormError(string.Format(_resourceLoader.GetString("Accounts_Error_RoleSelectorFailed"), ex.Message));
            }
        }

        private void UpdateSelectedRolesText()
        {
            if (_selectedRoleIds.Count == 0)
            {
                SelectedRolesText.Text = _resourceLoader.GetString("Accounts_SelectRolesPlaceholder");
                SelectedRolesText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrushTextSecondary"];
            }
            else
            {
                SelectedRolesText.Text = string.Format(_resourceLoader.GetString("Accounts_RolesSelectedCount"), _selectedRoleIds.Count);
                SelectedRolesText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrushTextPrimary"];
            }
        }

        private async void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
            {
                ShowFormError(LocalizationHelper.GetString("Accounts_Validation_SelectEmployee"));
                return;
            }

            // Validate inputs
            var username = UsernameTextBox.Text.Trim();
            var password = AccountPasswordBox.Password;
            var confirmPassword = AccountConfirmPasswordBox.Password;
            
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowFormError(LocalizationHelper.GetString("Accounts_Validation_EnterUsername"));
                UsernameTextBox.Focus(FocusState.Programmatic);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowFormError(LocalizationHelper.GetString("Accounts_Validation_EnterPassword"));
                AccountPasswordBox.Focus(FocusState.Programmatic);
                return;
            }

            if (password != confirmPassword)
            {
                ShowFormError(LocalizationHelper.GetString("Accounts_Validation_PasswordMismatch"));
                AccountConfirmPasswordBox.Focus(FocusState.Programmatic);
                return;
            }

            if (_selectedRoleIds.Count == 0)
            {
                ShowFormError(_resourceLoader.GetString("Accounts_Validation_SelectRole"));
                SelectRolesButton.Focus(FocusState.Programmatic);
                return;
            }

            try
            {
                // TODO: Create account using AccountService
                // var newAccount = new Account
                // {
                //     Username = username,
                //     PasswordHash = password, // Should be hashed
                //     PersonId = _selectedEmployee.PersonId,
                //     IsActive = ActivateAccountCheckBox.IsChecked ?? true
                // };
                // 
                // var success = await _accountService.CreateAccountAsync(newAccount);
                
                await ShowSuccessMessage(LocalizationHelper.GetString("Accounts_Success_AccountCreated"));
                
                // Reload employees list
                await LoadEmployeesWithoutAccountAsync();
                
                // Clear selection
                EmployeesListView.SelectedItem = null;
            }
            catch (Exception ex)
            {
                Logger.Error("Error creating account", ex);
                ShowFormError(string.Format(LocalizationHelper.GetString("Accounts_Error_AccountCreationFailed"), ex.Message));
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear selection
            EmployeesListView.SelectedItem = null;
        }

        private void ClearAccountForm()
        {
            UsernameTextBox.Text = string.Empty;
            AccountPasswordBox.Password = string.Empty;
            AccountConfirmPasswordBox.Password = string.Empty;
            _selectedRoleIds.Clear();
            UpdateSelectedRolesText();
            ActivateAccountCheckBox.IsChecked = true;
            FormErrorInfoBar.IsOpen = false;
        }

        private void ShowFormError(string message)
        {
            FormErrorInfoBar.Message = message;
            FormErrorInfoBar.IsOpen = true;
        }

        private async Task ShowSuccessMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("Common/Success"),
                Content = message,
                CloseButtonText = _resourceLoader.GetString("Common/Close"),
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        #endregion
    }

    #region ViewModel Classes

    public class AccountViewModel
    {        
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PersonType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        
        // Properties for UI display
        public string StatusText => IsActive 
            ? LocalizationHelper.GetString("Accounts_VM_Status_Active")
            : LocalizationHelper.GetString("Accounts_VM_Status_Deactivated");
        public string StatusColor => IsActive ? "#28a745" : "#dc3545";
        public string StatusIcon => IsActive ? "\uE8BB" : "\uE711";
// E8BB = CheckMark (active), E711 = Block/Close (inactive)

        public string CreatedAtText => CreatedAt.ToString("dd/MM/yyyy");

        public string LastLoginText => LastLogin?.ToString("dd/MM/yyyy HH:mm") 
            ?? LocalizationHelper.GetString("Accounts_NeverLoggedIn");
        public string PersonTypeColor => PersonType == LocalizationHelper.GetString("Accounts_Type_Employee") 
            ? "#007bff" 
            : "#17a2b8";
    }

    public class AccountEmployeeViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class FilterTag
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    #endregion
}
