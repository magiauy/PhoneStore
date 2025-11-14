using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using PhoneStore.Services.Helpers;
using PhoneStoreRepository.Models;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.View.Controls;
using PhoneStore.Services.ViewModels;
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
        private EmployeeFilterCriteria _currentFilterCriteria = new();

        // Search debounce
        private Timer? _searchTimer;
        private const int SearchDelayMs = 300;

        // Page caching
        private readonly Dictionary<string, (List<EmployeeViewModel> Employees, int TotalCount)> _pageCache = new();
        private const int CachePagesAround = 3;
        private CancellationTokenSource? _cachingCts;
        private bool _isPageActive = true;

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
            if (!_isPageActive)
            {
                return;
            }

            var searchTextSnapshot = _currentSearchText;
            var filterSnapshot = _currentFilterCriteria?.Clone() ?? new EmployeeFilterCriteria();
            var cacheKey = GetCacheKey(_currentPage, searchTextSnapshot, filterSnapshot);

            try
            {
                if (!_isPageActive)
                {
                    return;
                }

                Logger.Info($"Loading employees (page {_currentPage}, search='{searchTextSnapshot}')");

                if (_pageCache.TryGetValue(cacheKey, out var cachedData))
                {
                    Logger.Info($"Using cached employees for page {_currentPage}");
                    _totalCount = cachedData.TotalCount;
                    _totalPages = Math.Max(1, (int)Math.Ceiling(_totalCount / (double)_pageSize));

                    if (!_isPageActive)
                    {
                        return;
                    }

                    Items.Clear();
                    foreach (var employee in cachedData.Employees.Select(CloneEmployeeViewModel))
                    {
                        Items.Add(employee);
                    }

                    UpdateUI();
                    _ = StartBackgroundCachingAsync(searchTextSnapshot, filterSnapshot);
                    return;
                }

                var result = await Task.Run(() =>
                    _employeeService.GetEmployeesFiltered(searchTextSnapshot, _currentPage, _pageSize, filterSnapshot));

                if (!_isPageActive)
                {
                    return;
                }

                if (searchTextSnapshot != _currentSearchText
                    || !_currentFilterCriteria.HasSameState(filterSnapshot))
                {
                    Logger.Info($"Search or filter changed, reloading...");
                    await LoadDataAsync();
                    return;
                }

                if (result != null)
                {
                    _totalCount = result.Info.TotalRecords;
                    _totalPages = Math.Max(1, result.Info.TotalPages);

                    var employees = result.Employees?.ToList() ?? new List<EmployeeViewModel>();

                    if (!_isPageActive)
                    {
                        return;
                    }

                    Items.Clear();
                    foreach (var employee in employees)
                    {
                        Items.Add(employee);
                    }

                    _pageCache[cacheKey] = (employees.Select(CloneEmployeeViewModel).ToList(), _totalCount);

                    UpdateUI();

                    Logger.Info($"Loaded {Items.Count} employees (page {_currentPage}/{_totalPages}, total: {_totalCount})");

                    _ = StartBackgroundCachingAsync(searchTextSnapshot, filterSnapshot);
                }
                else
                {
                    Logger.Warning("Employee service returned null result");
                    if (_isPageActive)
                    {
                        Items.Clear();
                        _totalCount = 0;
                        _totalPages = 1;
                        UpdateUI();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load employees", ex);
                if (_isPageActive)
                {
                    Items.Clear();
                    _totalCount = 0;
                    _totalPages = 1;
                    UpdateUI();
                }
            }
        }

        private void UpdateUI()
        {
            var count = Items.Count;

            var recordCountText = count switch
            {
                0 => LocalizationHelper.GetString("EmployeesPage_NoEmployees"),
                1 => LocalizationHelper.GetString("EmployeesPage_RecordCountSingle"),
                _ => string.Format(
                    LocalizationHelper.GetString("EmployeesPage_RecordCountMultiple"),
                    count)
            };

            RecordCountText.Text = recordCountText;

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

            UpdateFilterStatusUI();
        }

        private void UpdateFilterStatusUI()
        {
            if (FilterStatusBadge == null || FilterStatusText == null)
            {
                return;
            }

            if (_currentFilterCriteria.HasAnyFilter())
            {
                FilterStatusBadge.Visibility = Visibility.Visible;
                FilterStatusText.Text = _currentFilterCriteria.ToSummaryString();
            }
            else
            {
                FilterStatusBadge.Visibility = Visibility.Collapsed;
                FilterStatusText.Text = "Không áp dụng bộ lọc";
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowEmployeeDialogAsync(EmployeeDialog.DialogMode.Add, null);
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender,
            AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!_isPageActive)
            {
                return;
            }

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var previousSearchText = _currentSearchText;
                _currentSearchText = sender.Text ?? string.Empty;
                _currentPage = 1;

                if (previousSearchText != _currentSearchText)
                {
                    ClearCache();
                    AsyncResourceCleanupHelper.CancelAndDispose(ref _cachingCts);
                }

                AsyncResourceCleanupHelper.DisposeTimer(ref _searchTimer);
                _searchTimer = new Timer(
                    _ =>
                    {
                        if (!_isPageActive)
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
                            if (!_isPageActive)
                            {
                                return;
                            }

                            await LoadDataAsync();
                        }))
                        {
                            Logger.Warning("Unable to enqueue employee search refresh on dispatcher queue");
                        }
                    },
                    null,
                    SearchDelayMs,
                    Timeout.Infinite);
            }
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowFilterDialogAsync();
        }

        private async Task ShowFilterDialogAsync()
        {
            var dialog = new EmployeeFilterDialog(_currentFilterCriteria)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                _currentFilterCriteria = dialog.Criteria.Clone();
                _currentPage = 1;
                UpdateFilterStatusUI();
                ClearCache();
                AsyncResourceCleanupHelper.CancelAndDispose(ref _cachingCts);
                await LoadDataAsync();
            }
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
            AsyncResourceCleanupHelper.CancelAndDispose(ref _cachingCts);

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
                menuItem.Tag is EmployeeViewModel employee)
            {
                await ShowEmployeeDialogAsync(EmployeeDialog.DialogMode.View, employee);
            }
        }

        private async void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem &&
                menuItem.Tag is EmployeeViewModel employee)
            {
                await ShowEmployeeDialogAsync(EmployeeDialog.DialogMode.Edit, employee);
            }
        }

        private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem &&
                menuItem.Tag is EmployeeViewModel employee)
            {
                await DeleteEmployeeAsync(employee);
            }
        }

        private async Task ShowEmployeeDialogAsync(EmployeeDialog.DialogMode mode, EmployeeViewModel? employee)
        {
            var dialogControl = new EmployeeDialog();
            dialogControl.SetMode(mode, employee);

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Content = dialogControl
            };

            switch (mode)
            {
                case EmployeeDialog.DialogMode.Add:
                    dialog.Title = "Thêm nhân viên";
                    dialog.PrimaryButtonText = "Thêm";
                    dialog.CloseButtonText = "Hủy";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    dialog.PrimaryButtonClick += async (s, args) =>
                    {
                        args.Cancel = true;
                        dialogControl.HideError();

                        if (!dialogControl.ValidateInputs())
                        {
                            return;
                        }

                        dialogControl.SetLoading(true);

                        try
                        {
                            AsyncResourceCleanupHelper.CancelAndDispose(ref _cachingCts);
                            var employeeModel = dialogControl.BuildEmployee();
                            var result = await _employeeService.AddEmployeeAsync(employeeModel);
                            if (result == null)
                            {
                                dialogControl.ShowError("Không thể thêm nhân viên. Vui lòng thử lại.");
                                return;
                            }

                            Items.Insert(0, new EmployeeViewModel(result));
                            _currentPage = 1;
                            ClearCache();
                            await LoadDataAsync();
                            dialog.Hide();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Failed to add employee", ex);
                            dialogControl.ShowError("Đã xảy ra lỗi khi thêm nhân viên.");
                        }
                        finally
                        {
                            dialogControl.SetLoading(false);
                        }
                    };
                    break;

                case EmployeeDialog.DialogMode.Edit:
                    dialog.Title = "Chỉnh sửa nhân viên";
                    dialog.PrimaryButtonText = "Lưu";
                    dialog.CloseButtonText = "Hủy";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    dialog.PrimaryButtonClick += async (s, args) =>
                    {
                        args.Cancel = true;
                        dialogControl.HideError();

                        if (!dialogControl.ValidateInputs())
                        {
                            return;
                        }

                        dialogControl.SetLoading(true);

                        try
                        {
                            AsyncResourceCleanupHelper.CancelAndDispose(ref _cachingCts);
                            var employeeModel = dialogControl.BuildEmployee();
                            var success = await _employeeService.UpdateEmployeeAsync(employeeModel);
                            if (!success)
                            {
                                dialogControl.ShowError("Không thể cập nhật nhân viên.");
                                return;
                            }

                            if (employee != null)
                            {
                                var updatedVm = new EmployeeViewModel(employeeModel);
                                var index = Items.IndexOf(employee);
                                if (index >= 0)
                                {
                                    Items[index] = updatedVm;
                                }
                            }

                            ClearCache();
                            await LoadDataAsync();
                            dialog.Hide();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Failed to update employee", ex);
                            dialogControl.ShowError("Đã xảy ra lỗi khi cập nhật nhân viên.");
                        }
                        finally
                        {
                            dialogControl.SetLoading(false);
                        }
                    };
                    break;

                case EmployeeDialog.DialogMode.View:
                    dialog.Title = "Thông tin nhân viên";
                    dialog.CloseButtonText = "Đóng";
                    dialog.DefaultButton = ContentDialogButton.Close;
                    break;
            }

            await dialog.ShowAsync();
        }

        private async Task DeleteEmployeeAsync(EmployeeViewModel employee)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Xóa nhân viên",
                Content = $"Bạn có chắc chắn muốn xóa nhân viên {employee.FullName}?",
                PrimaryButtonText = "Xóa",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                AsyncResourceCleanupHelper.CancelAndDispose(ref _cachingCts);
                var success = await _employeeService.DeleteEmployeeAsync(employee.Id);
                if (!success)
                {
                    await ShowMessageDialogAsync("Không thể xóa", "Xóa nhân viên thất bại. Vui lòng thử lại.");
                    return;
                }

                Items.Remove(employee);

                if (_currentPage > 1 && Items.Count == 0)
                {
                    _currentPage--;
                }

                ClearCache();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete employee {employee.Id}", ex);
                await ShowMessageDialogAsync("Đã xảy ra lỗi", "Không thể xóa nhân viên. Vui lòng thử lại sau.");
            }
        }

        private async Task ShowMessageDialogAsync(string title, string message)
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

        private async Task StartBackgroundCachingAsync(string searchText, EmployeeFilterCriteria filterCriteria)
        {
            if (!_isPageActive)
            {
                return;
            }

            AsyncResourceCleanupHelper.CancelAndDispose(ref _cachingCts);
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
                    if (token.IsCancellationRequested || !_isPageActive)
                        break;

                    await CachePageAsync(pageIndex, searchText, filterCriteria, token);
                    if (!_isPageActive)
                    {
                        break;
                    }

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

        private async Task CachePageAsync(int pageIndex, string searchText, EmployeeFilterCriteria filterCriteria, CancellationToken cancellationToken)
        {
            try
            {
                if (!_isPageActive)
                {
                    return;
                }

                var cacheKey = GetCacheKey(pageIndex, searchText, filterCriteria);

                if (_pageCache.ContainsKey(cacheKey))
                {
                    Logger.Info($"Page {pageIndex} already cached, skipping");
                    return;
                }

                var result = await Task.Run(() =>
                    _employeeService.GetEmployeesFiltered(searchText, pageIndex, _pageSize, filterCriteria),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested || !_isPageActive)
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

        private string GetCacheKey(int pageIndex, string searchText, EmployeeFilterCriteria filterCriteria)
        {
            var filterKey = filterCriteria?.GetCacheKey() ?? "nofilter";
            return $"{pageIndex}_{searchText ?? string.Empty}_{filterKey}".ToLowerInvariant();
        }

        private void ClearCache()
        {
            _pageCache.Clear();
            Logger.Info("Employee page cache cleared");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _isPageActive = true;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _isPageActive = false;
            AsyncResourceCleanupHelper.CleanupDebounceAndCaching(ref _searchTimer, ref _cachingCts);
            ClearCache();
            base.OnNavigatedFrom(e);
        }
    }
}
