using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Linq;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPersonRepository _personRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ISupplierRepository _supplierRepository;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            IPersonRepository personRepository,
            IEmployeeRepository employeeRepository,
            ISupplierRepository supplierRepository)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        }

        public Invoice? GetById(int id)
        {
            try
            {
                Logger.Info($"Getting Invoice by ID: {id}");
                return _invoiceRepository.GetById(id);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get Invoice by ID: {id}", ex);
                return null;
            }
        }

        public async void Insert(Invoice invoice)
        {          
            try
            {
                Logger.Info($"Inserting Invoice for Customer {invoice.CustomerName}");
                _invoiceRepository.Insert(invoice);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to insert Invoice", ex);
                throw;
            }          
        }

        public void Update(Invoice invoice)
        {
            try
            {
                Logger.Info($"Updating Invoice ID {invoice.Id}");
                _invoiceRepository.Update(invoice);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update Invoice {invoice.Id}", ex);
                throw;
            }
        }

        public void Delete(int id)
        {
            try
            {
                Logger.Info($"Deleting Invoice ID {id}");
                _invoiceRepository.Delete(id);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete Invoice {id}", ex);
                throw;
            }
        }

        public int CountAll()
        {
            try
            {
                return _invoiceRepository.GetTotalRecords(null, null,null, null, null, null, null, null);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to count all Invoices", ex);
                return 0;
            }
        }

        #region Filter + Pagination
        public InvoiceResult GetInvoicesFiltered(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var invoices = _invoiceRepository.GetInvoicesFiltered(
                    customerName, customerId, createdBy, status, fromDate, toDate, minAmount, maxAmount, page, pageSize);

                var totalRecords = _invoiceRepository.GetTotalRecords(
                    customerName, customerId, createdBy, status, fromDate, toDate, minAmount, maxAmount);

                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return new InvoiceResult(
                    invoices,
                    new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered Invoices", ex);
                return new InvoiceResult(new Invoice[0], new InfoTable(0, 0));
            }
        }
        #endregion

        #region Business logic
        public void MarkAsPaid(int id)
        {
            var invoice = GetById(id);
            if (invoice != null && invoice.Status == InvoiceStatus.UNPAID)
            {
                invoice.Status = InvoiceStatus.PAID;
                _invoiceRepository.Update(invoice);
            }
        }

        public void CancelInvoice(int id)
        {
            var invoice = GetById(id);
            if (invoice != null && invoice.Status != InvoiceStatus.CANCELLED)
            {
                invoice.Status = InvoiceStatus.CANCELLED;
                _invoiceRepository.Update(invoice);
            }
        }
        #endregion
    }
}

