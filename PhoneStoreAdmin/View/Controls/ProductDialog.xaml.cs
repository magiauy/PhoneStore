using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class ProductDialog : ContentControl
    {
        public sealed class SelectionOption<T>
        {
            public SelectionOption(string displayName, T value)
            {
                DisplayName = displayName;
                Value = value;
            }

            public string DisplayName { get; }
            public T Value { get; }
        }

        private sealed class AttributeInputState
        {
            public required ProductAttribute Attribute { get; init; }
            public required FrameworkElement InputControl { get; init; }
            public required TextBlock ErrorTextBlock { get; init; }
            public bool IsRequired { get; init; }
            public bool IsValid { get; set; }
        }

        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly IProductCategoryRepository _categoryRepository;
        private readonly IProductAttributeRepository _attributeRepository;
        private readonly IProductSerialRepository _serialRepository;
        private readonly ResourceLoader _resourceLoader;

        private readonly List<SelectionOption<int>> _categoryOptions = new();
        private readonly List<SelectionOption<int?>> _brandOptions = new();
        private readonly List<SelectionOption<ProductStatus>> _statusOptions = new();
        private readonly List<AttributeInputState> _attributeInputs = new();
        private IReadOnlyList<ProductAttribute> _attributeDefinitions = new List<ProductAttribute>();

        private bool _referenceDataLoaded;
        private bool _isSkuValid;
        private bool _isNameValid;
        private bool _isCategoryValid;
        private bool _isPriceValid;
        private bool _isCostValid;
        private bool _isWarrantyValid;
        private bool _isStatusValid;

        private int _currentProductId;
        private int _currentSerialCount;
        private DateTime _currentCreatedAt = DateTime.UtcNow;
        private ProductDetailViewModel? _currentDetail;

        public event EventHandler<ProductDetailViewModel>? ProductSaved;
        public event EventHandler? DialogClosed;
        public event EventHandler<int>? ViewSerialsRequested;

        public enum DialogMode
        {
            Add,
            Edit,
            View
        }

        private DialogMode _currentMode = DialogMode.Add;

        public ProductDialog()
        {
            InitializeComponent();
            _productService = App.GetService<IProductService>();
            _brandService = App.GetService<IBrandService>();
            _categoryRepository = App.GetService<IProductCategoryRepository>();
            _attributeRepository = App.GetService<IProductAttributeRepository>();
            _serialRepository = App.GetService<IProductSerialRepository>();
            _resourceLoader = new ResourceLoader();

            this.Unloaded += ProductDialog_Unloaded;
        }

        private void ProductDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            ProductSaved = null;
            DialogClosed = null;
            ViewSerialsRequested = null;
        }

        #region Public API

        public void SetMode(DialogMode mode, ProductDetailViewModel? detail = null)
        {
            _currentMode = mode;
            _currentDetail = detail;

            EnsureReferenceDataLoaded();
            PopulateForm(detail);
            UpdateUIForMode();
        }

        public void Save()
        {
            if (_currentMode == DialogMode.View)
            {
                DialogClosed?.Invoke(this, EventArgs.Empty);
                return;
            }

            HideInfoBars();
            var isValid = IsValid();

            if (!isValid)
            {
                ErrorInfoBar.Message = _resourceLoader.GetString("ProductValidationFailedMessage");
                ErrorInfoBar.IsOpen = true;
                return;
            }

            try
            {
                ShowLoading(true);

                var product = BuildProductFromForm();
                var attributeInputs = BuildAttributeInputs().ToList();

                bool success = _currentMode == DialogMode.Add
                    ? _productService.CreateProduct(product, attributeInputs)
                    : _productService.UpdateProduct(product, attributeInputs);

                if (!success)
                {
                    ErrorInfoBar.Message = _resourceLoader.GetString("ProductSaveFailedMessage");
                    ErrorInfoBar.IsOpen = true;
                    return;
                }

                _currentProductId = product.Id;

                var detail = _productService.GetProductDetail(product.Id);
                if (detail != null)
                {
                    _currentDetail = detail;
                }
                else
                {
                    _currentDetail = BuildFallbackDetail(product, attributeInputs);
                }

                ProductSaved?.Invoke(this, _currentDetail!);

                SuccessInfoBar.Message = _resourceLoader.GetString("ProductSaveSuccessMessage");
                SuccessInfoBar.IsOpen = true;

                DialogClosed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving product", ex);
                ErrorInfoBar.Message = _resourceLoader.GetString("ProductSaveFailedMessage");
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
            return _isSkuValid
                && _isNameValid
                && _isCategoryValid
                && _isPriceValid
                && _isCostValid
                && _isWarrantyValid
                && _isStatusValid
                && _attributeInputs.All(i => i.IsValid);
        }

        public void Cancel()
        {
            ResetValidationStates();
            HideAllErrors();
            HideInfoBars();
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Field Events

        private void SkuTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateSku(SkuTextBox.Text ?? string.Empty);
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateName(NameTextBox.Text ?? string.Empty);
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateCategory();
        }

        private void BrandComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // optional field; keep error hidden
            HideError(BrandError);
        }

        private void PriceNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            ValidatePrice();
        }

        private void CostNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            ValidateCost();
        }

        private void WarrantyNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            ValidateWarranty();
        }

        private void SerialTrackedToggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateSerialSummaryVisibility();
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateStatus();
        }

        private void ViewSerialsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProductId > 0)
            {
                ViewSerialsRequested?.Invoke(this, _currentProductId);
            }
        }

        #endregion

        #region Validation Logic

        private void ValidateAllFields()
        {
            ValidateSku(SkuTextBox.Text ?? string.Empty);
            ValidateName(NameTextBox.Text ?? string.Empty);
            ValidateCategory();
            ValidatePrice();
            ValidateCost();
            ValidateWarranty();
            ValidateStatus();

            foreach (var attribute in _attributeInputs)
            {
                ValidateAttribute(attribute);
            }
        }

        private void ValidateSku(string sku)
        {
            _isSkuValid = !string.IsNullOrWhiteSpace(sku);

            if (!_isSkuValid)
            {
                ShowError(SkuError, _resourceLoader.GetString("ProductSkuRequiredError"));
            }
            else
            {
                HideError(SkuError);
            }
        }

        private void ValidateName(string name)
        {
            _isNameValid = !string.IsNullOrWhiteSpace(name);

            if (!_isNameValid)
            {
                ShowError(NameError, _resourceLoader.GetString("ProductNameRequiredError"));
            }
            else
            {
                HideError(NameError);
            }
        }

        private void ValidateCategory()
        {
            _isCategoryValid = CategoryComboBox.SelectedValue is int;

            if (!_isCategoryValid)
            {
                ShowError(CategoryError, _resourceLoader.GetString("ProductCategoryRequiredError"));
            }
            else
            {
                HideError(CategoryError);
            }
        }

        private void ValidatePrice()
        {
            var value = PriceNumberBox.Value;
            if (double.IsNaN(value) || value < 0)
            {
                _isPriceValid = false;
                ShowError(PriceError, _resourceLoader.GetString("ProductPriceInvalidError"));
                return;
            }

            _isPriceValid = true;
            HideError(PriceError);
        }

        private void ValidateCost()
        {
            var value = CostNumberBox.Value;
            if (double.IsNaN(value) || value < 0)
            {
                _isCostValid = false;
                ShowError(CostError, _resourceLoader.GetString("ProductCostInvalidError"));
                return;
            }

            _isCostValid = true;
            HideError(CostError);
        }

        private void ValidateWarranty()
        {
            var value = WarrantyNumberBox.Value;
            if (double.IsNaN(value) || value < 0)
            {
                _isWarrantyValid = false;
                ShowError(WarrantyError, _resourceLoader.GetString("ProductWarrantyInvalidError"));
                return;
            }

            _isWarrantyValid = true;
            HideError(WarrantyError);
        }

        private void ValidateStatus()
        {
            _isStatusValid = StatusComboBox.SelectedValue is ProductStatus;

            if (!_isStatusValid)
            {
                ShowError(StatusError, _resourceLoader.GetString("ProductStatusRequiredError"));
            }
            else
            {
                HideError(StatusError);
            }
        }

        private void ValidateAttribute(AttributeInputState state)
        {
            if (state.Attribute == null)
            {
                return;
            }

            switch (state.Attribute.DataType)
            {
                case AttributeDataType.TEXT:
                    ValidateTextAttribute(state);
                    break;
                case AttributeDataType.NUMBER:
                    ValidateNumberAttribute(state);
                    break;
                case AttributeDataType.DATE:
                    ValidateDateAttribute(state);
                    break;
                case AttributeDataType.BOOLEAN:
                    ValidateBooleanAttribute(state);
                    break;
                default:
                    state.IsValid = true;
                    HideError(state.ErrorTextBlock);
                    break;
            }
        }

        private void ValidateTextAttribute(AttributeInputState state)
        {
            var textBox = state.InputControl as TextBox;
            var text = textBox?.Text?.Trim();
            var hasValue = !string.IsNullOrWhiteSpace(text);

            if (state.IsRequired && !hasValue)
            {
                ShowAttributeRequiredError(state);
                state.IsValid = false;
                return;
            }

            state.IsValid = true;
            HideError(state.ErrorTextBlock);
        }

        private void ValidateNumberAttribute(AttributeInputState state)
        {
            var numberBox = state.InputControl as NumberBox;
            var text = numberBox?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                if (state.IsRequired)
                {
                    ShowAttributeRequiredError(state);
                    state.IsValid = false;
                }
                else
                {
                    HideError(state.ErrorTextBlock);
                    state.IsValid = true;
                }
                return;
            }

            if (!double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                ShowError(state.ErrorTextBlock, _resourceLoader.GetString("ProductAttributeInvalidNumberError"));
                state.IsValid = false;
                return;
            }

            state.IsValid = true;
            HideError(state.ErrorTextBlock);
        }

        private void ValidateDateAttribute(AttributeInputState state)
        {
            var datePicker = state.InputControl as DatePicker;
            var hasValue = datePicker?.Date != null;

            if (state.IsRequired && !hasValue)
            {
                ShowAttributeRequiredError(state);
                state.IsValid = false;
                return;
            }

            state.IsValid = true;
            HideError(state.ErrorTextBlock);
        }

        private void ValidateBooleanAttribute(AttributeInputState state)
        {
            // Boolean toggle always has a value; treat as valid even when required
            state.IsValid = true;
            HideError(state.ErrorTextBlock);
        }

        private void ShowAttributeRequiredError(AttributeInputState state)
        {
            ShowError(state.ErrorTextBlock, string.Format(
                _resourceLoader.GetString("ProductAttributeRequiredError"),
                state.Attribute?.Name ?? string.Empty));
        }

        #endregion

        #region UI Helpers

        private void PopulateForm(ProductDetailViewModel? detail)
        {
            ResetValidationStates();
            HideAllErrors();
            HideInfoBars();

            if (detail == null || _currentMode == DialogMode.Add)
            {
                _currentProductId = 0;
                _currentSerialCount = 0;
                _currentCreatedAt = DateTime.UtcNow;

                SkuTextBox.Text = string.Empty;
                NameTextBox.Text = string.Empty;
                CategoryComboBox.SelectedIndex = -1;
                BrandComboBox.SelectedIndex = -1;
                PriceNumberBox.Value = 0;
                CostNumberBox.Value = 0;
                WarrantyNumberBox.Value = 12;
                SerialTrackedToggle.IsOn = false;
                StatusComboBox.SelectedValue = ProductStatus.ACTIVE;

                RenderAttributeInputs(null);
                SerialSummaryPanel.Visibility = Visibility.Collapsed;
                return;
            }

            var product = detail.Product;
            _currentProductId = product.Id;
            _currentSerialCount = detail.Serials?.Count ?? 0;
            _currentCreatedAt = product.CreatedAt;

            SkuTextBox.Text = product.Sku;
            NameTextBox.Text = product.Name;
            CategoryComboBox.SelectedValue = product.CategoryId;
            if (product.BrandId.HasValue)
            {
                BrandComboBox.SelectedValue = product.BrandId.Value;
            }
            else
            {
                BrandComboBox.SelectedIndex = -1;
            }
            PriceNumberBox.Value = Convert.ToDouble(product.Price);
            CostNumberBox.Value = Convert.ToDouble(product.Cost);
            WarrantyNumberBox.Value = product.WarrantyMonths;
            SerialTrackedToggle.IsOn = product.IsSerialTracked;
            StatusComboBox.SelectedValue = product.Status;

            RenderAttributeInputs(detail.AttributeValues);
            UpdateSerialSummaryVisibility();

            ValidateAllFields();
        }

        private void UpdateSerialSummaryVisibility()
        {
            if (_currentProductId <= 0 || !SerialTrackedToggle.IsOn)
            {
                _currentSerialCount = 0;
                SerialSummaryPanel.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var count = _serialRepository.GetByProductId(_currentProductId)?.Count() ?? 0;
                var format = _resourceLoader.GetString("ProductSerialSummaryFormat");
                if (string.IsNullOrWhiteSpace(format))
                {
                    format = "Serials in stock: {0}";
                }

                _currentSerialCount = count;
                SerialSummaryText.Text = string.Format(format, count);
                ViewSerialsButton.Content = _resourceLoader.GetString("ProductSerialsButtonText");
                SerialSummaryPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load serial summary", ex);
                _currentSerialCount = 0;
                SerialSummaryPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            SetInputsEnabled(!isLoading && _currentMode != DialogMode.View);
        }

        private void SetInputsEnabled(bool enabled)
        {
            SkuTextBox.IsEnabled = enabled;
            NameTextBox.IsEnabled = enabled;
            CategoryComboBox.IsEnabled = enabled;
            BrandComboBox.IsEnabled = enabled;
            PriceNumberBox.IsEnabled = enabled;
            CostNumberBox.IsEnabled = enabled;
            WarrantyNumberBox.IsEnabled = enabled;
            SerialTrackedToggle.IsEnabled = enabled;
            StatusComboBox.IsEnabled = enabled;
            var hasSerialSummary = SerialSummaryPanel.Visibility == Visibility.Visible;
            ViewSerialsButton.IsEnabled = (enabled || _currentMode == DialogMode.View) && _currentProductId > 0 && hasSerialSummary;

            foreach (var attribute in _attributeInputs)
            {
                attribute.InputControl.IsEnabled = enabled;
            }
        }

        private void UpdateUIForMode()
        {
            bool isEditable = _currentMode != DialogMode.View;
            SetInputsEnabled(isEditable);

            if (!isEditable)
            {
                HideAllErrors();
                HideInfoBars();
            }
        }

        private void HideInfoBars()
        {
            ErrorInfoBar.IsOpen = false;
            SuccessInfoBar.IsOpen = false;
        }

        private void RenderAttributeInputs(IEnumerable<ProductAttributeValueViewModel>? values)
        {
            AttributesPanel.Children.Clear();
            _attributeInputs.Clear();

            var valueLookup = values?.ToDictionary(v => v.AttributeId) ?? new Dictionary<int, ProductAttributeValueViewModel>();

            foreach (var attribute in _attributeDefinitions)
            {
                var isRequired = IsAttributeRequired(attribute);
                var container = new StackPanel();

                var label = new TextBlock
                {
                    Style = (Style)Resources["InputLabelStyle"],
                    Text = BuildAttributeLabel(attribute.Name, isRequired)
                };

                container.Children.Add(label);

                valueLookup.TryGetValue(attribute.Id, out var existingValue);
                FrameworkElement inputControl = CreateAttributeInputControl(attribute, existingValue);
                container.Children.Add(inputControl);

                var errorText = new TextBlock
                {
                    Style = (Style)Resources["ValidationErrorStyle"]
                };
                container.Children.Add(errorText);

                AttributesPanel.Children.Add(container);

                var state = new AttributeInputState
                {
                    Attribute = attribute,
                    InputControl = inputControl,
                    ErrorTextBlock = errorText,
                    IsRequired = isRequired,
                    IsValid = !isRequired
                };

                AttachAttributeValidationHandlers(state);
                ValidateAttribute(state);

                _attributeInputs.Add(state);
            }
        }

        private FrameworkElement CreateAttributeInputControl(ProductAttribute attribute, ProductAttributeValueViewModel? existing)
        {
            switch (attribute.DataType)
            {
                case AttributeDataType.NUMBER:
                    var numberBox = new NumberBox
                    {
                        Style = (Style)Resources["NumberInputStyle"],
                        Minimum = double.MinValue,
                        Maximum = double.MaxValue,
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                        Margin = new Thickness(0, 4, 0, 8)
                    };
                    numberBox.Value = existing?.ValueNumber.HasValue == true
                        ? Convert.ToDouble(existing.ValueNumber.Value)
                        : double.NaN;
                    return numberBox;

                case AttributeDataType.DATE:
                    var datePicker = new DatePicker
                    {
                        Height = 40,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        CornerRadius = new CornerRadius(6),
                        Margin = new Thickness(0, 4, 0, 8)
                    };
                    if (existing?.ValueDate.HasValue == true)
                    {
                        datePicker.Date = new DateTimeOffset(existing.ValueDate.Value);
                    }
                    return datePicker;

                case AttributeDataType.BOOLEAN:
                    var toggle = new ToggleSwitch
                    {
                        IsOn = existing?.ValueBool ?? false,
                        Margin = new Thickness(0, 4, 0, 8)
                    };
                    return toggle;

                case AttributeDataType.TEXT:
                default:
                    var textBox = new TextBox
                    {
                        Style = (Style)Resources["TextInputStyle"],
                        Text = existing?.ValueText ?? string.Empty
                    };
                    return textBox;
            }
        }

        private void AttachAttributeValidationHandlers(AttributeInputState state)
        {
            switch (state.Attribute.DataType)
            {
                case AttributeDataType.TEXT:
                    if (state.InputControl is TextBox textBox)
                    {
                        textBox.TextChanged += (_, _) => ValidateAttribute(state);
                    }
                    break;
                case AttributeDataType.NUMBER:
                    if (state.InputControl is NumberBox numberBox)
                    {
                        numberBox.ValueChanged += (_, _) => ValidateAttribute(state);
                    }
                    break;
                case AttributeDataType.DATE:
                    if (state.InputControl is DatePicker datePicker)
                    {
                        datePicker.DateChanged += (_, _) => ValidateAttribute(state);
                    }
                    break;
                case AttributeDataType.BOOLEAN:
                    if (state.InputControl is ToggleSwitch toggleSwitch)
                    {
                        toggleSwitch.Toggled += (_, _) => ValidateAttribute(state);
                    }
                    break;
            }
        }

        private string BuildAttributeLabel(string name, bool isRequired)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = _resourceLoader.GetString("UnknownAttributeLabel");
            }

            return isRequired ? string.Format(CultureInfo.InvariantCulture, "{0} *", name) : name;
        }

        private bool IsAttributeRequired(ProductAttribute attribute)
        {
            if (attribute == null)
            {
                return false;
            }

            var note = attribute.Note;
            if (string.IsNullOrWhiteSpace(note))
            {
                return false;
            }

            return note.Contains("required", StringComparison.OrdinalIgnoreCase);
        }

        private void HideAllErrors()
        {
            HideError(SkuError);
            HideError(NameError);
            HideError(CategoryError);
            HideError(BrandError);
            HideError(PriceError);
            HideError(CostError);
            HideError(WarrantyError);
            HideError(StatusError);

            foreach (var attribute in _attributeInputs)
            {
                HideError(attribute.ErrorTextBlock);
            }
        }

        private void ResetValidationStates()
        {
            _isSkuValid = false;
            _isNameValid = false;
            _isCategoryValid = false;
            _isPriceValid = false;
            _isCostValid = false;
            _isWarrantyValid = false;
            _isStatusValid = false;

            foreach (var attribute in _attributeInputs)
            {
                attribute.IsValid = !attribute.IsRequired;
            }
        }

        private void ShowError(TextBlock errorTextBlock, string? message)
        {
            if (errorTextBlock == null)
            {
                return;
            }

            errorTextBlock.Text = message ?? string.Empty;
            errorTextBlock.Visibility = string.IsNullOrWhiteSpace(message)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void HideError(TextBlock errorTextBlock)
        {
            if (errorTextBlock == null)
            {
                return;
            }

            errorTextBlock.Text = string.Empty;
            errorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void EnsureReferenceDataLoaded()
        {
            if (_referenceDataLoaded)
            {
                return;
            }

            try
            {
                LoadCategories();
                LoadBrands();
                LoadStatuses();
                LoadAttributes();

                _referenceDataLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load reference data for ProductDialog", ex);
                ErrorInfoBar.Message = _resourceLoader.GetString("ProductReferenceDataErrorMessage");
                ErrorInfoBar.IsOpen = true;
            }
        }

        private void LoadCategories()
        {
            _categoryOptions.Clear();
            var categories = _categoryRepository.GetAll() ?? Enumerable.Empty<ProductCategory>();

            foreach (var category in categories.OrderBy(c => c.Name ?? string.Empty))
            {
                var displayName = string.IsNullOrWhiteSpace(category.Name)
                    ? _resourceLoader.GetString("UnknownCategoryLabel")
                    : category.Name!;

                _categoryOptions.Add(new SelectionOption<int>(displayName, category.Id));
            }

            CategoryComboBox.ItemsSource = null;
            CategoryComboBox.ItemsSource = _categoryOptions;
            CategoryComboBox.SelectedIndex = -1;
        }

        private void LoadBrands()
        {
            _brandOptions.Clear();
            _brandOptions.Add(new SelectionOption<int?>(_resourceLoader.GetString("FilterOptionAll"), null));

            var brands = _brandService.GetAll() ?? Enumerable.Empty<Brand>();
            foreach (var brand in brands.OrderBy(b => b.Name ?? string.Empty))
            {
                var displayName = string.IsNullOrWhiteSpace(brand.Name)
                    ? _resourceLoader.GetString("UnknownBrandLabel")
                    : brand.Name!;

                _brandOptions.Add(new SelectionOption<int?>(displayName, brand.Id));
            }

            BrandComboBox.ItemsSource = null;
            BrandComboBox.ItemsSource = _brandOptions;
            BrandComboBox.SelectedIndex = -1;
        }

        private void LoadStatuses()
        {
            _statusOptions.Clear();

            foreach (ProductStatus status in Enum.GetValues(typeof(ProductStatus)))
            {
                var labelKey = status switch
                {
                    ProductStatus.ACTIVE => "Status_Active",
                    ProductStatus.INACTIVE => "Status_Inactive",
                    ProductStatus.DISCONTINUED => "Product_Status_Discontinued",
                    _ => status.ToString()
                };

                var label = _resourceLoader.GetString(labelKey);
                if (string.IsNullOrWhiteSpace(label))
                {
                    label = status.ToString();
                }

                _statusOptions.Add(new SelectionOption<ProductStatus>(label, status));
            }

            StatusComboBox.ItemsSource = null;
            StatusComboBox.ItemsSource = _statusOptions;
            StatusComboBox.SelectedValue = ProductStatus.ACTIVE;
        }

        private void LoadAttributes()
        {
            _attributeDefinitions = _attributeRepository.GetAll()?.OrderBy(a => a.Name).ToList()
                ?? new List<ProductAttribute>();
        }

        private Product BuildProductFromForm()
        {
            var product = new Product
            {
                Id = _currentProductId,
                Sku = (SkuTextBox.Text ?? string.Empty).Trim(),
                Name = (NameTextBox.Text ?? string.Empty).Trim(),
                CategoryId = CategoryComboBox.SelectedValue is int categoryId ? categoryId : 0,
                BrandId = GetSelectedBrandId(),
                Price = ConvertToDecimal(PriceNumberBox.Value),
                Cost = ConvertToDecimal(CostNumberBox.Value),
                WarrantyMonths = ConvertToWarrantyMonths(WarrantyNumberBox.Value),
                IsSerialTracked = SerialTrackedToggle.IsOn,
                Status = StatusComboBox.SelectedValue is ProductStatus status ? status : ProductStatus.ACTIVE,
                CreatedAt = _currentCreatedAt == default ? DateTime.UtcNow : _currentCreatedAt
            };

            if (_currentMode == DialogMode.Add)
            {
                product.CreatedAt = DateTime.UtcNow;
            }

            return product;
        }

        private IEnumerable<ProductAttributeValueInput> BuildAttributeInputs()
        {
            foreach (var state in _attributeInputs)
            {
                var input = new ProductAttributeValueInput
                {
                    AttributeId = state.Attribute.Id
                };

                switch (state.Attribute.DataType)
                {
                    case AttributeDataType.TEXT:
                        var text = (state.InputControl as TextBox)?.Text?.Trim();
                        input.TextValue = string.IsNullOrWhiteSpace(text) ? null : text;
                        input.RawValue = text;
                        break;
                    case AttributeDataType.NUMBER:
                        var numberText = (state.InputControl as NumberBox)?.Text?.Trim();
                        if (string.IsNullOrWhiteSpace(numberText))
                        {
                            input.NumberValue = null;
                            input.RawValue = numberText;
                        }
                        else if (decimal.TryParse(numberText, NumberStyles.Any, CultureInfo.InvariantCulture, out var numberValue))
                        {
                            input.NumberValue = numberValue;
                            input.RawValue = numberText;
                        }
                        else
                        {
                            input.RawValue = numberText;
                        }
                        break;
                    case AttributeDataType.DATE:
                        var date = (state.InputControl as DatePicker)?.Date;
                        input.DateValue = date?.DateTime;
                        input.RawValue = date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        break;
                    case AttributeDataType.BOOLEAN:
                        var toggle = state.InputControl as ToggleSwitch;
                        input.BoolValue = toggle?.IsOn;
                        input.RawValue = toggle?.IsOn.ToString();
                        break;
                }

                yield return input;
            }
        }

        private ProductDetailViewModel BuildFallbackDetail(Product product, IEnumerable<ProductAttributeValueInput> attributeInputs)
        {
            var categoryName = _categoryOptions.FirstOrDefault(c => c.Value == product.CategoryId)?.DisplayName ?? string.Empty;
            var brandName = product.BrandId.HasValue
                ? _brandOptions.FirstOrDefault(b => b.Value == product.BrandId)?.DisplayName
                : null;

            var productViewModel = new ProductListItemViewModel(product, categoryName, brandName, _currentSerialCount);

            var attributeLookup = _attributeDefinitions.ToDictionary(a => a.Id);
            var attributeViewModels = attributeInputs.Select(input =>
            {
                attributeLookup.TryGetValue(input.AttributeId, out var attribute);
                return new ProductAttributeValueViewModel
                {
                    AttributeId = input.AttributeId,
                    AttributeName = attribute?.Name ?? string.Empty,
                    DataType = attribute?.DataType ?? AttributeDataType.TEXT,
                    ValueText = input.TextValue,
                    ValueNumber = input.NumberValue,
                    ValueDate = input.DateValue,
                    ValueBool = input.BoolValue
                };
            });

            return new ProductDetailViewModel(productViewModel, attributeViewModels, Array.Empty<ProductSerialViewModel>());
        }

        private int? GetSelectedBrandId()
        {
            var selected = BrandComboBox.SelectedValue;
            return selected switch
            {
                int value => value,
                int? value => value,
                _ => null
            };
        }

        private decimal ConvertToDecimal(double value)
        {
            if (double.IsNaN(value))
            {
                return 0m;
            }

            return Convert.ToDecimal(value);
        }

        private int ConvertToWarrantyMonths(double value)
        {
            if (double.IsNaN(value))
            {
                return 0;
            }

            var clamped = Math.Max(0, Math.Min(240, value));
            return Convert.ToInt32(Math.Round(clamped, MidpointRounding.AwayFromZero));
        }

        #endregion
    }
}
