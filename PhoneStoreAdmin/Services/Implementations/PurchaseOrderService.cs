using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Implementations;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _poRepository;
        private readonly IPurchaseOrderLineRepository _lineRepository;  

        public PurchaseOrderService(IPurchaseOrderRepository poRepository, IPurchaseOrderLineRepository lineRepository)
        {
            _poRepository = poRepository ?? throw new ArgumentNullException(nameof(poRepository));
            _lineRepository = lineRepository ?? throw new ArgumentNullException(nameof(lineRepository));
        }

        public PurchaseOrder? GetById(int id)
        {
            try
            {
                Logger.Info($"Getting PurchaseOrder by ID: {id}");
                var po = _poRepository.GetById(id);
                if (po != null)
                {
                    po.PurchaseOrderLines = _lineRepository.GetByPurchaseOrderId(id);
                }
                return po;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get PurchaseOrder by ID: {id}", ex);
                return null;
            }
        }

        public void Insert(PurchaseOrder po)
        {
            try
            {
                po.TotalAmount = po.PurchaseOrderLines.Sum(l => l.TotalCost);
                Logger.Info($"Inserting PurchaseOrder for Supplier {po.SupplierId}");
                _poRepository.Insert(po);
                foreach (var line in po.PurchaseOrderLines)
                {
                    line.PurchaseOrderId = po.Id;
                    _lineRepository.Insert(line);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to insert PurchaseOrder", ex);
                throw;
            }
        }

        public void Update(PurchaseOrder po)
        {
            try
            {
                po.TotalAmount = po.PurchaseOrderLines.Sum(l => l.TotalCost);
                Logger.Info($"Updating PurchaseOrder ID {po.Id}");
                _poRepository.Update(po);
                _lineRepository.DeleteByPurchaseOrderId(po.Id);
                foreach (var line in po.PurchaseOrderLines)
                {
                    line.PurchaseOrderId = po.Id;
                    _lineRepository.Insert(line);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update PurchaseOrder {po.Id}", ex);
                throw;
            }
        }

        public void Delete(int id)
        {
            try
            {
                Logger.Info($"Deleting PurchaseOrder ID {id}");
                _lineRepository.DeleteByPurchaseOrderId(id);
                _poRepository.Delete(id);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete PurchaseOrder {id}", ex);
                throw;
            }
        }

        #region Filter + Pagination
        public PurchaseOrderResult GetPurchaseOrdersFiltered(
            string? supplierName,
            int? supplierId,
            int? createdBy,
            PoStatus? status,
            string? note,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var orders = _poRepository.GetPurchaseOrdersFiltered(
                    supplierName, supplierId, createdBy, status, note, fromDate, toDate, minAmount, maxAmount, page, pageSize);

                var totalRecords = _poRepository.GetTotalRecords(
                    supplierName, supplierId, createdBy, status, note, fromDate, toDate, minAmount, maxAmount);

                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return new PurchaseOrderResult(
                    orders,
                    new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered PurchaseOrders", ex);
                return new PurchaseOrderResult(new PurchaseOrder[0], new InfoTable(0, 0));
            }
        }
        #endregion

        #region Business logic
        public void MarkAsReceived(int id)
        {
            var po = GetById(id);
            if (po != null && po.Status == PoStatus.DRAFT)
            {
                po.Status = PoStatus.RECEIVED;
                _poRepository.Update(po);
            }
        }

        public void CancelOrder(int id)
        {
            var po = GetById(id);
            if (po != null && po.Status != PoStatus.CANCELLED)
            {
                po.Status = PoStatus.CANCELLED;
                _poRepository.Update(po);
            }
        }
        #endregion
    }
}