using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public SupplierService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public IEnumerable<Supplier> GetAll()
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                return context.Suppliers.AsNoTracking()
                    .Select(s => new Supplier
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Phone = s.Phone,
                        Email = s.Email,
                        Address = s.Address,
                        TaxNumber = s.TaxNumber,
                        IsActive = s.IsActive
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get Supplier all: {ex.Message}");
                return Enumerable.Empty<Supplier>();
            }
        }

        public bool Insert(Supplier supplier)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var entity = new SupplierEntity
                {
                    Name = supplier.Name,
                    Phone = supplier.Phone,
                    Email = supplier.Email,
                    Address = supplier.Address,
                    TaxNumber = supplier.TaxNumber,
                    IsActive = supplier.IsActive
                };

                context.Suppliers.Add(entity);
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to insert Supplier: {ex.Message}");
                return false;
            }
        }

        public bool Update(Supplier supplier)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var entity = context.Suppliers.Find(supplier.Id);
                if (entity == null) return false;

                entity.Name = supplier.Name;
                entity.Phone = supplier.Phone;
                entity.Email = supplier.Email;
                entity.Address = supplier.Address;
                entity.TaxNumber = supplier.TaxNumber;
                entity.IsActive = supplier.IsActive;

                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update Supplier {supplier.Id}: {ex.Message}");
                return false;
            }
        }

        public Supplier? GetSupplierById(int supplierId)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var entity = context.Suppliers.AsNoTracking().FirstOrDefault(s => s.Id == supplierId);
                if (entity == null) return null;

                return new Supplier
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Phone = entity.Phone,
                    Email = entity.Email,
                    Address = entity.Address,
                    TaxNumber = entity.TaxNumber,
                    IsActive = entity.IsActive
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get Supplier by ID: {supplierId}: {ex.Message}");
                return null;
            }
        }

        public (IEnumerable<Supplier> Items, int TotalCount, int TotalPages) GetSuppliersFiltered(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var query = context.Suppliers.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(name))
                    query = query.Where(s => s.Name.Contains(name));
                if (!string.IsNullOrWhiteSpace(phone))
                    query = query.Where(s => s.Phone != null && s.Phone.Contains(phone));
                if (!string.IsNullOrWhiteSpace(email))
                    query = query.Where(s => s.Email != null && s.Email.Contains(email));
                if (!string.IsNullOrWhiteSpace(address))
                    query = query.Where(s => s.Address != null && s.Address.Contains(address));
                if (!string.IsNullOrWhiteSpace(taxNumber))
                    query = query.Where(s => s.TaxNumber != null && s.TaxNumber.Contains(taxNumber));
                if (isActive.HasValue)
                    query = query.Where(s => s.IsActive == isActive.Value);

                var totalRecords = query.Count();
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                var items = query
                    .OrderByDescending(s => s.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new Supplier
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Phone = s.Phone,
                        Email = s.Email,
                        Address = s.Address,
                        TaxNumber = s.TaxNumber,
                        IsActive = s.IsActive
                    })
                    .ToList();

                return (items, totalRecords, totalPages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load filtered suppliers: {ex.Message}");
                return (Enumerable.Empty<Supplier>(), 0, 0);
            }
        }

        public void ActivateSupplier(int supplierId)
        {
            var supplier = GetSupplierById(supplierId);
            if (supplier != null && !supplier.IsActive)
            {
                supplier.IsActive = true;
                Update(supplier);
            }
        }

        public void DeactivateSupplier(int supplierId)
        {
            var supplier = GetSupplierById(supplierId);
            if (supplier != null && supplier.IsActive)
            {
                supplier.IsActive = false;
                Update(supplier);
            }
        }
    }
}
