using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreAdmin.Models;
using System;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class AccountFilterDialog : ContentDialog
    {
        public AccountFilterCriteria FilterCriteria { get; private set; }
        public bool IsApplied { get; private set; }

        public AccountFilterDialog()
        {
            this.InitializeComponent();
            FilterCriteria = new AccountFilterCriteria();
            IsApplied = false;
            
            // Don't set default dates - let user choose
        }

        public void SetCurrentFilters(AccountFilterCriteria criteria)
        {
            if (criteria == null) return;

            // Status
            StatusComboBox.SelectedIndex = criteria.Status switch
            {
                "Activated" => 1,
                "Deactivated" => 2,
                _ => 0
            };

            // Account Type
            AccountTypeComboBox.SelectedIndex = criteria.AccountType switch
            {
                "Employee" => 1,
                "Customer" => 2,
                _ => 0
            };

            // Created Date Range
            if (criteria.CreatedFrom.HasValue)
                CreatedFromDatePicker.SelectedDate = criteria.CreatedFrom.Value;
            if (criteria.CreatedTo.HasValue)
                CreatedToDatePicker.SelectedDate = criteria.CreatedTo.Value;

            // Last Login
            if (criteria.NeverLoggedIn)
            {
                LastLoginFilterType.SelectedIndex = 1;
            }
            else
            {
                LastLoginFilterType.SelectedIndex = 0;
                if (criteria.LastLoginFrom.HasValue)
                    LastLoginFromDatePicker.SelectedDate = criteria.LastLoginFrom.Value;
                if (criteria.LastLoginTo.HasValue)
                    LastLoginToDatePicker.SelectedDate = criteria.LastLoginTo.Value;
            }

            UpdateActiveFiltersSummary();
        }

        private void LastLoginFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LastLoginDateRangePanel == null) return;

            var selectedRadio = LastLoginFilterType.SelectedItem as RadioButton;
            var tag = selectedRadio?.Tag?.ToString();

            // Show/hide date range based on selection
            LastLoginDateRangePanel.Visibility = tag == "DateRange" 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        private void ApplyButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FilterCriteria = new AccountFilterCriteria();

            // Status
            var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;
            FilterCriteria.Status = statusItem?.Tag?.ToString() ?? "All";

            // Account Type
            var accountTypeItem = AccountTypeComboBox.SelectedItem as ComboBoxItem;
            FilterCriteria.AccountType = accountTypeItem?.Tag?.ToString() ?? "All";

            // Created Date Range
            if (CreatedFromDatePicker.SelectedDate.HasValue)
                FilterCriteria.CreatedFrom = CreatedFromDatePicker.SelectedDate.Value.DateTime;
            if (CreatedToDatePicker.SelectedDate.HasValue)
                FilterCriteria.CreatedTo = CreatedToDatePicker.SelectedDate.Value.DateTime;

            // Last Login
            var selectedRadio = LastLoginFilterType.SelectedItem as RadioButton;
            var lastLoginFilterTag = selectedRadio?.Tag?.ToString();

            if (lastLoginFilterTag == "NeverLoggedIn")
            {
                FilterCriteria.NeverLoggedIn = true;
            }
            else
            {
                FilterCriteria.NeverLoggedIn = false;
                if (LastLoginFromDatePicker.SelectedDate.HasValue)
                    FilterCriteria.LastLoginFrom = LastLoginFromDatePicker.SelectedDate.Value.DateTime;
                if (LastLoginToDatePicker.SelectedDate.HasValue)
                    FilterCriteria.LastLoginTo = LastLoginToDatePicker.SelectedDate.Value.DateTime;
            }

            IsApplied = true;
        }

        private void ResetButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Reset all filters to default
            StatusComboBox.SelectedIndex = 0;
            AccountTypeComboBox.SelectedIndex = 0;
            CreatedFromDatePicker.SelectedDate = null;
            CreatedToDatePicker.SelectedDate = null;
            LastLoginFilterType.SelectedIndex = 0;
            LastLoginFromDatePicker.SelectedDate = null;
            LastLoginToDatePicker.SelectedDate = null;

            UpdateActiveFiltersSummary();

            // Don't close dialog on reset
            args.Cancel = true;
        }

        private void CancelButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            IsApplied = false;
        }

        private void UpdateActiveFiltersSummary()
        {
            var filters = new System.Collections.Generic.List<string>();

            // Status
            var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;
            if (statusItem?.Tag?.ToString() != "All")
                filters.Add($"Trạng thái: {statusItem?.Content}");

            // Account Type
            var accountTypeItem = AccountTypeComboBox.SelectedItem as ComboBoxItem;
            if (accountTypeItem?.Tag?.ToString() != "All")
                filters.Add($"Loại: {accountTypeItem?.Content}");

            // Created Date
            if (CreatedFromDatePicker.SelectedDate.HasValue || CreatedToDatePicker.SelectedDate.HasValue)
            {
                var from = CreatedFromDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "...";
                var to = CreatedToDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "...";
                filters.Add($"Ngày tạo: {from} - {to}");
            }

            // Last Login
            var selectedRadio = LastLoginFilterType.SelectedItem as RadioButton;
            if (selectedRadio?.Tag?.ToString() == "NeverLoggedIn")
            {
                filters.Add("Chưa đăng nhập bao giờ");
            }
            else if (LastLoginFromDatePicker.SelectedDate.HasValue || LastLoginToDatePicker.SelectedDate.HasValue)
            {
                var from = LastLoginFromDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "...";
                var to = LastLoginToDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "...";
                filters.Add($"Đăng nhập cuối: {from} - {to}");
            }

            ActiveFiltersText.Text = filters.Count > 0 
                ? string.Join(" • ", filters)
                : "Chưa có bộ lọc nào";
        }
    }
}
