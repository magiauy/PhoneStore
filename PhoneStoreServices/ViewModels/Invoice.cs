using System;
using System.Collections.Generic;
using System.Linq;
using PhoneStore.Services.Helpers;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStore.Services.ViewModels
{
    public class InvoiceViewModel
    {
        public int Id { get; set; }
        public int? PersonId { get; set; }
        public int? PromotionCodeId { get; set; }
        public int CreatedBy { get; set; }
        public DateTime InvoiceDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Note { get; set; }

        // Hiển thị phụ
        public string CustomerName { get; set; } = "Unknown";
        public string CreatedByName { get; set; } = "System";
        public string DiscountCode { get; set; } = "_";

        // Danh sách chi tiết hóa đơn
        public List<InvoiceLineViewModel> InvoiceLines { get; set; } = new();

        // Hiển thị màu và text trạng thái
        public string LocalizedStatusText =>
    LocalizationHelper.GetString($"InvoiceStatus_{Status.ToString().ToUpper()}");


        public string StatusColor => Status switch
        {
            InvoiceStatus.PENDING => "#6c757d", // Xám: Trạng thái chờ, chưa có hành động (Neutral)
            InvoiceStatus.UNPAID => "#ffc107", // Vàng: Cảnh báo, cần thanh toán (Warning)
            InvoiceStatus.PAID => "#17a2b8", // Xanh dương nhạt (Teal): Đã tiền, đang chờ xử lý tiếp (Info)
            InvoiceStatus.DELIVERING => "#007bff", // Xanh dương đậm: Đang di chuyển, đang xử lý (Primary)
            InvoiceStatus.COMPLETED => "#28a745", // Xanh lá: Thành công trọn vẹn (Success)
            InvoiceStatus.CANCELLED => "#dc3545", // Đỏ: Thất bại, hủy bỏ (Danger)
            _ => "#6c757d"
        };

        public List<Person> Customers { get; set; }
        public Person? SelectedCustomer { get; set; }

        public List<Person> Creator { get; set; }
        public Person? SelectedCreator { get; set; }

        public List<Promotion> Promotions { get; set; }
        public Promotion? SelectedPromotion { get; set; }

        public List<PaymentMethod> PaymentMethods { get; set; }
        public PaymentMethod SelectedPaymentMethod { get; set; }

        public List<InvoiceStatus> StatusList { get; set; }
        public InvoiceStatus SelectedStatus { get; set; }

        public InvoiceViewModel() { }

        public InvoiceViewModel(Invoice invoice)
        {
            Id = invoice.Id;
            PersonId = invoice.PersonId;
            PromotionCodeId = invoice.PromotionCodeId;
            CreatedBy = invoice.CreatedBy;
            InvoiceDate = invoice.InvoiceDate;
            Status = (InvoiceStatus)invoice.Status;
            TotalAmount = invoice.TotalAmount;
            DiscountAmount = invoice.DiscountAmount;
            FinalAmount = invoice.FinalAmount;
            PaymentMethod = invoice.PaymentMethod;
            Note = invoice.Note;

            CustomerName = invoice.Customer?.FullName ?? "Unknown";
            CreatedByName = invoice.Creator?.FullName ?? "System";
            DiscountCode = invoice.PromotionCode?.Code ?? "_";

            InvoiceLines = invoice.InvoiceLines?.Select(l => new InvoiceLineViewModel(l)).ToList() ?? new();
        }
    }

    public class InvoiceLineViewModel
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPct { get; set; }
        public decimal TotalPrice { get; set; }

        public InvoiceLineViewModel() { }

        public InvoiceLineViewModel(InvoiceLine line)
        {
            Id = line.Id;
            InvoiceId = line.InvoiceId;
            ProductId = line.ProductId;
            Quantity = line.Quantity;
            UnitPrice = line.UnitPrice;
            DiscountPct = line.DiscountPct;
            TotalPrice = line.TotalPrice;
        }
    }

    public class InvoiceResult
    {
        public IEnumerable<InvoiceViewModel> Invoices { get; set; }
        public InfoTable Info { get; set; }

        public InvoiceResult(IEnumerable<Invoice> invoices, InfoTable info)
        {
            Invoices = invoices.Select(i => new InvoiceViewModel(i)).ToList();
            Info = info;
        }
    }

    /// <summary>
    /// ViewModel for top selling product statistics
    /// </summary>
    public class TopProductStatViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

