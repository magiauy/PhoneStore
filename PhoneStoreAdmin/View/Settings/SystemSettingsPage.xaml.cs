using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Models;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PhoneStoreAdmin.View
{
    public sealed partial class SystemSettingsPage : Page
    {
        private readonly ISettingStringService _settingStringService;
        private readonly ResourceLoader _resourceLoader = new();

        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize = 20;
        private string? _searchText;

        public ObservableCollection<SettingString> Settings { get; } = new();

        public bool CanEditSetting => UserSession.Instance.HasPermission("SETTING_EDIT");

        private SettingString? _editingSetting;

        public SystemSettingsPage()
        {
            InitializeComponent();
            
            _settingStringService = ServiceContainer.GetService<ISettingStringService>()
                ?? throw new InvalidOperationException("SettingStringService is not registered.");

            InitializeLocalizedStrings();
        }

        private void InitializeLocalizedStrings()
        {
            // Header
            HeaderTitle.Text = _resourceLoader.GetString("SystemSettings_Title");
            HeaderSubtitle.Text = _resourceLoader.GetString("SystemSettings_Subtitle");
            BtnInitializeSettingsText.Text = _resourceLoader.GetString("SystemSettings_BtnInitialize");
            
            // Search
            SearchBox.PlaceholderText = _resourceLoader.GetString("SystemSettings_SearchPlaceholder");
            
            // Table Headers
            ColIdHeader.Text = _resourceLoader.GetString("SystemSettings_ColId");
            ColCodeHeader.Text = _resourceLoader.GetString("SystemSettings_ColCode");
            ColValueHeader.Text = _resourceLoader.GetString("SystemSettings_ColValue");
            ColTypeHeader.Text = _resourceLoader.GetString("SystemSettings_ColType");
            ColActionsHeader.Text = _resourceLoader.GetString("SystemSettings_ColActions");
            
            // Empty State
            EmptyStateTitle.Text = _resourceLoader.GetString("SystemSettings_EmptyTitle");
            EmptyStateDescription.Text = _resourceLoader.GetString("SystemSettings_EmptyDescription");
            
            // Footer
            ShowingText.Text = _resourceLoader.GetString("SystemSettings_Showing");
            SettingsText.Text = _resourceLoader.GetString("SystemSettings_Settings");
            PageText.Text = _resourceLoader.GetString("SystemSettings_Page");
            
            // Dialog
            EditCodeTextBox.Header = _resourceLoader.GetString("SystemSettings_CodeLabel");
            EditValueTextBox.Header = _resourceLoader.GetString("SystemSettings_ValueLabel");
            EditTypeComboBox.Header = _resourceLoader.GetString("SystemSettings_TypeLabel");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            CheckAndInitializeSettings();
            LoadSettingsData();
        }

        private void CheckAndInitializeSettings()
        {
            // Check if all settings are initialized
            bool allInitialized = _settingStringService.AreAllSettingsInitialized();
            InitializeSettingsButton.Visibility = allInitialized ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnInitializeSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _settingStringService.InitializeSettings();
                InitializeSettingsButton.Visibility = Visibility.Collapsed;
                LoadSettingsData();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize settings", ex);
                ShowErrorDialog(_resourceLoader.GetString("SystemSettings_Error_InitializeFailed"));
            }
        }

        private void LoadSettingsData()
        {
            try
            {
                var result = _settingStringService.GetSettingsFiltered(_searchText, _currentPage, _pageSize);
                
                Settings.Clear();
                foreach (var setting in result.Settings)
                {
                    Settings.Add(setting);
                }

                _totalPages = result.Info.TotalPages > 0 ? result.Info.TotalPages : 1;
                RecordCountText.Text = result.Info.TotalRecords.ToString();
                PageInfoText.Text = $"{_currentPage} / {_totalPages}";
                
                PreviousPageButton.IsEnabled = _currentPage > 1;
                NextPageButton.IsEnabled = _currentPage < _totalPages;

                EmptyStatePanel.Visibility = Settings.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                SettingsListView.Visibility = Settings.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load settings", ex);
                ShowErrorDialog(_resourceLoader.GetString("SystemSettings_Error_LoadFailed"));
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                _searchText = string.IsNullOrWhiteSpace(sender.Text) ? null : sender.Text;
                _currentPage = 1;
                LoadSettingsData();
            }
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            _searchText = null;
            _currentPage = 1;
            LoadSettingsData();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                OpenEditDialog(id);
            }
        }

        private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is SettingString setting)
            {
                OpenEditDialog(setting.Id);
            }
        }

        private void OpenEditDialog(int id)
        {
            if (!CanEditSetting)
            {
                ShowErrorDialog(_resourceLoader.GetString("SystemSettings_Error_NoEditPermission"));
                return;
            }

            var setting = Settings.FirstOrDefault(s => s.Id == id);
            if (setting == null)
            {
                ShowErrorDialog(_resourceLoader.GetString("SystemSettings_Error_NotFound"));
                return;
            }

            _editingSetting = setting;
            
            EditDialog.Title = _resourceLoader.GetString("SystemSettings_DialogTitleEdit");
            EditDialog.PrimaryButtonText = _resourceLoader.GetString("Common/Save");
            EditDialog.SecondaryButtonText = _resourceLoader.GetString("Common/Cancel");
            
            EditCodeTextBox.Text = setting.Code;
            EditValueTextBox.Text = setting.Value;
            
            // Select the correct type in ComboBox
            for (int i = 0; i < EditTypeComboBox.Items.Count; i++)
            {
                if (EditTypeComboBox.Items[i] is ComboBoxItem item && 
                    item.Tag?.ToString() == setting.Type)
                {
                    EditTypeComboBox.SelectedIndex = i;
                    break;
                }
            }
            
            EditDialog.XamlRoot = this.XamlRoot;
            _ = EditDialog.ShowAsync();
        }

        private void EditDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_editingSetting == null)
            {
                args.Cancel = true;
                return;
            }

            var value = EditValueTextBox.Text?.Trim();

            // Only update value - code and type are read-only
            _editingSetting.Value = value ?? string.Empty;

            bool success = _settingStringService.Update(_editingSetting);

            if (success)
            {
                LoadSettingsData();
            }
            else
            {
                ShowErrorDialog(_resourceLoader.GetString("SystemSettings_Error_SaveFailed"));
                args.Cancel = true;
            }
        }

        private void EditDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            _editingSetting = null;
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadSettingsData();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadSettingsData();
            }
        }

        private async void ShowErrorDialog(string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("Common/Error"),
                    Content = message,
                    CloseButtonText = _resourceLoader.GetString("Common/Close"),
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show error dialog", ex);
            }
        }
    }
}
