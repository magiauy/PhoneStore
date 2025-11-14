using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreRepository.Models;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class CustomerFilterDialog : ContentDialog
    {
        public CustomerFilterCriteria FilterCriteria { get; private set; }
        public bool IsApplied { get; private set; }

        public CustomerFilterDialog()
        {
            this.InitializeComponent();
            FilterCriteria = new CustomerFilterCriteria();
            IsApplied = false;

            PrimaryButtonClick += CustomerFilterDialog_PrimaryButtonClick;
            SecondaryButtonClick += CustomerFilterDialog_SecondaryButtonClick;
            Closed += CustomerFilterDialog_Closed;

            StatusComboBox.SelectionChanged += (_, __) => UpdateActiveFiltersSummary();
            CreatedFromDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            CreatedToDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            PhonePrefixTextBox.TextChanged += (_, __) => UpdateActiveFiltersSummary();
            CityTextBox.TextChanged += (_, __) => UpdateActiveFiltersSummary();
            HasEmailToggle.Toggled += (_, __) => UpdateActiveFiltersSummary();
            HasAddressToggle.Toggled += (_, __) => UpdateActiveFiltersSummary();

            UpdateActiveFiltersSummary();
        }

        private void DatePicker_SelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
        {
            UpdateActiveFiltersSummary();
        }

        private void CustomerFilterDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            if (args.Result != ContentDialogResult.Primary)
            {
                IsApplied = false;
            }
        }

        private void CustomerFilterDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ResetFilters();
            args.Cancel = true;
        }

        private void CustomerFilterDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ApplyFilters();
        }

        public void SetCurrentFilters(CustomerFilterCriteria? criteria)
        {
            if (criteria == null)
            {
                ResetFilters();
                return;
            }

            // Status
            StatusComboBox.SelectedIndex = criteria.Status switch
            {
                "Active" => 1,
                "Inactive" => 2,
                _ => 0
            };

            // Created range
            CreatedFromDatePicker.SelectedDate = criteria.CreatedFrom;
            CreatedToDatePicker.SelectedDate = criteria.CreatedTo;

            // Phone prefix and city
            PhonePrefixTextBox.Text = criteria.PhonePrefix ?? string.Empty;
            CityTextBox.Text = criteria.City ?? string.Empty;

            // Toggles
            HasEmailToggle.IsOn = criteria.HasEmail ?? false;
            HasAddressToggle.IsOn = criteria.HasAddress ?? false;

            UpdateActiveFiltersSummary();
        }

        private void ApplyFilters()
        {
            FilterCriteria = new CustomerFilterCriteria
            {
                Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All",
                CreatedFrom = CreatedFromDatePicker.SelectedDate?.DateTime,
                CreatedTo = CreatedToDatePicker.SelectedDate?.DateTime,
                PhonePrefix = string.IsNullOrWhiteSpace(PhonePrefixTextBox.Text)
                    ? null
                    : PhonePrefixTextBox.Text.Trim(),
                City = string.IsNullOrWhiteSpace(CityTextBox.Text)
                    ? null
                    : CityTextBox.Text.Trim(),
                HasEmail = HasEmailToggle.IsOn ? true : (bool?)null,
                HasAddress = HasAddressToggle.IsOn ? true : (bool?)null
            };

            // For toggles, if off, we keep null to indicate no filter.
            if (!HasEmailToggle.IsOn)
            {
                FilterCriteria.HasEmail = null;
            }

            if (!HasAddressToggle.IsOn)
            {
                FilterCriteria.HasAddress = null;
            }

            IsApplied = true;
            UpdateActiveFiltersSummary();
        }

        private void ResetFilters()
        {
            StatusComboBox.SelectedIndex = 0;
            CreatedFromDatePicker.SelectedDate = null;
            CreatedToDatePicker.SelectedDate = null;
            PhonePrefixTextBox.Text = string.Empty;
            CityTextBox.Text = string.Empty;
            HasEmailToggle.IsOn = false;
            HasAddressToggle.IsOn = false;

            FilterCriteria = new CustomerFilterCriteria();
            UpdateActiveFiltersSummary();
        }

        private void UpdateActiveFiltersSummary()
        {
            var activeFilters = new List<string>();

            var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;
            if (statusItem?.Tag?.ToString() != "All")
            {
                activeFilters.Add($"Trạng thái: {statusItem?.Content}");
            }

            if (CreatedFromDatePicker.SelectedDate.HasValue || CreatedToDatePicker.SelectedDate.HasValue)
            {
                var from = CreatedFromDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "...";
                var to = CreatedToDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "...";
                activeFilters.Add($"Ngày tạo: {from} - {to}");
            }

            if (!string.IsNullOrWhiteSpace(PhonePrefixTextBox.Text))
            {
                activeFilters.Add($"Đầu số: {PhonePrefixTextBox.Text.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(CityTextBox.Text))
            {
                activeFilters.Add($"Thành phố: {CityTextBox.Text.Trim()}");
            }

            if (HasEmailToggle.IsOn)
            {
                activeFilters.Add("Có email");
            }

            if (HasAddressToggle.IsOn)
            {
                activeFilters.Add("Có địa chỉ");
            }

            ActiveFiltersText.Text = activeFilters.Count == 0
                ? "Chưa có bộ lọc nào"
                : string.Join(" • ", activeFilters);
        }
    }
}
