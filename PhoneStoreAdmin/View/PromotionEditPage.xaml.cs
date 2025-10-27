using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using PhoneStoreAdmin.View.Controls;
using System.Collections.ObjectModel;
using System;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Utils;

namespace PhoneStoreAdmin.View
{
    public sealed partial class PromotionEditPage : Page
    {
        private readonly IPromotionService _promotionService = App.GetService<IPromotionService>();
        private readonly IPromotionCodeService _promotionCodeService = App.GetService<IPromotionCodeService>();

        private PromotionViewModel? _currentPromotion;
        private readonly ResourceLoader _resourceLoader = new ResourceLoader();
        public ObservableCollection<PromotionCodeViewModel> PromotionCodesCollection { get; set; } = new ObservableCollection<PromotionCodeViewModel>();

        public PromotionEditPage()
        {
            this.InitializeComponent();
            InitializeLocalizedStrings();
            Logger.Info("PromotionEditPage initialized");
        }

        private void InitializeLocalizedStrings()
        {
            SaveButton.Content = _resourceLoader.GetString("Common/Save");
            CancelButton.Content = _resourceLoader.GetString("Common/Cancel");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is int promotionId)
            {
                Logger.Info($"Navigated to PromotionEditPage with promotionId={promotionId}");
                try
                {
                    LoadPromotionAsync(promotionId);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error loading promotion in PromotionEditPage", ex);
                }
            }
        }

        private void LoadPromotionAsync(int promotionId)
        {
            Logger.Info($"Loading promotion id={promotionId}");
            var promotion = _promotionService.GetPromotionById(promotionId);
            if (promotion == null)
            {
                Logger.Warning($"Promotion id={promotionId} not found, navigating back");
                // Navigate back if not found
                Frame?.Navigate(typeof(PromotionsPage));
                return;
            }

            _currentPromotion = new PromotionViewModel(promotion);
            Logger.Info($"Loaded promotion: id={_currentPromotion.Id}, name={_currentPromotion.Name}");

            // Bind to controls
            PromotionNameTextBox.Text = _currentPromotion.Name;
            DescriptionTextBox.Text = _currentPromotion.Description ?? string.Empty;
            StartDatePicker.Date = new System.DateTimeOffset(_currentPromotion.StartDate);
            EndDatePicker.Date = new System.DateTimeOffset(_currentPromotion.EndDate);
            IsActiveToggle.IsOn = _currentPromotion.IsActive;

            // Load codes
            Logger.Info("Loading promotion codes...");
            var codesResult = _promotionCodeService.GetPromotionCodesFilteredWithPromotionNames(
                code: null,
                promotionId: promotionId,
                isActive: null,
                page: 1,
                pageSize: 1000);

            PromotionCodesCollection.Clear();
            foreach (var code in codesResult.PromotionCodes)
            {
                PromotionCodesCollection.Add(code);
            }
            Logger.Info($"Loaded {PromotionCodesCollection.Count} codes for promotion id={promotionId}");
        }

        private void AddCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPromotion == null) return;

            var dialogCtrl = new PromotionCodeDialog();
            dialogCtrl.SetMode(PromotionCodeDialog.DialogMode.Add, new PromotionCodeViewModel { PromotionId = _currentPromotion.Id });

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("AddPromotionCodeTitle"),
                Content = dialogCtrl,
                PrimaryButtonText = _resourceLoader.GetString("DialogAdd"),
                CloseButtonText = _resourceLoader.GetString("DialogCancel"),
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                if (!dialogCtrl.IsValid())
                {
                    args.Cancel = true;
                    return;
                }
                dialogCtrl.Save();
            };

            dialogCtrl.PromotionCodeSaved += (s, vm) =>
            {
                // refresh list
                if (_currentPromotion != null)
                    LoadPromotionAsync(_currentPromotion.Id);
            };

            _ = dialog.ShowAsync();
        }

        private void EditPromotionCode_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionCodeId)) return;

            var existing = _promotionCodeService.GetPromotionCodeById(promotionCodeId);
            if (existing == null) return;

            var vm = new PromotionCodeViewModel(existing);
            var dialogCtrl = new PromotionCodeDialog();
            dialogCtrl.SetMode(PromotionCodeDialog.DialogMode.Edit, vm);

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("EditPromotionCodeTitle"),
                Content = dialogCtrl,
                PrimaryButtonText = _resourceLoader.GetString("DialogUpdate"),
                CloseButtonText = _resourceLoader.GetString("DialogCancel"),
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                if (!dialogCtrl.IsValid())
                {
                    args.Cancel = true;
                    return;
                }
                dialogCtrl.Save();
            };

            dialogCtrl.PromotionCodeSaved += (s, savedVm) =>
            {
                if (_currentPromotion != null)
                    LoadPromotionAsync(_currentPromotion.Id);
            };

            _ = dialog.ShowAsync();
        }

        private async void ActivatePromotionCode_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionCodeId)) return;

            var confirm = new ContentDialog
            {
                Title = _resourceLoader.GetString("ConfirmActivateCodeTitle"),
                Content = _resourceLoader.GetString("ConfirmActivateCodeContent"),
                PrimaryButtonText = _resourceLoader.GetString("BtnConfirm"),
                CloseButtonText = _resourceLoader.GetString("BtnCancel"),
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await confirm.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _promotionCodeService.ActivatePromotionCode(promotionCodeId);
                if (_currentPromotion != null)
                    LoadPromotionAsync(_currentPromotion.Id);
            }
        }

        private async void DeactivatePromotionCode_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionCodeId)) return;

            var confirm = new ContentDialog
            {
                Title = _resourceLoader.GetString("ConfirmDeactivateCodeTitle"),
                Content = _resourceLoader.GetString("ConfirmDeactivateCodeContent"),
                PrimaryButtonText = _resourceLoader.GetString("BtnConfirm"),
                CloseButtonText = _resourceLoader.GetString("BtnCancel"),
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await confirm.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _promotionCodeService.DeactivatePromotionCode(promotionCodeId);
                if (_currentPromotion != null)
                    LoadPromotionAsync(_currentPromotion.Id);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPromotion == null) return;

            // Basic validation
            var name = PromotionNameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return;

            var updated = new PhoneStoreAdmin.Models.Promotion
            {
                Id = _currentPromotion.Id,
                Name = name,
                Description = DescriptionTextBox.Text,
                StartDate = StartDatePicker.Date.DateTime,
                EndDate = EndDatePicker.Date.DateTime,
                IsActive = IsActiveToggle.IsOn
            };

            _promotionService.Update(updated);
            Frame?.Navigate(typeof(PromotionsPage));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Frame?.Navigate(typeof(PromotionsPage));
        }

        private void BreadcrumbPromotions_Click(object sender, RoutedEventArgs e)
        {
            Frame?.Navigate(typeof(PromotionsPage));
        }
    }
}
