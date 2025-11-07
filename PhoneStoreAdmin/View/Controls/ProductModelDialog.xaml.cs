using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class ProductModelDialog : ContentControl
    {
        public enum DialogMode
        {
            Create,
            Edit,
            Duplicate
        }

        private readonly IProductService _productService;
        private readonly ResourceLoader _resourceLoader;

        private DialogMode _mode = DialogMode.Create;
        private int _currentModelId;
        private int _sourceModelId;
        private DateTime _createdAt = DateTime.UtcNow;

        private bool _isNameValid;
        private bool _isDescriptionValid = true;
        private bool _isImageValid = true;

        public event EventHandler<ProductModelListItemViewModel>? ModelSaved;

        public ProductModelDialog()
        {
            InitializeComponent();
            _productService = App.GetService<IProductService>();
            _resourceLoader = new ResourceLoader();
        }

        public void SetMode(DialogMode mode, ProductModelListItemViewModel? model)
        {
            _mode = mode;
            _currentModelId = model?.Id ?? 0;
            _sourceModelId = model?.Id ?? 0;
            _createdAt = model?.CreatedAt ?? DateTime.UtcNow;

            NameTextBox.Text = model?.Name ?? string.Empty;
            DescriptionTextBox.Text = model?.Description ?? string.Empty;
            ImageUrlTextBox.Text = model?.DefaultImageUrl ?? string.Empty;

            if (mode == DialogMode.Duplicate && !string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NameTextBox.Text = string.Format("{0} Copy", NameTextBox.Text);
            }

            HideInfoBars();
            HideAllErrors();
            ValidateAll();
        }

        public bool Save()
        {
            HideInfoBars();
            if (!ValidateAll())
            {
                ErrorInfoBar.Message = _resourceLoader.GetString("ProductModelValidationFailed") ?? "Please review required fields.";
                ErrorInfoBar.IsOpen = true;
                return false;
            }

            try
            {
                var name = (NameTextBox.Text ?? string.Empty).Trim();
                var description = NormalizeOptional(DescriptionTextBox.Text);
                var imageUrl = NormalizeOptional(ImageUrlTextBox.Text);

                ProductModel? model;
                if (_mode == DialogMode.Duplicate && _sourceModelId > 0)
                {
                    model = _productService.DuplicateProductModel(_sourceModelId, name, description, imageUrl);
                }
                else
                {
                    model = new ProductModel
                    {
                        Id = _mode == DialogMode.Edit ? _currentModelId : 0,
                        Name = name,
                        Description = description,
                        DefaultImageUrl = imageUrl,
                        CreatedAt = _mode == DialogMode.Edit ? _createdAt : DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (!_productService.SaveProductModel(model))
                    {
                        model = null;
                    }
                }

                if (model == null)
                {
                    ErrorInfoBar.Message = _resourceLoader.GetString("ProductModelSaveFailed") ?? "Unable to save product model.";
                    ErrorInfoBar.IsOpen = true;
                    return false;
                }

                var detail = _productService.GetProductModelDetail(model.Id);
                var viewModel = detail?.Model ?? new ProductModelListItemViewModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    DefaultImageUrl = model.DefaultImageUrl,
                    VariantCount = detail?.Model.VariantCount ?? 0,
                    CreatedAt = model.CreatedAt,
                    UpdatedAt = model.UpdatedAt
                };

                ModelSaved?.Invoke(this, viewModel);
                SuccessInfoBar.Message = _resourceLoader.GetString("ProductModelSaveSuccess") ?? "Model saved successfully.";
                SuccessInfoBar.IsOpen = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = string.Format(
                    _resourceLoader.GetString("ProductModelSaveFailedWithReason") ?? "Unable to save product model: {0}",
                    ex.Message);
                ErrorInfoBar.IsOpen = true;
                return false;
            }
        }

        public void Cancel()
        {
            HideInfoBars();
        }

        private bool ValidateAll()
        {
            ValidateName();
            ValidateDescription();
            ValidateImageUrl();
            return _isNameValid && _isDescriptionValid && _isImageValid;
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateName();
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateDescription();
        }

        private void ImageUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateImageUrl();
        }

        private void ValidateName()
        {
            var text = (NameTextBox.Text ?? string.Empty).Trim();
            _isNameValid = !string.IsNullOrWhiteSpace(text);

            if (_isNameValid)
            {
                HideError(NameErrorText);
            }
            else
            {
                ShowError(NameErrorText, _resourceLoader.GetString("ProductModelNameRequired") ?? "Model name is required.");
            }
        }

        private void ValidateDescription()
        {
            var text = DescriptionTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                _isDescriptionValid = true;
                HideError(DescriptionErrorText);
                return;
            }

            _isDescriptionValid = text.Length <= 500;
            if (_isDescriptionValid)
            {
                HideError(DescriptionErrorText);
            }
            else
            {
                ShowError(DescriptionErrorText, _resourceLoader.GetString("ProductModelDescriptionTooLong") ?? "Description is too long (max 500 characters).");
            }
        }

        private void ValidateImageUrl()
        {
            var text = NormalizeOptional(ImageUrlTextBox.Text);
            if (string.IsNullOrEmpty(text))
            {
                _isImageValid = true;
                HideError(ImageUrlErrorText);
                return;
            }

            _isImageValid = Uri.TryCreate(text, UriKind.Absolute, out _);
            if (_isImageValid)
            {
                HideError(ImageUrlErrorText);
            }
            else
            {
                ShowError(ImageUrlErrorText, _resourceLoader.GetString("ProductModelImageInvalid") ?? "Enter a valid absolute image URL.");
            }
        }

        private static string? NormalizeOptional(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return text.Trim();
        }

        private void ShowError(TextBlock target, string message)
        {
            target.Text = message;
            target.Visibility = Visibility.Visible;
        }

        private void HideError(TextBlock target)
        {
            target.Text = string.Empty;
            target.Visibility = Visibility.Collapsed;
        }

        private void HideAllErrors()
        {
            HideError(NameErrorText);
            HideError(DescriptionErrorText);
            HideError(ImageUrlErrorText);
        }

        private void HideInfoBars()
        {
            ErrorInfoBar.IsOpen = false;
            SuccessInfoBar.IsOpen = false;
        }
    }
}
