using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using System;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        IEnumerable<Invoice> GetInvoicesFiltered(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int page = 1,
            int pageSize = 20
        );

        int GetTotalRecords(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount
        );

        int GetTotalPages(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int pageSize
        );
    }

    public interface IInvoiceLineRepository
    {
        /// <summary>
        /// Thêm một dòng chi tiết vào hóa đơn
        /// </summary>
        /// <param name="entity">Đối tượng InvoiceLine</param>
        void Insert(InvoiceLine entity);

        /// <summary>
        /// Lấy danh sách các dòng chi tiết theo ID hóa đơn
        /// </summary>
        /// <param name="invoiceId">ID của hóa đơn cha</param>
        /// <returns>Danh sách InvoiceLine</returns>
        IEnumerable<InvoiceLine> GetByInvoiceId(int invoiceId);

        /// <summary>
        /// Cập nhật một dòng chi tiết (nếu cần chỉnh sửa số lượng/giá sau này)
        /// </summary>
        void Update(InvoiceLine entity);

        /// <summary>
        /// Xóa một dòng chi tiết dựa trên ID của dòng đó
        /// </summary>
        void Delete(int id);
    }
}

