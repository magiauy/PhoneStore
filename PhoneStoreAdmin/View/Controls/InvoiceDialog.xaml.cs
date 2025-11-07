using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Implementations;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public event EventHandler<InvoiceViewModel>? InvoiceSaved;
        public event EventHandler? DialogClosed;

        public enum DialogMode { Add, Edit, View }
        private DialogMode _currentMode = DialogMode.Add;

        public InvoiceDialog()
        {
            this.InitializeComponent();
            _invoiceService = ServiceContainer.GetService<IInvoiceService>();
            _personService = ServiceContainer.GetService<IPersonService>();
            _promotionService = ServiceContainer.GetService<IPromotionCodeService>();
            _resourceLoader = new ResourceLoader();
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            ResetValidationStates();
            HideAllErrors();
            LoadComboBoxData();
            UpdateValidationUI();
        }

        private async Task LoadComboBoxData()
        {
            try
            {
                var persons = await _personService.GetAllAsync();
                var promos = await _promotionService.GetAllPromotionCodesAsync();
                
                PersonComboBox.ItemsSource = persons;
                PromotionComboBox.ItemsSource = promos;
              
                PaymentMethodComboBox.ItemsSource = new List<string>
                {
                    "CASH", "CARD", "BANK", "EWALLET"
                };
            }
            catch (Exception ex)
            {
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
                _isTotalValid = total >= 0;

                decimal discount = 0;
                if (decimal.TryParse(DiscountAmountTextBox.Text, out decimal disc))
                    discount = disc;

                FinalAmountTextBox.Text = Math.Max(total - discount, 0).ToString("0.00");
            }
            else
            {
                _isTotalValid = false;
            }

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
        }

        private void UpdateValidationUI()
        {
            bool isFormValid = _isTotalValid && _isPaymentValid;
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
            SetMode(DialogMode.Add);
            ResetValidationStates();
            HideAllErrors();
            UpdateValidationUI();
            ShowLoading(false);
        }

        public async Task SetMode(DialogMode mode, InvoiceViewModel? invoice = null)
        {
            _currentMode = mode;
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

                SetMode(DialogMode.Edit, viewModel);
            }
            catch (Exception ex)
            {
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

        public async void Save()
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
                    PersonId = (int)PersonComboBox.SelectedValue,
                    PromotionCodeId = (int?)PromotionComboBox.SelectedValue,
                    CreatedBy = (int)CreatedByComboBox.SelectedValue,
                    InvoiceDate = DateTime.Now,
                    TotalAmount = decimal.Parse(TotalAmountTextBox.Text),
                    DiscountAmount = decimal.Parse(DiscountAmountTextBox.Text),
                    FinalAmount = decimal.Parse(FinalAmountTextBox.Text),
                    PaymentMethod = Enum.TryParse<PaymentMethod>(
                        (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        out var method)
                        ? method
                        : PaymentMethod.CASH,
                    Status = Enum.TryParse<InvoiceStatus>("Pending", out var status)
                        ? status
                        : InvoiceStatus.PAID, // hoặc giá trị mặc định

                    Note = NoteTextBox.Text
                };

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

        public bool IsValid()
        {
            ValidateAllFields();
            return _isPaymentValid;
        }

        private void ValidateAllFields()
        {
            ValidatePaymentMethod();
        }
       
    }
}
