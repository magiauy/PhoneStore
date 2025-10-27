using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class PromotionDialog : ContentControl
    {
        private readonly IPromotionService _promotionService;
        private readonly ResourceLoader _resourceLoader;
        private bool _isNameValid = false;
        private bool _isDescriptionValid = true; // Optional field
        private bool _isDatesValid = false;

        public event EventHandler<PromotionViewModel>? PromotionSaved;
        public event EventHandler? DialogClosed;

        public enum DialogMode { Add, Edit, View }

        private DialogMode _currentMode = DialogMode.Add;

        public PromotionDialog()
        {
            this.InitializeComponent();
            _promotionService = App.GetService<IPromotionService>();
            _resourceLoader = new ResourceLoader();
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            ResetValidationStates();
            HideAllErrors();

            // Set default dates for new promotions
            if (_currentMode == DialogMode.Add)
            {
                PromotionStartDatePicker.SelectedDate = new DateTimeOffset(DateTime.Now);
                PromotionEndDatePicker.SelectedDate = new DateTimeOffset(DateTime.Now.AddDays(30));
            }
        }

        #region Field Validation Events

        private void PromotionNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = PromotionNameTextBox?.Text ?? string.Empty;
            ValidateName(text);
        }

        private void PromotionDescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = PromotionDescriptionTextBox?.Text ?? string.Empty;
            ValidateDescription(text);
        }

        private void PromotionStartDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            ValidateDates();
        }

        private void PromotionEndDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            ValidateDates();
        }

        #endregion

        #region Validation Logic

        private void ValidateName(string name)
        {
            _isNameValid = !string.IsNullOrWhiteSpace(name) && name.Length >= 2;

            if (!_isNameValid)
            {
                ShowError(PromotionNameError, _resourceLoader.GetString("NameRequired/Text") ?? "Name is required (min 2 characters)");
            }
            else
            {
                HideError(PromotionNameError);
            }
        }

        private void ValidateDescription(string description)
        {
            // Description is optional, but if provided, max 255 chars
            _isDescriptionValid = string.IsNullOrEmpty(description) || description.Length <= 255;

            if (!_isDescriptionValid)
            {
                ShowError(PromotionDescriptionError, "Description must be 255 characters or less");
            }
            else
            {
                HideError(PromotionDescriptionError);
            }
        }

        private void ValidateDates()
        {
            DateTime? start = null;
            var dateOffsetFrom = PromotionStartDatePicker?.Date;
            if (dateOffsetFrom.HasValue && dateOffsetFrom.Value.Year > 1900)
                start = dateOffsetFrom.Value.DateTime.Date;

            DateTime? end = null;
            var dateOffsetTo = PromotionEndDatePicker?.Date;
            if (dateOffsetTo.HasValue && dateOffsetTo.Value.Year > 1900)
                end = dateOffsetTo.Value.DateTime.Date.AddDays(1).AddSeconds(-1);

            HideError(StartDateError);
            HideError(EndDateError);
            HideError(DateValidationError);

            if (!start.HasValue)
            {
                ShowError(StartDateError, "Start date is required");
                _isDatesValid = false;
                return;
            }

            if (!end.HasValue)
            {
                ShowError(EndDateError, "End date is required");
                _isDatesValid = false;
                return;
            }

            if (_currentMode == DialogMode.Add && start.Value.Date < DateTime.Now.Date)
            {
                ShowError(StartDateError, "Start date must be today or in the future");
                _isDatesValid = false;
                return;
            }

            if (end.Value <= start.Value)
            {
                ShowError(DateValidationError, "End date must be after start date");
                _isDatesValid = false;
                return;
            }

            _isDatesValid = true;
        }

        #endregion

        #region UI Helper Methods

        private void ShowError(TextBlock errorTextBlock, string message)
        {
            if (errorTextBlock != null)
            {
                errorTextBlock.Text = message;
                errorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void HideError(TextBlock errorTextBlock)
        {
            if (errorTextBlock != null)
                errorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void HideAllErrors()
        {
            HideError(PromotionNameError);
            HideError(PromotionDescriptionError);
            HideError(StartDateError);
            HideError(EndDateError);
            HideError(DateValidationError);
            if (ErrorInfoBar != null)
                ErrorInfoBar.IsOpen = false;
        }

        private void ResetValidationStates()
        {
            _isNameValid = false;
            _isDescriptionValid = true; // Optional field
            _isDatesValid = false;
        }

        private void ShowLoading(bool isLoading)
        {
            if (LoadingOverlay != null)
                LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            bool isEditable = !isLoading && _currentMode != DialogMode.View;

            if (PromotionNameTextBox != null)
                PromotionNameTextBox.IsEnabled = isEditable;
            if (PromotionDescriptionTextBox != null)
                PromotionDescriptionTextBox.IsEnabled = isEditable;
            if (PromotionStartDatePicker != null)
                PromotionStartDatePicker.IsEnabled = isEditable;
            if (PromotionEndDatePicker != null)
                PromotionEndDatePicker.IsEnabled = isEditable;
            if (PromotionIsActiveToggle != null)
                PromotionIsActiveToggle.IsEnabled = isEditable;
        }

        #endregion

        #region Public Methods

        public void SetMode(DialogMode mode, PromotionViewModel? promotion = null)
        {
            _currentMode = mode;
            LoadData(promotion);
            UpdateUIForMode();
        }

        private void LoadData(PromotionViewModel? promotion)
        {
            if (promotion == null || _currentMode == DialogMode.Add)
            {
                if (PromotionIdTextBox != null) PromotionIdTextBox.Text = "";
                if (PromotionNameTextBox != null) PromotionNameTextBox.Text = "";
                if (PromotionDescriptionTextBox != null) PromotionDescriptionTextBox.Text = "";
                if (PromotionStartDatePicker != null) PromotionStartDatePicker.SelectedDate = new DateTimeOffset(DateTime.Now);
                if (PromotionEndDatePicker != null) PromotionEndDatePicker.SelectedDate = new DateTimeOffset(DateTime.Now.AddDays(30));
                if (PromotionIsActiveToggle != null) PromotionIsActiveToggle.IsOn = true;
                return;
            }

            if (PromotionIdTextBox != null) PromotionIdTextBox.Text = promotion.Id.ToString();
            if (PromotionNameTextBox != null) PromotionNameTextBox.Text = promotion.Name;
            if (PromotionDescriptionTextBox != null) PromotionDescriptionTextBox.Text = promotion.Description ?? "";
            if (PromotionStartDatePicker != null) PromotionStartDatePicker.SelectedDate = new DateTimeOffset(promotion.StartDate);
            if (PromotionEndDatePicker != null) PromotionEndDatePicker.SelectedDate = new DateTimeOffset(promotion.EndDate);
            if (PromotionIsActiveToggle != null) PromotionIsActiveToggle.IsOn = promotion.IsActive;

            // Validate loaded data
            ValidateName(promotion.Name);
            ValidateDescription(promotion.Description ?? "");
            ValidateDates();
        }

        private void UpdateUIForMode()
        {
            bool isEditable = _currentMode != DialogMode.View;

            if (PromotionIdTextBox != null) PromotionIdTextBox.IsReadOnly = true;
            if (PromotionNameTextBox != null) PromotionNameTextBox.IsReadOnly = !isEditable;
            if (PromotionDescriptionTextBox != null) PromotionDescriptionTextBox.IsReadOnly = !isEditable;
            if (PromotionStartDatePicker != null) PromotionStartDatePicker.IsEnabled = isEditable;
            if (PromotionEndDatePicker != null) PromotionEndDatePicker.IsEnabled = isEditable;
            if (PromotionIsActiveToggle != null) PromotionIsActiveToggle.IsEnabled = isEditable;
        }

        public void Save()
        {
            if (_currentMode == DialogMode.View)
            {
                DialogClosed?.Invoke(this, EventArgs.Empty);
                return;
            }

            ValidateAllFields();

            bool isFormValid = _isNameValid && _isDescriptionValid && _isDatesValid;
            if (!isFormValid)
                return;

            try
            {
                ShowLoading(true);
                HideAllErrors();

                DateTime startDate = DateTime.Now;
                var dateOffsetFrom = PromotionStartDatePicker?.Date;
                if (dateOffsetFrom.HasValue && dateOffsetFrom.Value.Year > 1900)
                    startDate = dateOffsetFrom.Value.DateTime.Date;

                DateTime endDate = DateTime.Now.AddDays(30);
                var dateOffsetTo = PromotionEndDatePicker?.Date;
                if (dateOffsetTo.HasValue && dateOffsetTo.Value.Year > 1900)
                    endDate = dateOffsetTo.Value.DateTime.Date.AddDays(1).AddSeconds(-1);

                var promotion = new Promotion
                {
                    Id = int.TryParse(PromotionIdTextBox?.Text, out int id) ? id : 0,
                    Name = PromotionNameTextBox?.Text ?? string.Empty,
                    Description = string.IsNullOrWhiteSpace(PromotionDescriptionTextBox?.Text)
                        ? null : PromotionDescriptionTextBox?.Text,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsActive = PromotionIsActiveToggle?.IsOn ?? true
                };

                if (_currentMode == DialogMode.Add)
                    _promotionService.Insert(promotion);
                else
                    _promotionService.Update(promotion);

                var viewModel = new PromotionViewModel(promotion);
                PromotionSaved?.Invoke(this, viewModel);

                DialogClosed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                if (ErrorInfoBar != null)
                {
                    ErrorInfoBar.Message = ex.Message;
                    ErrorInfoBar.IsOpen = true;
                }
                throw;
            }
            finally
            {
                ShowLoading(false);
            }
        }

        public bool IsValid()
        {
            ValidateAllFields();
            return _isNameValid && _isDescriptionValid && _isDatesValid;
        }

        public void Cancel()
        {
            ResetValidationStates();
            HideAllErrors();
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

        private void ValidateAllFields()
        {
            ValidateName(PromotionNameTextBox?.Text ?? string.Empty);
            ValidateDescription(PromotionDescriptionTextBox?.Text ?? string.Empty);
            ValidateDates();
        }

        #endregion
    }
}
