using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PhoneStore.Services.Interfaces;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class InvoiceDialog : ContentDialog
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPersonService _personService;
        private readonly IPromotionCodeService _promotionService;
        private readonly ResourceLoader _resourceLoader;
       
        private bool _isTotalValid = false;
        private bool _isPaymentValid = false;
        private bool _isPersonValid = false;
        private bool _isCreatedByValid = false;
        private bool _isStatusValid = false;

        public event EventHandler<InvoiceViewModel>? InvoiceSaved;
        public event EventHandler? DialogClosed;

        public enum DialogMode { Add, Edit, View }
        private DialogMode _currentMode = DialogMode.Add;
        private int _editingInvoiceId;

        public InvoiceDialog()
        {
            InitializeComponent();
            _invoiceService = ServiceContainer.GetService<IInvoiceService>();
            _personService = ServiceContainer.GetService<IPersonService>();
            _promotionService = ServiceContainer.GetService<IPromotionCodeService>();
            _resourceLoader = new ResourceLoader();

            // Hiển thị nút Save / Cancel
            this.PrimaryButtonText = _resourceLoader.GetString("DialogAddInvoice") ?? "Save";
            this.CloseButtonText = _resourceLoader.GetString("DialogCancelInvoice") ?? "Cancel";
            this.IsPrimaryButtonEnabled = false; // ban đầu disabled, sẽ bật khi validate
            this.SecondaryButtonText = "Reset";

            // Xử lý event
            this.PrimaryButtonClick += (s, e) => Save();
            this.CloseButtonClick += (s, e) => Cancel();
            this.SecondaryButtonClick += (s, e) =>
            {
                Reset();     // đặt lại tất cả giá trị
                e.Cancel = true;  // giữ dialog mở
            };
        }

        private async void InvoiceDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            // Gỡ handler để đảm bảo chỉ chạy 1 lần
            this.Loaded -= InvoiceDialog_Loaded;

            try
            {
                await InitializeDialogAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeDialogAsync failed: {ex}");
                ErrorInfoBar.Message = $"Lỗi khi khởi tạo dialog: {ex.Message}";
                ErrorInfoBar.IsOpen = true;
            }
        }

        private async Task InitializeDialogAsync()
        {
            ResetValidationStates();
            HideAllErrors();

            ShowLoading(true);
            try
            {
                await LoadComboBoxData();
                // Validate ngay sau khi dữ liệu load xong
                ValidateTotalAmount();
                ValidatePaymentMethod();
                ValidateCreator();
                ValidatePerson();
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = $"Error initializing dialog: {ex.Message}";
                ErrorInfoBar.IsOpen = true;
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task LoadComboBoxData()
        {
            try
            {
                var persons = await _personService.GetAllAsync();
                var promos = _promotionService.GetAll();
                void SetItems()
                {
                    PersonComboBox.ItemsSource = persons;
                    PromotionComboBox.ItemsSource = promos;
                    CreatedByComboBox.ItemsSource = persons;
                    PaymentMethodComboBox.ItemsSource = Enum.GetValues(typeof(PaymentMethod)).Cast<PaymentMethod>().ToList();
                }
                if (!this.DispatcherQueue.HasThreadAccess)
                    this.DispatcherQueue.TryEnqueue(SetItems);
                else
                    SetItems();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ErrorInfoBar.Message = $"Error loading data: {ex.Message}";
                ErrorInfoBar.IsOpen = true;
            }
        }

        private void PromotionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PromotionComboBox.SelectedItem is PromotionCode promo)
            {
                DiscountAmountTextBox.Text = promo.DiscountAmount.ToString("0.00");

                if (decimal.TryParse(TotalAmountTextBox.Text, out decimal total))
                {
                    FinalAmountTextBox.Text = Math.Max(total - promo.DiscountAmount, 0).ToString("0.00");
                }
            }
            else
            {
                DiscountAmountTextBox.Text = "0.00";
            }
        }

        private void TotalAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(TotalAmountTextBox.Text, out decimal total))
            {
                decimal discount = 0;
                if (decimal.TryParse(DiscountAmountTextBox.Text, out decimal disc))
                    discount = disc;

                FinalAmountTextBox.Text = Math.Max(total - discount, 0).ToString("0.00");
            }

            ValidateTotalAmount(); // gọi hàm validate mới
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidatePaymentMethod();
        }

        private void PersonComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidatePerson();
        }

        private void CreatedByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateCreator();
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateStatus();
        }

        private void ValidateTotalAmount()
        {
            decimal total = 0;
            decimal discount = 0;

            // Lấy giá trị TotalAmount
            if (!decimal.TryParse(TotalAmountTextBox.Text, out total))
            {
                _isTotalValid = false;
                return;
            }

            // Lấy giá trị DiscountAmount
            if (!decimal.TryParse(DiscountAmountTextBox.Text, out discount))
            {
                discount = 0;
            }

            // Kiểm tra Total >= Discount
            if (total < discount)
            {
                _isTotalValid = false;
            }
            else
            {
                _isTotalValid = true;
                HideError(TotalAmountError);
            }

            // Cập nhật nút Save
            UpdateValidationUI();
        }

        private void ValidatePaymentMethod()
        {
            _isPaymentValid = PaymentMethodComboBox.SelectedItem != null;

            if (!_isPaymentValid)
            {
                ShowError(PaymentMethodError, "Please select payment method");
            }
            else
            {
                HideError(PaymentMethodError);
            }

            // Cập nhật trạng thái nút Save
            UpdateValidationUI();
        }

        private void ValidatePerson()
        {
            if (PersonComboBox.SelectedItem == null)
            {
                _isPersonValid = false;
            }
            else
            {
                _isPersonValid = true;
                HideError(PersonError);
            }
            UpdateValidationUI();
        }

        private void ValidateCreator()
        {
            if (CreatedByComboBox.SelectedItem == null)
            {
                _isCreatedByValid = false;
            }
            else
            {
                _isCreatedByValid = true;
                HideError(CreatedByError);
            }
            UpdateValidationUI();
        }

        private void ValidateStatus()
        {
            if (StatusComboBox.SelectedItem == null)
            {
                _isStatusValid = false;
            }
            else
            {
                _isStatusValid = true;
                HideError(StatusError);
            }
        }


        public bool IsValid()
        {
            ValidateAllFields();
            return _isPaymentValid;
        }

        private void ValidateAllFields()
        {
            ValidatePaymentMethod();
            ValidateTotalAmount();
            ValidatePerson();
            ValidateCreator();
            ValidateStatus();
        }

        private void UpdateValidationUI()
        {
            bool isFormValid = _isTotalValid && _isPaymentValid && _isCreatedByValid && _isPersonValid &&_isStatusValid;
            // Nếu InvoiceDialog kế thừa ContentDialog, có thuộc tính IsPrimaryButtonEnabled
            this.IsPrimaryButtonEnabled = isFormValid;
        }

        private void ShowError(TextBlock error, string message)
        {
            error.Text = message;
            error.Visibility = Visibility.Visible;
        }

        private void HideError(TextBlock error)
        {
            error.Visibility = Visibility.Collapsed;
        }

        private void HideAllErrors()
        {
            HideError(TotalAmountError);
            HideError(PaymentMethodError);
            ErrorInfoBar.IsOpen = false;
        }

        private void ResetValidationStates()
        {
            _isTotalValid = false;
            _isPaymentValid = false;
        }

        private void ShowLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            PersonComboBox.IsEnabled = !isLoading;
            PromotionComboBox.IsEnabled = !isLoading;
            TotalAmountTextBox.IsEnabled = !isLoading;
            PaymentMethodComboBox.IsEnabled = !isLoading;
            NoteTextBox.IsEnabled = !isLoading;
        }

        public void Reset()
        {
            // Đặt lại chế độ Add
            _currentMode = DialogMode.Add;

            // Reset các flag validate
            ResetValidationStates();

            // Ẩn tất cả lỗi
            HideAllErrors();

            // Đặt lại các giá trị trong UI
            PersonComboBox.SelectedIndex = -1;
            PromotionComboBox.SelectedIndex = -1;
            CreatedByComboBox.SelectedIndex = -1;
            PaymentMethodComboBox.SelectedIndex = -1;

            TotalAmountTextBox.Text = "";
            DiscountAmountTextBox.Text = "0.00";
            FinalAmountTextBox.Text = "0.00";
            NoteTextBox.Text = "";

            // Update trạng thái nút Save
            UpdateValidationUI();

            // Nếu có overlay loading, tắt nó
            ShowLoading(false);
        }

        public async Task SetMode(DialogMode mode, InvoiceViewModel? invoice = null)
        {
            if (mode == DialogMode.Edit && invoice != null)
            {
                _editingInvoiceId = invoice.Id;
            }
            _currentMode = mode;
            // Ẩn nút Reset nếu là Edit hoặc View
            if (mode == DialogMode.Edit || mode == DialogMode.View)
            {
                this.SecondaryButtonText = null;   // Ẩn hoàn toàn
            }
            else
            {
                this.SecondaryButtonText = "Reset"; // Hiện lại khi Add
            }

            await LoadComboBoxData();
            LoadData(invoice);
            UpdateUIForMode();
        }

        private void LoadData(InvoiceViewModel? invoice)
        {
            if (invoice == null || _currentMode == DialogMode.Add)
            {
                PersonComboBox.SelectedIndex = -1;
                PromotionComboBox.SelectedIndex = -1;
                CreatedByComboBox.SelectedIndex = -1;
                PaymentMethodComboBox.SelectedIndex = -1;

                TotalAmountTextBox.Text = "";
                DiscountAmountTextBox.Text = "0.00";
                FinalAmountTextBox.Text = "0.00";
                NoteTextBox.Text = "";
                return;
            }

            PersonComboBox.SelectedValue = invoice.PersonId;
            PromotionComboBox.SelectedValue = invoice.PromotionCodeId;
            CreatedByComboBox.SelectedValue = invoice.CreatedBy;
            TotalAmountTextBox.Text = invoice.TotalAmount.ToString("0.00");
            DiscountAmountTextBox.Text = invoice.DiscountAmount.ToString("0.00");
            FinalAmountTextBox.Text = invoice.FinalAmount.ToString("0.00");
            PaymentMethodComboBox.SelectedItem = invoice.PaymentMethod;
            NoteTextBox.Text = invoice.Note;
            InvoiceDatePicker.Date = new DateTimeOffset(invoice.InvoiceDate);
            StatusComboBox.SelectedIndex = (int)invoice.Status;

            _isTotalValid = true;
            _isPaymentValid = true;
        }

        public async void LoadById(int invoiceId)
        {
            try
            {
                ShowLoading(true);

                var invoice = _invoiceService.GetById(invoiceId);

                if (invoice == null)
                {
                    ErrorInfoBar.Message = "Không tìm thấy hóa đơn.";
                    ErrorInfoBar.IsOpen = true;
                    return;
                }

                var viewModel = new InvoiceViewModel(invoice);

                await SetMode(DialogMode.Edit, viewModel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ErrorInfoBar.Message = $"Lỗi khi tải dữ liệu hóa đơn: {ex.Message}";
                ErrorInfoBar.IsOpen = true;
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void UpdateUIForMode()
        {
            bool editable = _currentMode != DialogMode.View;

            PersonComboBox.IsEnabled = editable;
            PromotionComboBox.IsEnabled = editable;
            TotalAmountTextBox.IsReadOnly = !editable;
            PaymentMethodComboBox.IsEnabled = editable;
            NoteTextBox.IsReadOnly = !editable;
        }

        public void Save()
        {
            if (_currentMode == DialogMode.View)
            {
                DialogClosed?.Invoke(this, EventArgs.Empty);
                return;
            }

            ValidatePaymentMethod();
            UpdateValidationUI();

            if (!_isTotalValid || !_isPaymentValid)
                return;

            try
            {
                ShowLoading(true);

                var invoice = new Invoice
                {
                    Id = _editingInvoiceId,
                    PersonId = (int)PersonComboBox.SelectedValue,
                    PromotionCodeId = (int?)PromotionComboBox.SelectedValue,
                    CreatedBy = (int)CreatedByComboBox.SelectedValue,
                    InvoiceDate = DateTime.Now,
                    TotalAmount = decimal.Parse(TotalAmountTextBox.Text),
                    DiscountAmount = decimal.Parse(DiscountAmountTextBox.Text),
                    FinalAmount = decimal.Parse(FinalAmountTextBox.Text),
                    Note = NoteTextBox.Text
                };

                // PaymentMethod: SelectedItem là string (CASH, CARD, ...)
                if (PaymentMethodComboBox.SelectedItem is string pmString &&
                    Enum.TryParse<PaymentMethod>(pmString, out var pm))
                {
                    invoice.PaymentMethod = pm;
                }
                else
                {
                    invoice.PaymentMethod = PaymentMethod.CASH;
                }

                if (_currentMode == DialogMode.Add)
                    _invoiceService.Insert(invoice); // Remove await
                else
                    _invoiceService.Update(invoice); // Remove await

                var vm = new InvoiceViewModel(invoice);
                InvoiceSaved?.Invoke(this, vm);
                DialogClosed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = $"Save failed: {ex.Message}";
                ErrorInfoBar.IsOpen = true;
            }
            finally
            {
                ShowLoading(false);
            }
        }

        public void Cancel()
        {
            ResetValidationStates();
            HideAllErrors();
            DialogClosed?.Invoke(this, EventArgs.Empty);
        }

    }
}
