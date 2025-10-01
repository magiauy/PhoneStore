using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Implementations;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.ViewModels
{
    public class PurchaseOrderViewModel
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public int CreatedBy { get; set; }
        public DateTime OrderDate { get; set; }
        public PoStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        // Danh sách line thuộc đơn hàng
        public List<PurchaseOrderLineViewModel> PurchaseOrderLines { get; set; } = new();

        // Dùng để hiển thị text trạng thái theo ngôn ngữ
        public string LocalizedStatusText
        {
            get
            {
                var key = $"POStatus_{Status}_data";
                return PhoneStoreAdmin.Helpers.LocalizationHelper.GetString(key.ToUpper());
            }
        }

        // Màu trạng thái (có thể chỉnh theo logic của bạn)
        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    PoStatus.DRAFT => "#6c757d",       // xám
                    PoStatus.RECEIVED => "#007bff",    // xanh dương
                    PoStatus.CANCELLED => "#dc3545",   // đỏ
                    _ => "#6c757d"
                };
            }
        }

        public PurchaseOrderViewModel() { }

        public PurchaseOrderViewModel(PurchaseOrder po)
        {
            Id = po.Id;
            SupplierId = po.SupplierId;
            CreatedBy = po.CreatedBy;
            OrderDate = po.OrderDate;
            Status = po.Status;
            TotalAmount = po.TotalAmount;
            Note = po.Note;
            PurchaseOrderLines = po.PurchaseOrderLines.Select(line => new PurchaseOrderLineViewModel(line)).ToList();
        }
    }

    public class PurchaseOrderLineViewModel
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }

        public PurchaseOrderLineViewModel() { }

        public PurchaseOrderLineViewModel(PurchaseOrderLine line)
        {
            Id = line.Id;
            PurchaseOrderId = line.PurchaseOrderId;
            ProductId = line.ProductId;
            Quantity = line.Quantity;
            UnitCost = line.UnitCost;
            TotalCost = line.TotalCost;
        }
    }

    public class PurchaseOrderResult
    {
        public IEnumerable<PurchaseOrderViewModel> PurchaseOrders { get; set; }
        public InfoTable Info { get; set; }

        public PurchaseOrderResult(IEnumerable<PurchaseOrder> purchaseOrders, InfoTable info)
        {
            PurchaseOrders = purchaseOrders.Select(po => new PurchaseOrderViewModel(po)).ToList();
            Info = info;
        }
    }
}