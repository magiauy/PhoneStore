using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.ViewModels;
using PhoneStoreAdmin.Utils;
using Microsoft.Windows.ApplicationModel.Resources;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class PromotionCodeDialog : ContentControl
    {
        private readonly IPromotionCodeService _promotionCodeService;
        private readonly IPromotionService _promotionService;
        private readonly ResourceLoader _resourceLoader;
        private bool _isCodeValid = false;
        private bool _isPromotionValid = false;
        private bool _isDiscountAmountValid = false;
        private bool _isMinimumAmountValid = false;
        private bool _isInitializing = true;
        private PromotionCodeViewModel? _pendingPromotionCodeData = null;
        private DialogMode _pendingMode = DialogMode.Add;

        public event EventHandler<PromotionCodeViewModel>? PromotionCodeSaved;
        public event EventHandler? DialogClosed;

        public enum DialogMode { Add, Edit, View }

        private DialogMode _currentMode = DialogMode.Add;

        public PromotionCodeDialog()
        {
            Logger.Info("=== PromotionCodeDialog Constructor Started ===");
            try
            {
                this.InitializeComponent();
                Logger.Info("InitializeComponent completed");
                
                _promotionCodeService = App.GetService<IPromotionCodeService>();
                _promotionService = App.GetService<IPromotionService>();
                _resourceLoader = new ResourceLoader();
                Logger.Info("Services initialized");
                
                this.Loaded += PromotionCodeDialog_Loaded;
                this.Unloaded += PromotionCodeDialog_Unloaded;
                
                Logger.Info("PromotionCodeDialog constructor completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed in PromotionCodeDialog constructor", ex);
                throw;
            }
        }

        private void PromotionCodeDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("PromotionCodeDialog_Loaded event fired");
            try
            {
                InitializeDialog();
                
                // Apply pending mode and data if they exist
                if (_pendingPromotionCodeData != null || _pendingMode != DialogMode.Add)
                {
                    Logger.Info("Applying pending mode and data after dialog loaded");
                    ApplyModeAndData();
                }
                
                Logger.Info("PromotionCodeDialog_Loaded completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed in PromotionCodeDialog_Loaded", ex);
            }
        }

        private void PromotionCodeDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            _pendingPromotionCodeData = null;
        }

        private void InitializeDialog()
        {
            // Set localized toggle switch content
            IsActiveToggleSwitch.OnContent = _resourceLoader.GetString("Status_Active");
            IsActiveToggleSwitch.OffContent = _resourceLoader.GetString("Status_Inactive");
            
            // Only do basic initialization if no pending data
            if (_pendingPromotionCodeData == null && _pendingMode == DialogMode.Add)
            {
                ResetValidationStates();
                HideAllErrors();
                LoadPromotions();
                _isInitializing = false;
            }
        }

        private void LoadPromotions()
        {
            Logger.Info("LoadPromotions method started in PromotionCodeDialog");
            try
            {
                Logger.Info("Getting promotion service and fetching promotions...");
                
                // In Edit mode or when a preset PromotionId is provided (e.g., from PromotionEditPage), load all promotions
                // Otherwise in Add mode, only load active promotions
                bool hasPresetPromotion = (_pendingPromotionCodeData?.PromotionId ?? 0) > 0;
                var promotions = (_currentMode == DialogMode.Edit || hasPresetPromotion)
                    ? _promotionService.GetAll().ToList()
                    : _promotionService.GetAll().Where(p => p.IsActive).ToList();
                    
                Logger.Info($"Loaded {promotions.Count} promotions for dialog (Mode: {_currentMode})");
                
                var promotionViewModels = promotions.Select(p => new PromotionViewModel(p)).ToList();
                Logger.Info($"Created {promotionViewModels.Count} PromotionViewModel objects");

                Logger.Info("About to set PromotionComboBox.ItemsSource...");
                if (PromotionComboBox == null)
                {
                    Logger.Error("PromotionComboBox is null!");
                    return;
                }
                
                // assign a collection - ComboBox will work with any IEnumerable
                PromotionComboBox.ItemsSource = promotionViewModels;
                Logger.Info("Successfully set PromotionComboBox.ItemsSource");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load promotions in PromotionCodeDialog", ex);
                ShowError(PromotionError, _resourceLoader.GetString("LoadPromotionsError"));
            }
        }

        private void LoadPromotionCodeData(int promotionCodeId)
        {
            try
            {
                var promotionCode = _promotionCodeService.GetPromotionCodeById(promotionCodeId);
                if (promotionCode != null)
                {
                    PromotionCodeIdTextBox.Text = promotionCode.Id.ToString();
                    PromotionCodeTextBox.Text = promotionCode.Code;
                    DiscountAmountNumberBox.Value = (double)promotionCode.DiscountAmount;
                    MinimumAmountNumberBox.Value = (double)promotionCode.MinimumAmount;
                    
                    if (promotionCode.UsageLimit.HasValue)
                    {
                        UsageLimitNumberBox.Value = promotionCode.UsageLimit.Value;
                    }
                    
                    UsedCountTextBox.Text = promotionCode.UsedCount.ToString();
                    IsActiveToggleSwitch.IsOn = promotionCode.IsActive;

                    try
                    {
                        var promotionViewModels = PromotionComboBox?.ItemsSource as IEnumerable<PromotionViewModel>;
                        var selectedPromotion = promotionViewModels?.FirstOrDefault(p => p.Id == promotionCode.PromotionId);
                        if (selectedPromotion != null && PromotionComboBox != null)
                        {
                            PromotionComboBox.SelectedItem = selectedPromotion;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error selecting promotion in PromotionCodeDialog for PromotionId={promotionCode.PromotionId}", ex);
                    }

                    // Set validation states
                    _isCodeValid = !string.IsNullOrWhiteSpace(promotionCode.Code);
                    _isPromotionValid = promotionCode.PromotionId > 0;
                    _isDiscountAmountValid = promotionCode.DiscountAmount > 0;
                    _isMinimumAmountValid = promotionCode.MinimumAmount >= 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load promotion code in PromotionCodeDialog", ex);
                ShowError(PromotionCodeError, _resourceLoader.GetString("LoadPromotionCodeError"));
            }
        }

        #region Validation

        private void PromotionCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = PromotionCodeTextBox?.Text ?? string.Empty;
            ValidateCode(text);
        }

        private void PromotionComboBox_DropDownOpened(object sender, object e)
        {
            try
            {
                Logger.Info("PromotionComboBox DropDownOpened fired");
                var comboBox = sender as ComboBox;
                Logger.Info($"ComboBox ItemsSource is null: {comboBox?.ItemsSource == null}");
                Logger.Info($"ComboBox Items count: {comboBox?.Items?.Count ?? -1}");
            }
            catch (Exception ex)
            {
                Logger.Error("Exception in PromotionComboBox_DropDownOpened", ex);
            }
        }

        private void PromotionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Logger.Info("PromotionComboBox SelectionChanged fired");
                ValidatePromotion();
            }
            catch (Exception ex)
            {
                Logger.Error("Unhandled exception in PromotionComboBox_SelectionChanged", ex);
                // surface a user-visible error and avoid crash
                ShowError(PromotionError, _resourceLoader.GetString("PromotionSelectionError"));
            }
        }

        private void DiscountAmountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            ValidateDiscountAmount();
        }

        private void MinimumAmountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            ValidateMinimumAmount();
        }

        private void ValidateCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                if (!_isInitializing)
                    ShowError(PromotionCodeError, _resourceLoader.GetString("PromotionCodeRequiredError"));
                _isCodeValid = false;
                return;
            }

            if (code.Length < 3)
            {
                if (!_isInitializing)
                    ShowError(PromotionCodeError, _resourceLoader.GetString("PromotionCodeMinLengthError"));
                _isCodeValid = false;
                return;
            }

            HideError(PromotionCodeError);
            _isCodeValid = true;
        }

        private void ValidatePromotion()
        {
            if (PromotionComboBox.SelectedItem == null)
            {
                if (!_isInitializing)
                    ShowError(PromotionError, _resourceLoader.GetString("PromotionSelectionRequiredError"));
                _isPromotionValid = false;
                return;
            }

            HideError(PromotionError);
            _isPromotionValid = true;
        }

        private void ValidateDiscountAmount()
        {
            if (double.IsNaN(DiscountAmountNumberBox.Value) || DiscountAmountNumberBox.Value <= 0)
            {
                if (!_isInitializing)
                    ShowError(DiscountAmountError, _resourceLoader.GetString("DiscountAmountGreaterThanZeroError"));
                _isDiscountAmountValid = false;
                return;
            }

            HideError(DiscountAmountError);
            _isDiscountAmountValid = true;
        }

        private void ValidateMinimumAmount()
        {
            if (double.IsNaN(MinimumAmountNumberBox.Value) || MinimumAmountNumberBox.Value < 0)
            {
                if (!_isInitializing)
                    ShowError(MinimumAmountError, _resourceLoader.GetString("MinimumAmountNonNegativeError"));
                _isMinimumAmountValid = false;
                return;
            }

            HideError(MinimumAmountError);
            _isMinimumAmountValid = true;
        }

        private void UpdatePrimaryButtonState()
        {
            // For ContentControl, we don't have IsPrimaryButtonEnabled property
            // This will be handled by the parent dialog
        }

        #endregion

        #region UI Helper Methods

        private void ShowLoading(bool isLoading)
        {
            // Implement loading indicator if needed
        }

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
            HideError(PromotionCodeError);
            HideError(PromotionError);
            HideError(DiscountAmountError);
            HideError(MinimumAmountError);
        }

        private void ResetValidationStates()
        {
            _isCodeValid = false;
            _isPromotionValid = false;
            _isDiscountAmountValid = false;
            _isMinimumAmountValid = false;
        }

        #endregion

        #region Public Methods

        public void SetMode(DialogMode mode, PromotionCodeViewModel? promotionCode = null)
        {
            Logger.Info($"SetMode called with mode: {mode}");
            try
            {
                _currentMode = mode;
                _pendingMode = mode;
                _pendingPromotionCodeData = promotionCode;
                
                // Only apply immediately if controls are ready
                if (PromotionCodeIdTextBox != null && PromotionCodeTextBox != null)
                {
                    ApplyModeAndData();
                }
                else
                {
                    Logger.Warning("Controls not ready yet in SetMode, storing for later application");
                }
                
                Logger.Info("SetMode completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SetMode", ex);
                throw;
            }
        }

        private void ApplyModeAndData()
        {
            Logger.Info($"ApplyModeAndData called with mode: {_pendingMode}");
            try
            {
                ResetValidationStates();
                HideAllErrors();
                
                // Ensure promotions are loaded before setting promotion code data
                LoadPromotions();
                LoadData(_pendingPromotionCodeData);
                UpdateUIForMode();
                
                _isInitializing = false;
                Logger.Info("ApplyModeAndData completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error in ApplyModeAndData", ex);
                throw;
            }
        }

        private void LoadData(PromotionCodeViewModel? promotionCode)
        {
            Logger.Info($"LoadData called with promotionCode: {promotionCode?.Id ?? 0}");
            try
            {
                if (promotionCode == null || _currentMode == DialogMode.Add)
                {
                    Logger.Info("Loading default data for Add mode");
                    if (PromotionCodeIdTextBox != null) PromotionCodeIdTextBox.Text = "";
                    if (PromotionCodeTextBox != null) PromotionCodeTextBox.Text = "";
                    if (DiscountAmountNumberBox != null) DiscountAmountNumberBox.Value = 0;
                    if (MinimumAmountNumberBox != null) MinimumAmountNumberBox.Value = 0;
                    if (UsageLimitNumberBox != null) UsageLimitNumberBox.Value = double.NaN;
                    if (UsedCountTextBox != null) UsedCountTextBox.Text = "0";
                    if (IsActiveToggleSwitch != null) IsActiveToggleSwitch.IsOn = true;

                    // If Add mode launched from PromotionEditPage with preselected PromotionId
                    if (promotionCode != null && promotionCode.PromotionId > 0 && PromotionComboBox != null)
                    {
                        var promotion = (PromotionComboBox.ItemsSource as System.Collections.Generic.IEnumerable<PromotionViewModel>)?.FirstOrDefault(p => p.Id == promotionCode.PromotionId);
                        if (promotion != null)
                        {
                            PromotionComboBox.SelectedItem = promotion;
                        }
                    }
                    Logger.Info("Default data loaded successfully");
                    return;
                }

                Logger.Info($"Loading existing promotion code data - ID: {promotionCode.Id}, Code: {promotionCode.Code}, PromotionId: {promotionCode.PromotionId}");
                if (PromotionCodeIdTextBox != null) PromotionCodeIdTextBox.Text = promotionCode.Id.ToString();
                if (PromotionCodeTextBox != null) PromotionCodeTextBox.Text = promotionCode.Code;
                if (DiscountAmountNumberBox != null) DiscountAmountNumberBox.Value = (double)promotionCode.DiscountAmount;
                if (MinimumAmountNumberBox != null) MinimumAmountNumberBox.Value = (double)promotionCode.MinimumAmount;
                if (UsageLimitNumberBox != null) UsageLimitNumberBox.Value = promotionCode.UsageLimit.HasValue ? (double)promotionCode.UsageLimit.Value : double.NaN;
                if (UsedCountTextBox != null) UsedCountTextBox.Text = promotionCode.UsedCount.ToString();
                if (IsActiveToggleSwitch != null) IsActiveToggleSwitch.IsOn = promotionCode.IsActive;

                // Set selected promotion
                if (PromotionComboBox != null && promotionCode.PromotionId > 0)
                {
                    Logger.Info($"Looking for promotion with ID: {promotionCode.PromotionId}");
                    var promotion = PromotionComboBox.Items?.OfType<PromotionViewModel>()
                        .FirstOrDefault(p => p.Id == promotionCode.PromotionId);
                    if (promotion != null)
                    {
                        PromotionComboBox.SelectedItem = promotion;
                        Logger.Info($"Selected promotion: {promotion.Name}");
                    }
                    else
                    {
                        Logger.Warning($"Promotion with ID {promotionCode.PromotionId} not found in ComboBox items");
                        Logger.Info($"Available promotions: {string.Join(", ", PromotionComboBox.Items?.OfType<PromotionViewModel>().Select(p => $"{p.Id}:{p.Name}") ?? new string[0])}");
                    }
                }

                // Validate loaded data
                ValidateCode(promotionCode.Code);
                ValidatePromotion();
                ValidateDiscountAmount();
                ValidateMinimumAmount();
                Logger.Info("Existing promotion code data loaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error in LoadData", ex);
                throw;
            }
        }

        private void UpdateUIForMode()
        {
            bool isEditable = _currentMode != DialogMode.View;

            if (PromotionCodeIdTextBox != null) PromotionCodeIdTextBox.IsReadOnly = true;
            if (PromotionCodeTextBox != null) PromotionCodeTextBox.IsReadOnly = !isEditable;
            // Only changeable in Add mode from standalone page; if PromotionId is preset (>0), keep disabled
            if (PromotionComboBox != null)
            {
                var preset = (_pendingPromotionCodeData?.PromotionId ?? 0) > 0;
                PromotionComboBox.IsEnabled = isEditable && _currentMode == DialogMode.Add && !preset;
            }
            if (DiscountAmountNumberBox != null) DiscountAmountNumberBox.IsEnabled = isEditable;
            if (MinimumAmountNumberBox != null) MinimumAmountNumberBox.IsEnabled = isEditable;
            if (UsageLimitNumberBox != null) UsageLimitNumberBox.IsEnabled = isEditable;
            if (UsedCountTextBox != null) UsedCountTextBox.IsReadOnly = true;
            if (IsActiveToggleSwitch != null) IsActiveToggleSwitch.IsEnabled = isEditable;
        }

        public void Save()
        {
            if (_currentMode == DialogMode.View)
            {
                DialogClosed?.Invoke(this, EventArgs.Empty);
                return;
            }

            ValidateAllFields();

            bool isFormValid = _isCodeValid && _isPromotionValid && _isDiscountAmountValid && _isMinimumAmountValid;
            if (!isFormValid)
                return;

            try
            {
                HideAllErrors();

                var promotionCode = new PromotionCode
                {
                    Id = int.TryParse(PromotionCodeIdTextBox?.Text, out int id) ? id : 0,
                    Code = PromotionCodeTextBox?.Text?.Trim() ?? string.Empty,
                    PromotionId = (PromotionComboBox?.SelectedItem as PromotionViewModel)?.Id ?? 0,
                    DiscountAmount = (decimal)DiscountAmountNumberBox.Value,
                    MinimumAmount = (decimal)MinimumAmountNumberBox.Value,
                    UsageLimit = !double.IsNaN(UsageLimitNumberBox.Value) ? (int?)UsageLimitNumberBox.Value : null,
                    UsedCount = int.TryParse(UsedCountTextBox?.Text, out int usedCount) ? usedCount : 0,
                    IsActive = IsActiveToggleSwitch?.IsOn ?? true
                };

                if (_currentMode == DialogMode.Add)
                    _promotionCodeService.Insert(promotionCode);
                else
                    _promotionCodeService.Update(promotionCode);

                var viewModel = new PromotionCodeViewModel(promotionCode);
                PromotionCodeSaved?.Invoke(this, viewModel);

            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save promotion code in PromotionCodeDialog", ex);
                ShowError(PromotionCodeError, ex.Message);
                throw;
            }
        }

        public bool IsValid()
        {
            ValidateAllFields();
            return _isCodeValid && _isPromotionValid && _isDiscountAmountValid && _isMinimumAmountValid;
        }

        public void Cancel()
        {
            ResetValidationStates();
            HideAllErrors();
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

        private void ValidateAllFields()
        {
            ValidateCode(PromotionCodeTextBox?.Text ?? string.Empty);
            ValidatePromotion();
            ValidateDiscountAmount();
            ValidateMinimumAmount();
        }

        #endregion
    }
}
