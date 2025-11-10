using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Helpers;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels.ProductAttributes;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ProductAttributesPage : Page, INotifyPropertyChanged
    {
        private const int PageSize = 20;

        private readonly IProductAttributeRepository _attributeRepository;
        private readonly IProductAttributeOptionService _optionService;
        private readonly ResourceLoader _resourceLoader;

        private bool _isInitialized;
        private string _searchText = string.Empty;
        private ProductAttributeViewModel? _selectedAttribute;
        private DataTypeFilterOption? _selectedFilterOption;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalRecords;
        private string _optionsHeaderText = string.Empty;
        private bool _isLoading;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ProductAttributeViewModel> Attributes { get; } = new();
        public ObservableCollection<ProductAttributeOptionViewModel> AttributeOptions { get; } = new();
        public ObservableCollection<DataTypeFilterOption> DataTypeFilters { get; } = new();

        public bool CanManageAttributes { get; }

        public bool HasSelectedAttribute => SelectedAttribute != null;

        public string OptionsHeaderText
        {
            get => _optionsHeaderText;
            private set => SetProperty(ref _optionsHeaderText, value);
        }

        public ProductAttributeViewModel? SelectedAttribute
        {
            get => _selectedAttribute;
            set
            {
                if (SetProperty(ref _selectedAttribute, value))
                {
                    OnPropertyChanged(nameof(HasSelectedAttribute));
                    UpdateOptionsHeader();
                    if (_isInitialized)
                    {
                        LoadOptionsForAttribute(value?.Id);
                    }
                }
            }
        }

        public DataTypeFilterOption? SelectedFilterOption
        {
            get => _selectedFilterOption;
            set
            {
                if (SetProperty(ref _selectedFilterOption, value) && _isInitialized && !_isLoading)
                {
                    CurrentPage = 1;
                    LoadAttributes();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        public ProductAttributesPage()
        {
            InitializeComponent();
            DataContext = this;

            _attributeRepository = App.GetService<IProductAttributeRepository>();
            _optionService = App.GetService<IProductAttributeOptionService>();
            _resourceLoader = new ResourceLoader();

            var session = UserSession.Instance;
            CanManageAttributes = session.HasPermission("PRODUCT_ATTRIBUTE_MANAGE");

            OptionsHeaderText = _resourceLoader.GetString("ProductAttributes_OptionsHeader.Text") ?? "Attribute options";

            InitializeDataTypeFilters();

            Loaded += ProductAttributesPage_Loaded;
        }

        private void ProductAttributesPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ProductAttributesPage_Loaded;
            _isInitialized = true;
            LoadAttributes();
        }

        private void InitializeDataTypeFilters()
        {
            DataTypeFilters.Clear();

            var allLabel = _resourceLoader.GetString("ProductAttributes_DataTypeFilter_All.Text") ?? "All types";
            DataTypeFilters.Add(new DataTypeFilterOption(null, allLabel));

            foreach (AttributeDataType dataType in Enum.GetValues(typeof(AttributeDataType)))
            {
                DataTypeFilters.Add(new DataTypeFilterOption(dataType, GetDataTypeLabel(dataType)));
            }

            SelectedFilterOption = DataTypeFilters.FirstOrDefault();
        }

        private void LoadAttributes()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_isLoading)
            {
                return;
            }

            try
            {
                _isLoading = true;

                var previousSelectedId = SelectedAttribute?.Id;
                var nameFilter = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText;
                var dataTypeFilter = SelectedFilterOption?.Value;

                var attributes = _attributeRepository
                    .GetAttributesFiltered(nameFilter, dataTypeFilter, CurrentPage, PageSize)
                    .ToList();

                TotalRecords = _attributeRepository.GetTotalRecords(nameFilter, dataTypeFilter);
                var totalPages = _attributeRepository.GetTotalPages(nameFilter, dataTypeFilter, PageSize);
                TotalPages = Math.Max(1, totalPages);

                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                    attributes = _attributeRepository
                        .GetAttributesFiltered(nameFilter, dataTypeFilter, CurrentPage, PageSize)
                        .ToList();
                }

                Attributes.Clear();

                foreach (var attribute in attributes)
                {
                    IReadOnlyList<ProductAttributeOption> options;
                    try
                    {
                        options = _optionService.GetEditableOptionsForAttribute(attribute.Id);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to load options for attribute {attribute.Id}", ex);
                        options = Array.Empty<ProductAttributeOption>();
                    }

                    Attributes.Add(new ProductAttributeViewModel(attribute, options.Count));
                }

                UpdatePaginationUi();
                UpdateEmptyStateVisibility();

                ProductAttributeViewModel? selection = null;
                if (previousSelectedId.HasValue)
                {
                    selection = Attributes.FirstOrDefault(a => a.Id == previousSelectedId.Value);
                }

                if (selection == null && Attributes.Count > 0)
                {
                    selection = Attributes.First();
                }

                if (Attributes.Count == 0)
                {
                    SelectedAttribute = null;
                    AttributeOptions.Clear();
                }
                else
                {
                    SelectedAttribute = selection;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load product attributes", ex);
                _ = ShowErrorAsync(
                    LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                    string.Format(
                        _resourceLoader.GetString("ProductAttributes_LoadError.Text") ??
                        "We couldn't load product attributes. {0}",
                        ex.Message));
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadOptionsForAttribute(int? attributeId)
        {
            AttributeOptions.Clear();

            if (!attributeId.HasValue || attributeId.Value <= 0)
            {
                if (OptionsEmptyState != null)
                {
                    OptionsEmptyState.Visibility = Visibility.Visible;
                }

                if (OptionRecordSummaryTextBlock != null)
                {
                    OptionRecordSummaryTextBlock.Text = string.Empty;
                }

                return;
            }

            try
            {
                var options = _optionService
                    .GetEditableOptionsForAttribute(attributeId.Value)
                    .OrderBy(option => option.SortOrder)
                    .ThenBy(option => option.DisplayValue)
                    .ToList();

                foreach (var option in options)
                {
                    AttributeOptions.Add(new ProductAttributeOptionViewModel(option));
                }

                if (OptionsEmptyState != null)
                {
                    OptionsEmptyState.Visibility = AttributeOptions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }

                if (SelectedAttribute != null)
                {
                    SelectedAttribute.OptionCount = AttributeOptions.Count;
                }

                if (OptionRecordSummaryTextBlock != null)
                {
                    OptionRecordSummaryTextBlock.Text = string.Format(
                        _resourceLoader.GetString("ProductAttributes_OptionsRecordSummaryFormat.Text") ?? "{0} of {1} options",
                        AttributeOptions.Count,
                        AttributeOptions.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load attribute options", ex);

                if (OptionsEmptyState != null)
                {
                    OptionsEmptyState.Visibility = Visibility.Visible;
                }

                if (OptionRecordSummaryTextBlock != null)
                {
                    OptionRecordSummaryTextBlock.Text = string.Empty;
                }

                _ = ShowErrorAsync(
                    LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                    string.Format(
                        _resourceLoader.GetString("ProductAttributes_OptionLoadError.Text") ??
                        "Unable to load attribute options. {0}",
                        ex.Message));
            }
        }

        private async System.Threading.Tasks.Task ShowErrorAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = _resourceLoader.GetString("DialogCloseButton") ?? "Close",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void UpdateOptionsHeader()
        {
            var header = _resourceLoader.GetString("ProductAttributes_OptionsHeader.Text") ?? "Attribute options";
            if (SelectedAttribute != null)
            {
                var format = _resourceLoader.GetString("ProductAttributes_OptionsHeaderFormat.Text");
                if (!string.IsNullOrWhiteSpace(format))
                {
                    header = string.Format(format, SelectedAttribute.Name);
                }
            }

            OptionsHeaderText = header;
        }

        private void UpdatePaginationUi()
        {
            if (PageInfoTextBlock != null)
            {
                var pageFormat = _resourceLoader.GetString("ProductAttributes_PageInfoFormat.Text") ?? "Page {0} of {1}";
                PageInfoTextBlock.Text = string.Format(pageFormat, CurrentPage, TotalPages);
            }

            if (RecordSummaryTextBlock != null)
            {
                var summaryFormat = _resourceLoader.GetString("ProductAttributes_RecordSummaryFormat.Text") ?? "{0} of {1} attributes";
                RecordSummaryTextBlock.Text = string.Format(summaryFormat, Attributes.Count, TotalRecords);
            }

            if (PreviousPageButton != null)
            {
                PreviousPageButton.IsEnabled = CurrentPage > 1;
            }

            if (NextPageButton != null)
            {
                NextPageButton.IsEnabled = CurrentPage < TotalPages;
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
            {
                EmptyStatePanel.Visibility = Attributes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (AttributesListView != null)
            {
                AttributesListView.Visibility = Attributes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private string GetDataTypeLabel(AttributeDataType dataType)
        {
            var key = dataType switch
            {
                AttributeDataType.TEXT => "AttributeDataType_Text.Text",
                AttributeDataType.NUMBER => "AttributeDataType_Number.Text",
                AttributeDataType.DATE => "AttributeDataType_Date.Text",
                AttributeDataType.BOOLEAN => "AttributeDataType_Boolean.Text",
                _ => "AttributeDataType_Text.Text"
            };

            return LocalizationHelper.GetString(key) ?? dataType.ToString();
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_isLoading)
            {
                return;
            }

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                _searchText = sender.Text?.Trim() ?? string.Empty;
                CurrentPage = 1;
                LoadAttributes();
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            var previousLoading = _isLoading;
            _isLoading = true;

            if (SearchBox != null)
            {
                SearchBox.Text = string.Empty;
            }

            SelectedFilterOption = DataTypeFilters.FirstOrDefault();

            _isLoading = previousLoading;

            CurrentPage = 1;
            LoadAttributes();
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadAttributes();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadAttributes();
            }
        }

        private async void AddAttributeButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowAttributeDialogAsync(null);
        }

        private async void EditAttributeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAttribute != null)
            {
                await ShowAttributeDialogAsync(SelectedAttribute);
            }
        }

        private async void DeleteAttributeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAttribute == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("ProductAttributes_DeleteConfirmTitle.Text") ?? "Delete attribute",
                Content = string.Format(
                    _resourceLoader.GetString("ProductAttributes_DeleteConfirmMessage.Text") ??
                    "Are you sure you want to delete the attribute \"{0}\"?",
                    SelectedAttribute.Name),
                PrimaryButtonText = _resourceLoader.GetString("DialogDelete") ?? "Delete",
                CloseButtonText = _resourceLoader.GetString("DialogCancel") ?? "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                _attributeRepository.Delete(SelectedAttribute.Id);

                if (Attributes.Count <= 1 && CurrentPage > 1)
                {
                    CurrentPage--;
                }

                LoadAttributes();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
                    LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                    string.Format(
                        _resourceLoader.GetString("ProductAttributes_DeleteError.Text") ??
                        "Unable to delete the attribute. {0}",
                        ex.Message));
            }
        }

        private async System.Threading.Tasks.Task ShowAttributeDialogAsync(ProductAttributeViewModel? existing)
        {
            var isEdit = existing != null;
            var nameBox = new TextBox
            {
                Text = existing?.Name ?? string.Empty,
                Header = _resourceLoader.GetString("ProductAttributes_AttributeNameLabel.Text") ?? "Name"
            };

            var dataTypeCombo = new ComboBox
            {
                Header = _resourceLoader.GetString("ProductAttributes_AttributeDataTypeLabel.Text") ?? "Data type",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            foreach (AttributeDataType type in Enum.GetValues(typeof(AttributeDataType)))
            {
                dataTypeCombo.Items.Add(new ComboBoxItem
                {
                    Content = GetDataTypeLabel(type),
                    Tag = type
                });
            }

            var selectedType = existing?.DataType ?? AttributeDataType.TEXT;
            foreach (ComboBoxItem item in dataTypeCombo.Items)
            {
                if (item.Tag is AttributeDataType dataType && dataType == selectedType)
                {
                    dataTypeCombo.SelectedItem = item;
                    break;
                }
            }

            var noteBox = new TextBox
            {
                Text = existing?.Note ?? string.Empty,
                Header = _resourceLoader.GetString("ProductAttributes_AttributeNoteLabel.Text") ?? "Description",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };

            var errorText = new TextBlock
            {
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Application.Current.Resources.TryGetValue("BrushError", out var brush)
                    ? brush as Brush
                    : null
            };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(nameBox);
            panel.Children.Add(dataTypeCombo);
            panel.Children.Add(noteBox);
            panel.Children.Add(errorText);

            ProductAttribute? savedAttribute = null;

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString(isEdit
                    ? "ProductAttributes_EditDialogTitle.Text"
                    : "ProductAttributes_CreateDialogTitle.Text") ?? (isEdit ? "Edit attribute" : "Create attribute"),
                Content = panel,
                PrimaryButtonText = _resourceLoader.GetString(isEdit ? "DialogUpdate" : "DialogAdd") ?? (isEdit ? "Save" : "Add"),
                CloseButtonText = _resourceLoader.GetString("DialogCancel") ?? "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                errorText.Visibility = Visibility.Collapsed;
                errorText.Text = string.Empty;

                var trimmedName = nameBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(trimmedName))
                {
                    errorText.Text = _resourceLoader.GetString("ProductAttributes_ValidationNameRequired.Text") ?? "Name is required.";
                    errorText.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }

                var selectedItem = dataTypeCombo.SelectedItem as ComboBoxItem;
                var dataType = selectedItem?.Tag is AttributeDataType enumValue ? enumValue : AttributeDataType.TEXT;
                var note = string.IsNullOrWhiteSpace(noteBox.Text) ? null : noteBox.Text.Trim();

                try
                {
                    var existingAttributes = _attributeRepository.GetByNames(new[] { trimmedName });
                    if (existingAttributes.TryGetValue(trimmedName, out var duplicate) && (!isEdit || duplicate.Id != existing?.Id))
                    {
                        errorText.Text = _resourceLoader.GetString("ProductAttributes_ValidationDuplicateName.Text") ??
                            "An attribute with this name already exists.";
                        errorText.Visibility = Visibility.Visible;
                        args.Cancel = true;
                        return;
                    }

                    ProductAttribute attribute;
                    if (isEdit)
                    {
                        attribute = _attributeRepository.GetById(existing!.Id);
                        attribute.Name = trimmedName;
                        attribute.DataType = dataType;
                        attribute.Note = note;
                        _attributeRepository.Update(attribute);
                    }
                    else
                    {
                        attribute = new ProductAttribute
                        {
                            Name = trimmedName,
                            DataType = dataType,
                            Note = note
                        };
                        _attributeRepository.Insert(attribute);
                    }

                    savedAttribute = attribute;
                }
                catch (Exception ex)
                {
                    errorText.Text = string.Format(
                        _resourceLoader.GetString("ProductAttributes_SaveError.Text") ??
                        "Unable to save the attribute. {0}",
                        ex.Message);
                    errorText.Visibility = Visibility.Visible;
                    args.Cancel = true;
                }
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && savedAttribute != null)
            {
                LoadAttributes();
                SelectedAttribute = Attributes.FirstOrDefault(a => a.Id == savedAttribute.Id) ?? SelectedAttribute;
            }
        }

        private async void AddOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAttribute == null)
            {
                return;
            }

            await ShowOptionDialogAsync(null);
        }

        private async void OptionEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int optionId)
            {
                var optionViewModel = AttributeOptions.FirstOrDefault(o => o.Id == optionId);
                if (optionViewModel != null)
                {
                    await ShowOptionDialogAsync(optionViewModel);
                }
            }
        }

        private async void OptionDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAttribute == null)
            {
                return;
            }

            if (sender is FrameworkElement element && element.Tag is int optionId)
            {
                var option = AttributeOptions.FirstOrDefault(o => o.Id == optionId);
                if (option == null)
                {
                    return;
                }

                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("ProductAttributes_DeleteOptionConfirmTitle.Text") ?? "Delete option",
                    Content = string.Format(
                        _resourceLoader.GetString("ProductAttributes_DeleteOptionConfirmMessage.Text") ??
                        "Are you sure you want to delete the option \"{0}\"?",
                        option.DisplayValue),
                    PrimaryButtonText = _resourceLoader.GetString("DialogDelete") ?? "Delete",
                    CloseButtonText = _resourceLoader.GetString("DialogCancel") ?? "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    return;
                }

                try
                {
                    _optionService.DeleteOption(optionId);
                    LoadOptionsForAttribute(SelectedAttribute.Id);
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync(
                        LocalizationHelper.GetString("ErrorTitle/Text") ?? "Error",
                        string.Format(
                            _resourceLoader.GetString("ProductAttributes_OptionDeleteError.Text") ??
                            "Unable to delete the option. {0}",
                            ex.Message));
                }
            }
        }

        private async System.Threading.Tasks.Task ShowOptionDialogAsync(ProductAttributeOptionViewModel? existing)
        {
            if (SelectedAttribute == null)
            {
                return;
            }

            var isEdit = existing != null;

            var displayBox = new TextBox
            {
                Text = existing?.DisplayValue ?? string.Empty,
                Header = _resourceLoader.GetString("ProductAttributes_OptionDisplayLabel.Text") ?? "Display value"
            };

            var normalizedBox = new TextBox
            {
                Text = existing?.NormalizedValue ?? string.Empty,
                Header = _resourceLoader.GetString("ProductAttributes_OptionNormalizedLabel.Text") ?? "Normalized value"
            };

            var sortNumberBox = new NumberBox
            {
                Value = existing?.SortOrder ?? 0,
                Header = _resourceLoader.GetString("ProductAttributes_OptionSortLabel.Text") ?? "Sort order",
                Minimum = 0,
                SmallChange = 1
            };

            var activeSwitch = new ToggleSwitch
            {
                Header = _resourceLoader.GetString("ProductAttributes_OptionIsActiveLabel.Text") ?? "Option is active",
                IsOn = existing?.IsActive ?? true
            };

            var errorText = new TextBlock
            {
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Application.Current.Resources.TryGetValue("BrushError", out var brush)
                    ? brush as Brush
                    : null
            };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(displayBox);
            panel.Children.Add(normalizedBox);
            panel.Children.Add(sortNumberBox);
            panel.Children.Add(activeSwitch);
            panel.Children.Add(errorText);

            ProductAttributeOption? savedOption = null;

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString(isEdit
                    ? "ProductAttributes_OptionDialogTitleEdit.Text"
                    : "ProductAttributes_OptionDialogTitleAdd.Text") ?? (isEdit ? "Edit option" : "Add option"),
                Content = panel,
                PrimaryButtonText = _resourceLoader.GetString(isEdit ? "DialogUpdate" : "DialogAdd") ?? (isEdit ? "Save" : "Add"),
                CloseButtonText = _resourceLoader.GetString("DialogCancel") ?? "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                errorText.Visibility = Visibility.Collapsed;
                errorText.Text = string.Empty;

                var displayValue = displayBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(displayValue))
                {
                    errorText.Text = _resourceLoader.GetString("ProductAttributes_ValidationOptionValueRequired.Text") ??
                        "Display value is required.";
                    errorText.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }

                var normalized = string.IsNullOrWhiteSpace(normalizedBox.Text) ? null : normalizedBox.Text.Trim();
                var sortOrder = (int)Math.Round(sortNumberBox.Value);
                var isActive = activeSwitch.IsOn;

                try
                {
                    if (isEdit)
                    {
                        var option = _optionService.GetOptionById(existing!.Id) ?? new ProductAttributeOption
                        {
                            Id = existing.Id,
                            AttributeId = SelectedAttribute.Id
                        };

                        option.DisplayValue = displayValue;
                        option.NormalizedValue = normalized;
                        option.SortOrder = sortOrder;
                        option.IsActive = isActive;
                        option.UpdatedAt = DateTime.UtcNow;

                        _optionService.UpdateOption(option);
                        savedOption = option;
                    }
                    else
                    {
                        var option = new ProductAttributeOption(SelectedAttribute.Id, displayValue)
                        {
                            NormalizedValue = normalized,
                            SortOrder = sortOrder,
                            IsActive = isActive,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        savedOption = _optionService.CreateOption(option);
                    }
                }
                catch (Exception ex)
                {
                    errorText.Text = string.Format(
                        _resourceLoader.GetString("ProductAttributes_OptionSaveError.Text") ??
                        "Unable to save the option. {0}",
                        ex.Message);
                    errorText.Visibility = Visibility.Visible;
                    args.Cancel = true;
                }
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && savedOption != null)
            {
                LoadOptionsForAttribute(SelectedAttribute?.Id);
            }
        }
    }
}
