using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class EmployeesPage : Page
    {
        // Data collection
        public ObservableCollection<EmployeeViewModel> Items { get; set; }

        // Services
        private readonly IEmployeeService _employeeService;

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
        private readonly Dictionary<string, (List<EmployeeViewModel> Employees, int TotalCount)> _pageCache = new();
        private const int CachePagesAround = 3;
        private CancellationTokenSource? _cachingCts;

        public EmployeesPage()
        {
            this.InitializeComponent();
            Items = new ObservableCollection<EmployeeViewModel>();

            _employeeService = ServiceContainer.GetService<IEmployeeService>()
                ?? throw new InvalidOperationException("EmployeeService not registered");

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var searchTextSnapshot = _currentSearchText;
            var cacheKey = GetCacheKey(_currentPage, searchTextSnapshot);

            try
            {
                Logger.Info($"Loading employees (page {_currentPage}, search='{searchTextSnapshot}')");

                if (_pageCache.TryGetValue(cacheKey, out var cachedData))
                {
                    Logger.Info($"Using cached employees for page {_currentPage}");
                    _totalCount = cachedData.TotalCount;
                    _totalPages = Math.Max(1, (int)Math.Ceiling(_totalCount / (double)_pageSize));

                    Items.Clear();
                    foreach (var employee in cachedData.Employees.Select(CloneEmployeeViewModel))
                    {
                        Items.Add(employee);
                    }

                    UpdateUI();
                    _ = StartBackgroundCachingAsync(searchTextSnapshot);
                    return;
                }

                var result = await Task.Run(() =>
                    _employeeService.GetEmployeesFiltered(searchTextSnapshot, _currentPage, _pageSize));

                if (searchTextSnapshot != _currentSearchText)
                {
                    Logger.Info($"Search text changed from '{searchTextSnapshot}' to '{_currentSearchText}', reloading...");
                    await LoadDataAsync();
                    return;
                }

                if (result != null)
                {
                    _totalCount = result.Info.TotalRecords;
                    _totalPages = Math.Max(1, result.Info.TotalPages);

                    var employees = result.Employees?.ToList() ?? new List<EmployeeViewModel>();

                    Items.Clear();
                    foreach (var employee in employees)
                    {
                        Items.Add(employee);
                    }

                    _pageCache[cacheKey] = (employees.Select(CloneEmployeeViewModel).ToList(), _totalCount);

                    UpdateUI();

                    Logger.Info($"Loaded {Items.Count} employees (page {_currentPage}/{_totalPages}, total: {_totalCount})");

                    _ = StartBackgroundCachingAsync(searchTextSnapshot);
                }
                else
                {
                    Logger.Warning("Employee service returned null result");
                    Items.Clear();
                    _totalCount = 0;
                    _totalPages = 1;
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load employees", ex);
                Items.Clear();
                _totalCount = 0;
                _totalPages = 1;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            var count = Items.Count;

            RecordCountText.Text = count == 0
                ? "Khong co nhan vien"
                : count == 1
                    ? "Hien thi 1 nhan vien"
                    : $"Hien thi {count} nhan vien";

            PageInfoText.Text = $"Trang {_currentPage} / {Math.Max(1, _totalPages)}";
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;

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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open add employee dialog or navigate to add page
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender,
            AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var previousSearchText = _currentSearchText;
                _currentSearchText = sender.Text ?? string.Empty;
                _currentPage = 1;

                if (previousSearchText != _currentSearchText)
                {
                    ClearCache();
                    _cachingCts?.Cancel();
                }

                _searchTimer?.Dispose();
                _searchTimer = new Timer(
                    async _ =>
                    {
                        DispatcherQueue.TryEnqueue(async () =>
                        {
                            await LoadDataAsync();
                        });
                    },
                    null,
                    SearchDelayMs,
                    Timeout.Infinite);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show filter dialog (by status, hire date, etc.)
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchBox != null)
            {
                SearchBox.Text = string.Empty;
            }

            _currentSearchText = string.Empty;
            _currentPage = 1;

            ClearCache();
            _cachingCts?.Cancel();

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
                menuItem.Tag is EmployeeViewModel employee)
            {
                // TODO: Navigate to employee detail page or show detail dialog
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem &&
                menuItem.Tag is EmployeeViewModel employee)
            {
                // TODO: Open edit employee dialog or navigate to edit page
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem &&
                menuItem.Tag is EmployeeViewModel employee)
            {
                // TODO: Show confirmation dialog and delete employee
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

        private EmployeeViewModel CloneEmployeeViewModel(EmployeeViewModel source)
        {
            return new EmployeeViewModel
            {
                Id = source.Id,
                Code = source.Code,
                FullName = source.FullName,
                Phone = source.Phone,
                Email = source.Email,
                HireDate = source.HireDate,
                IsActive = source.IsActive
            };
        }

        private async Task StartBackgroundCachingAsync(string searchText)
        {
            _cachingCts?.Cancel();
            _cachingCts = new CancellationTokenSource();
            var token = _cachingCts.Token;

            try
            {
                var pagesToCache = new List<int>();
                for (int i = Math.Max(1, _currentPage - CachePagesAround);
                    i <= Math.Min(_totalPages, _currentPage + CachePagesAround);
                    i++)
                {
                    if (i != _currentPage)
                    {
                        pagesToCache.Add(i);
                    }
                }

                Logger.Info($"Starting background cache for pages: [{string.Join(", ", pagesToCache)}]");

                foreach (var pageIndex in pagesToCache)
                {
                    if (token.IsCancellationRequested)
                        break;

                    await CachePageAsync(pageIndex, searchText, token);
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

                if (_pageCache.ContainsKey(cacheKey))
                {
                    Logger.Info($"Page {pageIndex} already cached, skipping");
                    return;
                }

                var result = await Task.Run(() =>
                    _employeeService.GetEmployeesFiltered(searchText, pageIndex, _pageSize),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (result != null)
                {
                    var employees = result.Employees?.Select(CloneEmployeeViewModel).ToList() ?? new List<EmployeeViewModel>();
                    _pageCache[cacheKey] = (employees, result.Info.TotalRecords);
                    Logger.Info($"Cached page {pageIndex} with {employees.Count} employees");
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored - caching was cancelled
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to cache page {pageIndex}", ex);
            }
        }

        private string GetCacheKey(int pageIndex, string searchText)
        {
            return $"{pageIndex}_{searchText ?? string.Empty}".ToLowerInvariant();
        }

        private void ClearCache()
        {
            _pageCache.Clear();
            Logger.Info("Employee page cache cleared");
        }
    }
}
