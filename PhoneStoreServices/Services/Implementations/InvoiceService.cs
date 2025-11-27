using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Implementations;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStore.Services.Implementations
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPersonRepository _personRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IInvoiceLineRepository _invoiceLineRepository;
        private readonly IProductSerialRepository _productSerialRepository;
        private readonly IBatchProductRepository _batchProductRepository;
        private readonly IInvoiceLineSerialRepository _invoiceLineSerialRepository;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            IPersonRepository personRepository,
            IEmployeeRepository employeeRepository,
            ISupplierRepository supplierRepository,
            IInvoiceLineRepository invoiceLineRepository,
            IProductSerialRepository productSerialRepository,
            IBatchProductRepository batchProductRepository,
            IInvoiceLineSerialRepository invoiceLineSerialRepository)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));

            _invoiceLineRepository = invoiceLineRepository ?? throw new ArgumentNullException(nameof(InvoiceLineRepository));
            _productSerialRepository = productSerialRepository ?? throw new ArgumentNullException(nameof(productSerialRepository));
            _batchProductRepository = batchProductRepository ?? throw new ArgumentNullException(nameof(batchProductRepository));
            _invoiceLineSerialRepository = invoiceLineSerialRepository ?? throw new ArgumentNullException(nameof(invoiceLineSerialRepository));
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

        public void CreateFullInvoice(Invoice invoice, List<InvoiceLine> uiItems, string customerName, string customerPhone, List<InvoiceLineSerialRequest>? serialRequests = null)
        {
            var existingPerson = _personRepository.GetByPhone(customerPhone);

            int customerId;

            if (existingPerson != null)
            {
                customerId = existingPerson.Id;
            }
            else
            {
                var newPerson = new Person
                {
                    FullName = customerName,
                    Phone = customerPhone,
                    Email = "",
                    PersonType = PersonType.CUSTOMER,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _personRepository.Insert(newPerson); 
                customerId = newPerson.Id;
            }


            invoice.PersonId = customerId;
            invoice.TotalAmount = uiItems.Sum(x => x.TotalPrice);
            invoice.FinalAmount = invoice.TotalAmount - invoice.DiscountAmount; 

            _invoiceRepository.Insert(invoice); 

            var serialLookup = (serialRequests ?? Enumerable.Empty<InvoiceLineSerialRequest>())
                .Where(req => req.Line != null)
                .ToDictionary(req => req.Line!, req => req.SerialNumbers);

            foreach (var item in uiItems)
            {
                item.InvoiceId = invoice.Id;
                item.TotalPrice = item.UnitPrice * item.Quantity;
                _invoiceLineRepository.Insert(item);

                if (!serialLookup.TryGetValue(item, out var serialNumbers) || serialNumbers.Count == 0)
                {
                    continue;
                }

                var sanitizedSerials = serialNumbers
                    .Where(sn => !string.IsNullOrWhiteSpace(sn))
                    .Select(sn => sn.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(item.Quantity)
                    .ToList();

                if (sanitizedSerials.Count == 0)
                {
                    continue;
                }

                var invoiceLineSerials = new List<InvoiceLineSerial>();

                foreach (var serialText in sanitizedSerials)
                {
                    var serialEntity = _productSerialRepository.TryGetBySerialNumber(serialText);
                    if (serialEntity == null || serialEntity.Status != SerialStatus.IN_STOCK)
                    {
                        continue;
                    }

                    serialEntity.Status = SerialStatus.SOLD;
                    _productSerialRepository.Update(serialEntity);

                    invoiceLineSerials.Add(new InvoiceLineSerial(item.Id, serialEntity.Id));

                    if (serialEntity.BatchId.HasValue)
                    {
                        _batchProductRepository.DecreaseQuantity(serialEntity.BatchId.Value, serialEntity.ProductId, 1);
                    }
                }

                if (invoiceLineSerials.Count > 0)
                {
                    _invoiceLineSerialRepository.InsertRange(invoiceLineSerials);
                }
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

