using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class EmployeeFilterDialog : ContentDialog
    {
        public EmployeeFilterCriteria Criteria { get; private set; }

        private bool _isInitialized;

        public EmployeeFilterDialog(EmployeeFilterCriteria? existingCriteria = null)
        {
            InitializeComponent();
            Criteria = existingCriteria?.Clone() ?? new EmployeeFilterCriteria();
            LoadCriteria();
            _isInitialized = true;
            UpdateSummary();
        }

        private void LoadCriteria()
        {
            _isInitialized = false;

            switch (Criteria.Status)
            {
                case "Active":
                    StatusComboBox.SelectedIndex = 1;
                    break;
                case "Inactive":
                    StatusComboBox.SelectedIndex = 2;
                    break;
                default:
                    StatusComboBox.SelectedIndex = 0;
                    break;
            }

            HireDateFromPicker.SelectedDate = Criteria.HireDateFrom.HasValue
                ? new DateTimeOffset(Criteria.HireDateFrom.Value)
                : (DateTimeOffset?)null;

            HireDateToPicker.SelectedDate = Criteria.HireDateTo.HasValue
                ? new DateTimeOffset(Criteria.HireDateTo.Value)
                : (DateTimeOffset?)null;

            HasEmailCheckBox.IsChecked = Criteria.HasEmail == true;
            HasPhoneCheckBox.IsChecked = Criteria.HasPhone == true;

            _isInitialized = true;
        }

        private void ApplyButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Criteria = BuildCriteriaFromInputs();
            UpdateSummary();
        }

        private void ResetButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            Criteria = new EmployeeFilterCriteria();
            LoadCriteria();
            UpdateSummary();
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            Criteria = BuildCriteriaFromInputs();
            UpdateSummary();
        }

        private void HireDatePicker_SelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
        {
            if (!_isInitialized)
            {
                return;
            }

            Criteria = BuildCriteriaFromInputs();
            UpdateSummary();
        }

        private void ContactCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            Criteria = BuildCriteriaFromInputs();
            UpdateSummary();
        }

        public EmployeeFilterCriteria BuildCriteriaFromInputs()
        {
            var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;
            var status = statusItem?.Tag?.ToString() ?? "All";

            DateTime? hireFrom = HireDateFromPicker.SelectedDate?.Date;
            DateTime? hireTo = HireDateToPicker.SelectedDate?.Date;

            if (hireFrom.HasValue && hireTo.HasValue && hireFrom > hireTo)
            {
                (hireFrom, hireTo) = (hireTo, hireFrom);
            }

            return new EmployeeFilterCriteria
            {
                Status = status,
                HireDateFrom = hireFrom,
                HireDateTo = hireTo,
                HasEmail = HasEmailCheckBox.IsChecked == true ? true : (bool?)null,
                HasPhone = HasPhoneCheckBox.IsChecked == true ? true : (bool?)null
            };
        }

        private void UpdateSummary()
        {
            SummaryTextBlock.Text = Criteria.ToSummaryString();
        }
    }
}
