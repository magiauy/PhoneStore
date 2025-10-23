using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class BrandDialog : ContentControl
    {
        private readonly IBrandService _brandService;
        private readonly ResourceLoader _resourceLoader;
        private bool _isNameValid = false;

        public event EventHandler<BrandViewModel>? BrandSaved;
        public event EventHandler? DialogClosed;

        public enum DialogMode { Add, Edit, View }

        private DialogMode _currentMode = DialogMode.Add;


        public BrandDialog()
        {
            this.InitializeComponent();
            _brandService = App.GetService<IBrandService>();
            _resourceLoader = new ResourceLoader();
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            ResetValidationStates();
            HideAllErrors();
        }

        #region Field Validation Events

        private void BrandNameTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            var text = BrandNameTextBox.Text ?? string.Empty;
            ValidateName(text);
        }

        #endregion

        #region Validation Logic

        private void ValidateName(string name)
        {
            _isNameValid = !string.IsNullOrWhiteSpace(name) && name.Length >= 2;

            if (!_isNameValid)
            {
                ShowError(BrandNameError, _resourceLoader.GetString("NameRequired/Text") ?? "Name is required (min 2 characters)");
            }
            else
            {
                HideError(BrandNameError);
            }
        }


        #endregion

        #region UI Helper Methods

        private void ShowError(TextBlock errorTextBlock, string message)
        {
            errorTextBlock.Text = message;
            errorTextBlock.Visibility = Visibility.Visible;
        }

        private void HideError(TextBlock errorTextBlock)
        {
            errorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void HideAllErrors()
        {
            HideError(BrandNameError);
            ErrorInfoBar.IsOpen = false;
        }

        private void ResetValidationStates()
        {
            _isNameValid = false;
        }

        private void ShowLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            BrandNameTextBox.IsEnabled = !isLoading && _currentMode != DialogMode.View;
        }

        #endregion

        #region Public Methods


        public void SetMode(DialogMode mode, BrandViewModel? brand = null)
        {
            _currentMode = mode;
            LoadData(brand);
            UpdateUIForMode();
        }

        private void LoadData(BrandViewModel? brand)
        {
            if (brand == null || _currentMode == DialogMode.Add)
            {
                BrandIdTextBox.Text = "";
                BrandNameTextBox.Text = "";
                return;
            }

            BrandIdTextBox.Text = brand.Id.ToString();
            BrandNameTextBox.Text = brand.Name;

            ValidateName(brand.Name);
        }

        private void UpdateUIForMode()
        {
            bool isEditable = _currentMode != DialogMode.View;

            BrandIdTextBox.IsReadOnly = true;
            BrandNameTextBox.IsReadOnly = !isEditable;
        }


        public void Save()
        {
            if (_currentMode == DialogMode.View)
            {
                DialogClosed?.Invoke(this, EventArgs.Empty);
                return;
            }

            ValidateAllFields();

            bool isFormValid = _isNameValid;
            if (!isFormValid)
                return;

            try
            {
                ShowLoading(true);
                HideAllErrors();

                var brand = new Brand
                {
                    Id = int.TryParse(BrandIdTextBox.Text, out int id) ? id : 0,
                    Name = BrandNameTextBox.Text ?? string.Empty
                };

                if (_currentMode == DialogMode.Add)
                    _brandService.Insert(brand);
                else
                    _brandService.Update(brand);

                var viewModel = new BrandViewModel(brand);
                BrandSaved?.Invoke(this, viewModel);

                DialogClosed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = _resourceLoader.GetString("UnexpectedError");
                ErrorInfoBar.IsOpen = true;
            }
            finally
            {
                ShowLoading(false);
            }
        }

        public bool IsValid()
        {
            ValidateAllFields();
            return _isNameValid;
        }

        public void Cancel()
        {
            ResetValidationStates();
            HideAllErrors();
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

        private void ValidateAllFields()
        {
            ValidateName(BrandNameTextBox.Text ?? string.Empty);
        }

        #endregion
    }
}