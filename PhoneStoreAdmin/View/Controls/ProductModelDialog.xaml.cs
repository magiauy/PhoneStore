using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class ProductModelDialog : ContentDialog
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

        private IReadOnlyList<ProductAttributeDefinition> _attributeDefinitions = Array.Empty<ProductAttributeDefinition>();
        private bool _attributesLoaded = false;
        private int _pendingModelIdForAttributeSelection = 0;

        public ObservableCollection<ModelAttributeSelection> AttributeSelections { get; } = new();

        public event EventHandler<ProductModelListItemViewModel>? ModelSaved;

        public ProductModelDialog()
        {
            InitializeComponent();
            _productService = App.GetService<IProductService>();
            _resourceLoader = new ResourceLoader();
            
            // Tải dữ liệu attributes asynchronously để tránh UI bị treo
            _ = LoadAttributeDefinitionsAsync();
        }

        public void SetMode(DialogMode mode, ProductModelListItemViewModel? model)
        {
            _mode = mode;
            _currentModelId = model?.Id ?? 0;
            _sourceModelId = model?.Id ?? 0;
            _createdAt = model?.CreatedAt ?? DateTime.UtcNow;

            NameTextBox.Text = model?.Name ?? string.Empty;
            DescriptionTextBox.Text = model?.Description ?? string.Empty;

            if (mode == DialogMode.Duplicate && !string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NameTextBox.Text = string.Format("{0} Copy", NameTextBox.Text);
            }

            HideInfoBars();
            HideAllErrors();
            UpdateAttributeSelectionsForModel(mode == DialogMode.Duplicate ? _sourceModelId : _currentModelId);
            ValidateAll();
        }

        private async Task LoadAttributeDefinitionsAsync()
        {
            try
            {
                // Chạy trên thread pool để tránh block UI thread
                await Task.Run(() =>
                {
                    _attributeDefinitions = _productService.GetAttributeDefinitions() ?? Array.Empty<ProductAttributeDefinition>();
                });

                // Cập nhật UI trên UI thread
                DispatcherQueue?.TryEnqueue(() =>
                {
                    AttributeSelections.Clear();
                    foreach (var definition in _attributeDefinitions.OrderBy(d => d.Attribute?.Name ?? string.Empty))
                    {
                        AttributeSelections.Add(new ModelAttributeSelection(definition));
                    }

                    _attributesLoaded = true;
                    UpdateAttributePlaceholder();
                    
                    // Nếu có pending model ID, apply selections ngay
                    if (_pendingModelIdForAttributeSelection > 0)
                    {
                        var pendingId = _pendingModelIdForAttributeSelection;
                        _pendingModelIdForAttributeSelection = 0;
                        UpdateAttributeSelectionsForModel(pendingId);
                    }
                    else
                    {
                        ApplyAttributeAssignments(Array.Empty<int>());
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading attributes: {ex.Message}");
            }
        }

        private void LoadAttributeDefinitions()
        {
            _attributeDefinitions = _productService.GetAttributeDefinitions() ?? Array.Empty<ProductAttributeDefinition>();

            AttributeSelections.Clear();
            foreach (var definition in _attributeDefinitions.OrderBy(d => d.Attribute?.Name ?? string.Empty))
            {
                AttributeSelections.Add(new ModelAttributeSelection(definition));
            }

            ApplyAttributeAssignments(Array.Empty<int>());
            UpdateAttributePlaceholder();
        }

        private void UpdateAttributePlaceholder()
        {
            if (EmptyAttributesText != null)
            {
                EmptyAttributesText.Visibility = AttributeSelections.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateAttributeSelectionsForModel(int modelId)
        {
            // Nếu attributes chưa load xong, lưu lại modelId để apply sau
            if (!_attributesLoaded || AttributeSelections.Count == 0)
            {
                _pendingModelIdForAttributeSelection = modelId;
                return;
            }

            IReadOnlyList<int> assignedIds = Array.Empty<int>();
            if (modelId > 0)
            {
                assignedIds = _productService.GetModelAttributeIds(modelId) ?? Array.Empty<int>();
            }

            ApplyAttributeAssignments(assignedIds);
        }

        private void ApplyAttributeAssignments(IReadOnlyCollection<int> attributeIds)
        {
            var lookup = attributeIds != null
                ? new HashSet<int>(attributeIds)
                : new HashSet<int>();

            foreach (var selection in AttributeSelections)
            {
                selection.IsSelected = lookup.Contains(selection.AttributeId);
            }
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
                var selectedAttributeIds = AttributeSelections
                    .Where(selection => selection.IsSelected)
                    .Select(selection => selection.AttributeId)
                    .ToList();

                ProductModel? model;
                if (_mode == DialogMode.Duplicate && _sourceModelId > 0)
                {
                    model = _productService.DuplicateProductModel(_sourceModelId, name, description, null, selectedAttributeIds);
                }
                else
                {
                    model = new ProductModel
                    {
                        Id = _mode == DialogMode.Edit ? _currentModelId : 0,
                        Name = name,
                        Description = description,
                        DefaultImageUrl = null,
                        CreatedAt = _mode == DialogMode.Edit ? _createdAt : DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (!_productService.SaveProductModel(model, selectedAttributeIds))
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

                UpdateAttributeSelectionsForModel(model.Id);
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
            return _isNameValid && _isDescriptionValid;
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateName();
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateDescription();
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
        }

        private void HideInfoBars()
        {
            ErrorInfoBar.IsOpen = false;
            SuccessInfoBar.IsOpen = false;
        }
    }
}
