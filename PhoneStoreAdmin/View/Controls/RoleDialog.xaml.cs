using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreAdmin.Helpers;
using PhoneStoreAdmin.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class RoleDialog : ContentDialog
    {
        private readonly Role? _existingRole;
        private readonly List<Permission> _allPermissions;
        private readonly int _minWeight;
        private readonly Dictionary<int, CheckBox> _permissionCheckboxes = new();

        public RoleDialogResult? Result { get; private set; }

        public RoleDialog(Role? role, IEnumerable<Permission>? permissions, int minWeight)
        {
            _existingRole = role;
            _allPermissions = permissions?.OrderBy(p => p.Code).ToList() ?? new List<Permission>();
            _minWeight = minWeight;

            this.InitializeComponent();
            this.Loaded += RoleDialog_Loaded;
        }

        private void RoleDialog_Loaded(object sender, RoutedEventArgs e)
        {
            NameTextBox.Text = _existingRole?.Name ?? string.Empty;
            DescriptionTextBox.Text = _existingRole?.Description ?? string.Empty;

            var minimumWeight = Math.Max(1, _minWeight + 1);
            WeightNumberBox.Minimum = minimumWeight;
            WeightNumberBox.Maximum = 1000;
            WeightNumberBox.Value = _existingRole?.Weight > _minWeight
                ? _existingRole.Weight
                : minimumWeight;

            WeightInfoText.Text = _minWeight > 0
                ? $"Trọng số phải lớn hơn {_minWeight} (trọng số vai trò của bạn)"
                : "Chọn trọng số cho vai trò.";

            RenderPermissions();
        }

        private void RenderPermissions()
        {
            PermissionsContainer.Children.Clear();
            _permissionCheckboxes.Clear();

            if (_allPermissions.Count == 0)
            {
                EmptyPermissionsTextBlock.Visibility = Visibility.Visible;
                return;
            }

            EmptyPermissionsTextBlock.Visibility = Visibility.Collapsed;

            var selectedPermissions = _existingRole?.RolePermissions?
                .Select(rp => rp.PermissionId)
                .ToHashSet() ?? new HashSet<int>();

            var permissionGroups = _allPermissions
                .GroupBy(p => GetModuleFromPermissionCode(p.Code))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in permissionGroups)
            {
                var section = new StackPanel
                {
                    Spacing = 8
                };

                var moduleCheckbox = new CheckBox
                {
                    Content = GetLocalizedModuleName(group.Key),
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    IsThreeState = true
                };

                var wrapGrid = new VariableSizedWrapGrid
                {
                    Orientation = Orientation.Horizontal,
                    ItemWidth = 220,
                    ItemHeight = 24,
                    Margin = new Thickness(24, 0, 0, 0)
                };

                moduleCheckbox.Click += (s, _) =>
                {
                    var moduleChecked = moduleCheckbox.IsChecked == true;
                    foreach (var child in wrapGrid.Children)
                    {
                        if (child is CheckBox childCheckbox)
                        {
                            childCheckbox.IsChecked = moduleChecked;
                        }
                    }
                };

                foreach (var permission in group)
                {
                    var checkbox = new CheckBox
                    {
                        Content = GetLocalizedPermissionName(permission.Code),
                        IsChecked = selectedPermissions.Contains(permission.Id),
                        Tag = permission.Id,
                        FontSize = 14
                    };

                    checkbox.Click += (s, _) => UpdateModuleCheckboxState(moduleCheckbox, wrapGrid);

                    wrapGrid.Children.Add(checkbox);
                    _permissionCheckboxes[permission.Id] = checkbox;
                }

                UpdateModuleCheckboxState(moduleCheckbox, wrapGrid);

                section.Children.Add(moduleCheckbox);
                section.Children.Add(wrapGrid);
                PermissionsContainer.Children.Add(section);
            }
        }

        private void UpdateModuleCheckboxState(CheckBox moduleCheckbox, VariableSizedWrapGrid wrapGrid)
        {
            int total = 0;
            int selected = 0;

            foreach (var child in wrapGrid.Children)
            {
                if (child is CheckBox checkbox)
                {
                    total++;
                    if (checkbox.IsChecked == true)
                    {
                        selected++;
                    }
                }
            }

            if (selected == 0)
            {
                moduleCheckbox.IsChecked = false;
            }
            else if (selected == total)
            {
                moduleCheckbox.IsChecked = true;
            }
            else
            {
                moduleCheckbox.IsChecked = null;
            }
        }

        private string GetModuleFromPermissionCode(string code)
        {
            var parts = code.Split('_');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}_{parts[1]}";
            }

            return parts.Length > 0 ? parts[0] : "OTHER";
        }

        private string GetLocalizedModuleName(string module)
        {
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
            return LocalizationHelper.GetPermissionString(code);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            var validationError = ValidateInputs();
            if (!string.IsNullOrEmpty(validationError))
            {
                ErrorTextBlock.Text = validationError;
                ErrorTextBlock.Visibility = Visibility.Visible;
                args.Cancel = true;
                return;
            }

            var selectedPermissionIds = _permissionCheckboxes
                .Where(kv => kv.Value.IsChecked == true)
                .Select(kv => kv.Key)
                .ToList();

            var role = _existingRole != null
                ? new Role
                {
                    Id = _existingRole.Id
                }
                : new Role();

            role.Name = NameTextBox.Text.Trim();
            role.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                ? null
                : DescriptionTextBox.Text.Trim();
            role.Weight = (int)Math.Round(WeightNumberBox.Value);
            role.RolePermissions = selectedPermissionIds
                .Select(id => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = id
                })
                .ToList();

            Result = new RoleDialogResult(role, selectedPermissionIds);
        }

        private string? ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                return "Tên vai trò không được để trống.";
            }

            var weight = WeightNumberBox.Value;
            if (double.IsNaN(weight))
            {
                return "Vui lòng nhập trọng số hợp lệ.";
            }

            if (weight <= _minWeight)
            {
                return $"Trọng số phải lớn hơn {_minWeight}.";
            }

            return null;
        }
    }

    public sealed class RoleDialogResult
    {
        public RoleDialogResult(Role role, List<int> selectedPermissionIds)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            SelectedPermissionIds = selectedPermissionIds ?? new List<int>();
        }

        public Role Role { get; }
        public List<int> SelectedPermissionIds { get; }
    }
}
