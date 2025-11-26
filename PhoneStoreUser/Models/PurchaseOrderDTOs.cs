using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreUser.Models
{
    public class PurchaseOrderDTO
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "DRAFT";
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }

        public List<PurchaseOrderLineDTO> Lines { get; set; } = new();
    }

    public class PurchaseOrderLineDTO
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public bool IsSerialTracked { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không hợp lệ")]
        public decimal UnitCost { get; set; }
        
        public decimal TotalCost => Quantity * UnitCost;

        // For UI handling of serials
        public List<ProductSerialDTO> Serials { get; set; } = new();
    }

    public class ProductSerialDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string? Imei1 { get; set; }
        public string? Imei2 { get; set; }
        public string Status { get; set; } = "rma"; // Default for new serials in draft PO
        public string? Note { get; set; }
    }
}
