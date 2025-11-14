using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStore.Services.Helpers;
using PhoneStoreRepository.Models;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStoreAdmin.View.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace PhoneStoreAdmin.View
{
    public sealed partial class RolesPage : Page
    {
        // ResourceLoader for localization
        private readonly ResourceLoader _resourceLoader = new();

        // ObservableCollection for binding
        public ObservableCollection<RoleViewModel> Roles { get; set; }
        public ObservableCollection<PermissionViewModel> Permissions { get; set; }

        // Services
        private readonly IRoleService? _roleService;
        private readonly IPermissionService? _permissionService;

        // Search debounce
        private Timer? _searchTimer;
        private const int SearchDelayMs = 300;

        // Selected role
        private Role? _selectedRole;
        private List<RoleViewModel> _allRoles = new();
        
        // All permissions (for reference)
        private List<Permission> _allPermissions = new();

        public RolesPage()
        {
            this.InitializeComponent();
            Roles = new ObservableCollection<RoleViewModel>();
            Permissions = new ObservableCollection<PermissionViewModel>();

            this.DataContext = this;

            // Get services from ServiceContainer
            _roleService = ServiceContainer.GetService<IRoleService>();
            _permissionService = ServiceContainer.GetService<IPermissionService>();

            // Initialize localized strings
            InitializeLocalizedStrings();
        }

        private void InitializeLocalizedStrings()
        {
            // Initialize any runtime string bindings if needed
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadRolesAsync();
            await LoadAllPermissionsAsync();
        }

        #region Data Loading

        private async Task LoadRolesAsync()
        {
            try
            {
                LoadingOverlay.Show();

                if (_roleService == null)
                {
                    Logger.Error("RoleService is not available");
                    return;
                }

                Logger.Info("Loading roles...");
                var roles = await _roleService.GetAllRolesAsync();
                Logger.Info($"Loaded {roles.Count} roles from service");
                
                _allRoles = roles.Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description ?? string.Empty,
                    Weight = r.Weight,
                    PermissionCount = r.RolePermissions?.Count ?? 0
                }).ToList();

                Logger.Info($"Created {_allRoles.Count} role view models");
                UpdateRolesList(_allRoles);
                Logger.Info($"Roles ObservableCollection now has {Roles.Count} items");

                // Show/hide empty state
                EmptyRolesState.Visibility = _allRoles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading roles: {ex.Message}");
                await ShowErrorDialogAsync("Lỗi tải dữ liệu", $"Không thể tải danh sách vai trò: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }

        private async Task LoadAllPermissionsAsync()
        {
            try
            {
                if (_permissionService == null)
                {
                    Logger.Error("PermissionService is not available");
                    return;
                }

                _allPermissions = (await _permissionService.GetAllPermissionsAsync()).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading permissions: {ex.Message}");
            }
        }

        private void UpdateRolesList(List<RoleViewModel> roles)
        {
            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
            }
        }

        #endregion

        #region Search

        private void RoleSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // Debounce search
                _searchTimer?.Dispose();
                _searchTimer = new Timer(_ =>
                {
                    DispatcherQueue.TryEnqueue(() => PerformSearch(sender.Text));
                }, null, SearchDelayMs, Timeout.Infinite);
            }
        }

        private void PerformSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                UpdateRolesList(_allRoles);
                EmptyRolesState.Visibility = _allRoles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            var filtered = _allRoles.Where(r =>
                r.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (r.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            UpdateRolesList(filtered);
            EmptyRolesState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Role Selection

        private async void RolesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RolesListView.SelectedItem is RoleViewModel selectedRoleVm)
            {
                await LoadRolePermissionsAsync(selectedRoleVm.Id);
            }
            else
            {
                ShowNoRoleSelectedState();
            }
        }

        private async Task LoadRolePermissionsAsync(int roleId)
        {
            PermissionsLoadingOverlay.Show();

            try
            {
                if (_allPermissions.Count == 0)
                {
                    await LoadAllPermissionsAsync();
                }

                if (_roleService == null)
                {
                    Logger.Error("RoleService is not available");
                    ShowNoRoleSelectedState();
                    return;
                }

                NoRoleSelectedPanel.Visibility = Visibility.Collapsed;
                PermissionsContentPanel.Visibility = Visibility.Visible;

                _selectedRole = await _roleService.GetRoleByIdAsync(roleId);
                
                if (_selectedRole == null)
                {
                    ShowNoRoleSelectedState();
                    return;
                }

                // Update header
                SelectedRoleName.Text = _selectedRole.Name;
                SelectedRoleDescription.Text = _selectedRole.Description ?? string.Empty;

                // Load permissions grouped by module
                await LoadPermissionsGroupedAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading role permissions: {ex.Message}");
                await ShowErrorDialogAsync("Lỗi", $"Không thể tải quyền của vai trò: {ex.Message}");
            }
            finally
            {
                PermissionsLoadingOverlay.Hide();
            }
        }

        private async Task LoadPermissionsGroupedAsync()
        {
            PermissionsContainer.Children.Clear();

            if (_selectedRole == null) return;

            var rolePermissionIds = _selectedRole.RolePermissions
                ?.Select(rp => rp.PermissionId)
                .ToHashSet() ?? new HashSet<int>();
            Logger.Info($"[RolePermissionIds] Role: '{_selectedRole.Name}' (ID: {_selectedRole.Id}) - Permission IDs: [{string.Join(", ", rolePermissionIds)}]");
            foreach (var permId in rolePermissionIds)
            {
                var perm = _allPermissions.FirstOrDefault(p => p.Id == permId);
                if (perm != null)
                {
                    Logger.Info($"    PermissionId: {permId}, Code: {perm.Code}, Description: {perm.Description}");
                }
                else
                {
                    Logger.Info($"    PermissionId: {permId} (not found in _allPermissions)");
                }
            }
            // Group permissions by module (extract from permission code)
            var permissionGroups = _allPermissions
                .GroupBy(p => GetModuleFromPermissionCode(p.Code))
                .OrderBy(g => g.Key)
                .ToList();

            for (int i = 0; i < permissionGroups.Count; i++)
            {
                var group = permissionGroups[i];

                // Create module section
                var moduleSection = CreateModuleSection(group.Key, group.ToList(), rolePermissionIds);
                PermissionsContainer.Children.Add(moduleSection);
                if (i < permissionGroups.Count - 1)
                {
                    var divider = new Rectangle
                    {
                        Height = 1,
                        Fill = (Brush)Application.Current.Resources["SystemControlForegroundBaseLowBrush"],
                        Opacity = 0.2,
                        Margin = new Thickness(0, 8, 0, 8)
                    };

                    PermissionsContainer.Children.Add(divider);
                }
            }
        }

        private string GetModuleFromPermissionCode(string code)
        {
            // Example: "PURCHASE_ORDERS_EDIT" -> "PURCHASE_ORDERS"
            var parts = code.Split('_');
            if (parts.Length >= 3)
                return $"{parts[0]}_{parts[1]}";
            return parts.Length > 0 ? parts[0] : "OTHER";
        }

        private StackPanel CreateModuleSection(string moduleName, List<Permission> permissions, HashSet<int> rolePermissionIds)
        {
            var section = new StackPanel
            {
                Spacing = 8,
                Margin = new Thickness(0, 0, 0, 12)
            };

            // Module header checkbox (parent)
            var moduleCheckbox = new CheckBox
            {
                Content = GetLocalizedModuleName(moduleName),
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Tag = $"MODULE_{moduleName}",
                Margin = new Thickness(0, 0, 0, 24)
            };

            // Check if all permissions in this module are granted
            var allGranted = permissions.All(p => rolePermissionIds.Contains(p.Id));
            var anyGranted = permissions.Any(p => rolePermissionIds.Contains(p.Id));
            
            moduleCheckbox.IsChecked = allGranted ? true : (anyGranted ? null : false);

            // Handle module checkbox click to toggle all children
            moduleCheckbox.Click += (s, e) =>
            {
                var isChecked = moduleCheckbox.IsChecked == true;
                
                // Find all child checkboxes in this section
                if (section.Children.Count > 1 && section.Children[1] is VariableSizedWrapGrid wrapGrid)
                {
                    foreach (var child in wrapGrid.Children)
                    {
                        if (child is CheckBox childCheckbox)
                        {
                            childCheckbox.IsChecked = isChecked;
                        }
                    }
                }
            };

            section.Children.Add(moduleCheckbox);

            // Create VariableSizedWrapGrid for child permissions (horizontal layout with wrapping)
            var wrapGrid = new VariableSizedWrapGrid
            {
                Orientation = Orientation.Horizontal,
                MaximumRowsOrColumns = 4, // Maximum 4 items per row
                ItemWidth = 200,
                ItemHeight = 20,
                Margin = new Thickness(24, 0, 0, 0) // Indent child permissions
            };

            // Permissions checkboxes (children)
            foreach (var permission in permissions)
            {
                Logger.Info($"Creating checkbox for permission: {permission.Code} (ID: {permission.Id})");
                Logger.Info($"Role has permission: {rolePermissionIds.Contains(permission.Id)}");
                Logger.Info($"RolePermissionIds for module '{moduleName}': [{string.Join(", ", rolePermissionIds)}]");
                var checkbox = new CheckBox
                {
                    Content = GetLocalizedPermissionName(permission.Code),
                    IsChecked = rolePermissionIds.Contains(permission.Id),
                    Tag = permission.Id,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Update parent checkbox state when child changes
                checkbox.Click += (s, e) =>
                {
                    UpdateModuleCheckboxState(moduleCheckbox, wrapGrid, permissions, rolePermissionIds);
                };

                wrapGrid.Children.Add(checkbox);
            }

            section.Children.Add(wrapGrid);

            return section;
        }

        private void UpdateModuleCheckboxState(CheckBox moduleCheckbox, VariableSizedWrapGrid wrapGrid, List<Permission> permissions, HashSet<int> rolePermissionIds)
        {
            int checkedCount = 0;
            int totalCount = 0;

            foreach (var child in wrapGrid.Children)
            {
                if (child is CheckBox checkbox)
                {
                    totalCount++;
                    if (checkbox.IsChecked == true)
                    {
                        checkedCount++;
                    }
                }
            }

            if (checkedCount == 0)
            {
                moduleCheckbox.IsChecked = false;
            }
            else if (checkedCount == totalCount)
            {
                moduleCheckbox.IsChecked = true;
            }
            else
            {
                moduleCheckbox.IsChecked = null; // Indeterminate state
            }
        }

        private string GetLocalizedModuleName(string module)
        {
            // Map module names to localized text
            return module switch
            {
                "PRODUCT" => "Sản phẩm",
                "ACCOUNT" => "Tài khoản",
                "ORDER" => "Đơn hàng",
                "INVOICE" => "Hóa đơn",
                "CUSTOMER" => "Khách hàng",
                "SUPPLIER" => "Nhà cung cấp",
                "REPORT" => "Báo cáo",
                "SETTING" => "Cài đặt",
                _ => module
            };
        }

        private string GetLocalizedPermissionName(string code)
        {
            // Use Permission resource file for localization
            return LocalizationHelper.GetPermissionString(code);
        }

        private void ShowNoRoleSelectedState()
        {
            NoRoleSelectedPanel.Visibility = Visibility.Visible;
            PermissionsContentPanel.Visibility = Visibility.Collapsed;
            _selectedRole = null;
        }

        #endregion

        #region CRUD Operations

        private async void CreateRoleButton_Click(object sender, RoutedEventArgs e)
        {
            var currentUserRole = UserSession.Instance.Role;
            int minWeight = currentUserRole?.Weight ?? 0;

            var dialog = new RoleDialog(null, minWeight)
            {
                XamlRoot = this.XamlRoot,
                Title = "Thêm vai trò mới",
                PrimaryButtonText = "Tạo",
                SecondaryButtonText = "Hủy"
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                await CreateRoleAsync(dialog.Result);
            }
        }

        private async Task CreateRoleAsync(RoleDialogResult dialogResult)
        {
            try
            {
                if (_roleService == null)
                {
                    Logger.Error("RoleService is not available");
                    return;
                }

                LoadingOverlay.Show();

                var createdRole = await _roleService.CreateRoleAsync(dialogResult.Role);

                await LoadRolesAsync();

                // Select the newly created role to display permissions immediately
                var createdRoleVm = Roles.FirstOrDefault(r => r.Id == createdRole.Id);
                if (createdRoleVm != null)
                {
                    RolesListView.SelectedItem = createdRoleVm;
                    await LoadRolePermissionsAsync(createdRoleVm.Id);
                }

                await ShowSuccessDialogAsync("Thành công", "Vai trò đã được tạo thành công");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating role: {ex.Message}");
                await ShowErrorDialogAsync("Lỗi", $"Không thể tạo vai trò: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }

        private async void EditRoleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;

            var currentUserRole = UserSession.Instance.Role;
            int minWeight = currentUserRole?.Weight ?? 0;

            var dialog = new RoleDialog(_selectedRole, minWeight)
            {
                XamlRoot = this.XamlRoot,
                Title = "Chỉnh sửa vai trò",
                PrimaryButtonText = "Lưu",
                SecondaryButtonText = "Hủy"
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.Result != null)
            {
                await UpdateRoleAsync(dialog.Result);
            }
        }

        private async Task UpdateRoleAsync(RoleDialogResult dialogResult)
        {
            try
            {
                if (_roleService == null)
                {
                    Logger.Error("RoleService is not available");
                    return;
                }

                LoadingOverlay.Show();

                await _roleService.UpdateRoleAsync(dialogResult.Role);

                await LoadRolesAsync();

                var updatedRoleVm = Roles.FirstOrDefault(r => r.Id == dialogResult.Role.Id);
                if (updatedRoleVm != null)
                {
                    RolesListView.SelectedItem = updatedRoleVm;
                    await LoadRolePermissionsAsync(updatedRoleVm.Id);
                }

                await ShowSuccessDialogAsync("Thành công", "Vai trò đã được cập nhật");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating role: {ex.Message}");
                await ShowErrorDialogAsync("Lỗi", $"Không thể cập nhật vai trò: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }

        private async void DeleteRoleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;

            // Store local reference to prevent race condition
            var selectedRole = _selectedRole;

            var confirmDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Xác nhận xóa",
                Content = $"Bạn có chắc chắn muốn xóa vai trò '{selectedRole.Name}'?",
                PrimaryButtonText = "Xóa",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await DeleteRoleAsync(selectedRole.Id);
            }
        }

        private async Task DeleteRoleAsync(int roleId)
        {
            try
            {
                if (_roleService == null)
                {
                    Logger.Error("RoleService is not available");
                    return;
                }

                LoadingOverlay.Show();

                await _roleService.DeleteRoleAsync(roleId);
                await LoadRolesAsync();

                ShowNoRoleSelectedState();
                RolesListView.SelectedItem = null;

                await ShowSuccessDialogAsync("Thành công", "Vai trò đã được xóa");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting role: {ex.Message}");
                await ShowErrorDialogAsync("Lỗi", $"Không thể xóa vai trò: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }

        #endregion

        #region Save Permissions

        private async void SavePermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRole == null) return;

            // Store local reference to prevent race condition
            var selectedRole = _selectedRole;

            try
            {
                LoadingOverlay.Show();

                // Collect selected permission IDs from checkboxes
                var selectedPermissionIds = new List<int>();

                foreach (var child in PermissionsContainer.Children)
                {
                    if (child is StackPanel section)
                    {
                        // Skip the module checkbox (index 0), get the VariableSizedWrapGrid (index 1)
                        if (section.Children.Count > 1 && section.Children[1] is VariableSizedWrapGrid wrapGrid)
                        {
                            foreach (var gridChild in wrapGrid.Children)
                            {
                                if (gridChild is CheckBox checkbox && checkbox.IsChecked == true && checkbox.Tag is int permId)
                                {
                                    selectedPermissionIds.Add(permId);
                                    Logger.Info($"Selected Permission: {checkbox.Content}, ID: {permId}");
                                }
                            }
                        }
                    }
                }

                if (_roleService == null)
                {
                    Logger.Error("RoleService is not available");
                    return;
                }

                if(selectedPermissionIds.Count == 0)
                {
                    var confirmDialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Title = "Xác nhận",
                        Content = "Bạn có chắc chắn muốn lưu mà không có quyền nào được cấp không?",
                        PrimaryButtonText = "Lưu",
                        CloseButtonText = "Hủy",
                        DefaultButton = ContentDialogButton.Close
                    };

                    var confirmResult = await confirmDialog.ShowAsync();
                    if (confirmResult != ContentDialogResult.Primary)
                    {
                        return; // User cancelled
                    }
                }

                // Update role permissions
                var updateSucceeded = await _roleService.UpdateRolePermissionsAsync(selectedRole.Id, selectedPermissionIds);
                if (!updateSucceeded)
                {
                    await ShowErrorDialogAsync("Lỗi", "Không thể lưu quyền cho vai trò. Vui lòng thử lại.");
                    return;
                }

                // Reload roles and selected role
                await LoadRolesAsync();
                
                // Only reload if the role still exists
                if (_selectedRole != null && _selectedRole.Id == selectedRole.Id)
                {
                    await LoadRolePermissionsAsync(selectedRole.Id);
                }

                await ShowSuccessDialogAsync("Thành công", "Quyền của vai trò đã được cập nhật");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving permissions: {ex.Message}");
                await ShowErrorDialogAsync("Lỗi", $"Không thể lưu quyền: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }

        #endregion

        #region Refresh

        private async void RefreshRolesButton_Click(object sender, RoutedEventArgs e)
        {
            RoleSearchBox.Text = string.Empty;
            RolesListView.SelectedItem = null;
            ShowNoRoleSelectedState();
            await LoadRolesAsync();
            await LoadAllPermissionsAsync();
        }

        #endregion

        #region Helper Methods

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = "Đóng"
            };

            await dialog.ShowAsync();
        }

        private async Task ShowSuccessDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };

            await dialog.ShowAsync();
        }

        #endregion
    }

    #region ViewModels

    public class RoleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Weight { get; set; }
        public int PermissionCount { get; set; }
    }

    public class PermissionViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsGranted { get; set; }
    }

    #endregion
}
