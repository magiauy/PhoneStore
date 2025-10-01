using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Implementations;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.ViewModels
{
    public class BatchViewModel
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public DateTime PurchaseOrderOrderDate { get; set; }
        public string? BatchCode { get; set; }
        public string? SupplierName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }

        // Danh sách sản phẩm thuộc batch
        public List<BatchProductViewModel> BatchProducts { get; set; } = new();

        public BatchViewModel() { }

        public BatchViewModel(Batches batch)
        {
            Id = batch.id;
            PurchaseOrderId = batch.PurchaseOrderId;
            BatchCode = batch.BatchCode;
            CreatedAt = batch.CreatedAt;
            Note = batch.Note;
            BatchProducts = batch.BatchProducts?.Select(product => new BatchProductViewModel(product)).ToList() ?? new();
            SupplierName = batch.PurchaseOrder?.Supplier?.Name;
            PurchaseOrderOrderDate = batch.PurchaseOrder?.OrderDate ?? DateTime.MinValue;
        }
    }

    public class BatchProductViewModel
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public BatchProductViewModel() { }

        public BatchProductViewModel(BatchProduct product)
        {
            Id = product.Id;
            BatchId = product.BatchId;
            ProductId = product.ProductId;
            Quantity = product.Quantity;
            CostPrice = product.CostPrice;
            SellingPrice = product.SellingPrice;
        }
    }

    public class BatchesResult
    {
        public IEnumerable<BatchViewModel> Batches { get; set; }
        public InfoTable Info { get; set; }

        public string? BatchCode { get; set; }
        public BatchesResult(IEnumerable<Batches> batches, InfoTable info)
        {
            Batches = batches.Select(b => new BatchViewModel(b)).ToList();
            Info = info;
        }
    }
}