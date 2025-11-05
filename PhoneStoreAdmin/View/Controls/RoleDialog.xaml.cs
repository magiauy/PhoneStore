using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreAdmin.Models;
using System;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class RoleDialog : ContentDialog
    {
        private readonly Role? _existingRole;
        private readonly int _minWeight;

        public RoleDialogResult? Result { get; private set; }

        public RoleDialog(Role? role, int minWeight)
        {
            _existingRole = role;
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
            Result = new RoleDialogResult(role);
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
        public RoleDialogResult(Role role)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
        }

        public Role Role { get; }
    }
}
