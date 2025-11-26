using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Data;
using PhoneStoreUser.Models;

namespace PhoneStoreUser.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly AppDbContext _context;

        public PurchaseOrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PurchaseOrderDTO>> GetPurchaseOrdersAsync()
        {
            var pos = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Lines)
                .OrderByDescending(p => p.OrderDate)
                .ToListAsync();

            return pos.Select(p => new PurchaseOrderDTO
            {
                Id = p.Id,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier?.Name ?? "",
                CreatedBy = p.CreatedBy,
                CreatedByName = "Admin", // TODO: Get actual user name
                OrderDate = p.OrderDate,
                Status = p.Status,
                TotalAmount = p.TotalAmount,
                Note = p.Note,
                Lines = p.Lines.Select(l => new PurchaseOrderLineDTO
                {
                    Id = l.Id,
                    PurchaseOrderId = l.PurchaseOrderId,
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost
                }).ToList()
            }).ToList();
        }

        public async Task<PurchaseOrderDTO?> GetPurchaseOrderByIdAsync(int id)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Lines)
                    .ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return null;

            var dto = new PurchaseOrderDTO
            {
                Id = po.Id,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier?.Name ?? "",
                CreatedBy = po.CreatedBy,
                CreatedByName = "Admin",
                OrderDate = po.OrderDate,
                Status = po.Status,
                TotalAmount = po.TotalAmount,
                Note = po.Note,
                Lines = po.Lines.Select(l => new PurchaseOrderLineDTO
                {
                    Id = l.Id,
                    PurchaseOrderId = l.PurchaseOrderId,
                    ProductId = l.ProductId,
                    ProductName = l.Product?.Name ?? "",
                    ProductSku = l.Product?.Sku ?? "",
                    IsSerialTracked = l.Product?.IsSerialTracked ?? false,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost
                }).ToList()
            };

            // Load serials for each line
            foreach (var line in dto.Lines)
            {
                if (line.IsSerialTracked)
                {
                    var serials = await _context.ProductSerials
                        .Where(s => s.PurchaseOrderLineId == line.Id)
                        .ToListAsync();

                    line.Serials = serials.Select(s => new ProductSerialDTO
                    {
                        Id = s.Id,
                        ProductId = s.ProductId,
                        SerialNumber = s.SerialNumber ?? "",
                        Imei1 = s.Imei1,
                        Imei2 = s.Imei2,
                        Status = s.Status,
                        Note = s.Note
                    }).ToList();
                }
            }

            return dto;
        }

        public async Task<int> CreatePurchaseOrderAsync(PurchaseOrderDTO dto)
        {
            var po = new PurchaseOrderEntity
            {
                SupplierId = dto.SupplierId,
                CreatedBy = dto.CreatedBy,
                OrderDate = dto.OrderDate,
                Status = "DRAFT",
                TotalAmount = dto.Lines.Sum(l => l.TotalCost),
                Note = dto.Note
            };

            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            foreach (var lineDto in dto.Lines)
            {
                var line = new PurchaseOrderLineEntity
                {
                    PurchaseOrderId = po.Id,
                    ProductId = lineDto.ProductId,
                    Quantity = lineDto.Quantity,
                    UnitCost = lineDto.UnitCost,
                    TotalCost = lineDto.Quantity * lineDto.UnitCost
                };
                _context.PurchaseOrderLines.Add(line);
                await _context.SaveChangesAsync(); // Save to get Line Id

                // Handle Serials if provided (though usually empty on create)
                if (lineDto.Serials != null && lineDto.Serials.Any())
                {
                    foreach (var serialDto in lineDto.Serials)
                    {
                        var serial = new ProductSerialEntity
                        {
                            ProductId = lineDto.ProductId,
                            SerialNumber = serialDto.SerialNumber,
                            Imei1 = serialDto.Imei1,
                            Imei2 = serialDto.Imei2,
                            Status = "rma", // Draft status
                            PurchaseOrderLineId = line.Id,
                            Note = serialDto.Note
                        };
                        _context.ProductSerials.Add(serial);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return po.Id;
        }

        public async Task UpdatePurchaseOrderAsync(PurchaseOrderDTO dto)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (po == null) throw new Exception("Purchase Order not found");
            if (po.Status == "RECEIVED" || po.Status == "CANCELLED") throw new Exception("Cannot edit finalized order");

            // Update Header
            po.SupplierId = dto.SupplierId;
            po.OrderDate = dto.OrderDate;
            po.Note = dto.Note;
            po.TotalAmount = dto.Lines.Sum(l => l.TotalCost);

            // Identify lines to remove
            var incomingLineIds = dto.Lines.Where(l => l.Id > 0).Select(l => l.Id).ToList();
            var linesToDelete = po.Lines.Where(l => !incomingLineIds.Contains(l.Id)).ToList();

            foreach (var line in linesToDelete)
            {
                // Delete associated serials first
                var serialsToDelete = await _context.ProductSerials.Where(s => s.PurchaseOrderLineId == line.Id).ToListAsync();
                _context.ProductSerials.RemoveRange(serialsToDelete);
                _context.PurchaseOrderLines.Remove(line);
            }

            // Process incoming lines
            foreach (var lineDto in dto.Lines)
            {
                PurchaseOrderLineEntity lineEntity;

                if (lineDto.Id > 0)
                {
                    // Update existing line
                    lineEntity = po.Lines.First(l => l.Id == lineDto.Id);
                    lineEntity.ProductId = lineDto.ProductId;
                    lineEntity.Quantity = lineDto.Quantity;
                    lineEntity.UnitCost = lineDto.UnitCost;
                    lineEntity.TotalCost = lineDto.Quantity * lineDto.UnitCost;
                }
                else
                {
                    // Create new line
                    lineEntity = new PurchaseOrderLineEntity
                    {
                        PurchaseOrderId = po.Id,
                        ProductId = lineDto.ProductId,
                        Quantity = lineDto.Quantity,
                        UnitCost = lineDto.UnitCost,
                        TotalCost = lineDto.Quantity * lineDto.UnitCost
                    };
                    _context.PurchaseOrderLines.Add(lineEntity);
                }

                // We need to save changes to get the ID for new lines if we want to add serials
                // However, saving inside the loop is inefficient. 
                // But since we need the LineID for ProductSerialEntity, we have to ensure it's generated.
                // EF Core fixes up navigation properties, but ProductSerialEntity doesn't have a navigation property back to PurchaseOrderLineEntity in the provided code (it has int? PurchaseOrderLineId).
                // So we MUST save to get the ID.
                await _context.SaveChangesAsync();

                // Handle Serials
                if (lineDto.Serials != null)
                {
                    var existingSerials = await _context.ProductSerials.Where(s => s.PurchaseOrderLineId == lineEntity.Id).ToListAsync();
                    var incomingSerialIds = lineDto.Serials.Where(s => s.Id > 0).Select(s => s.Id).ToList();

                    // Delete removed serials
                    var serialsToRemove = existingSerials.Where(s => !incomingSerialIds.Contains(s.Id)).ToList();
                    _context.ProductSerials.RemoveRange(serialsToRemove);

                    foreach (var serialDto in lineDto.Serials)
                    {
                        if (serialDto.Id > 0)
                        {
                            var serial = existingSerials.FirstOrDefault(s => s.Id == serialDto.Id);
                            if (serial != null)
                            {
                                serial.SerialNumber = serialDto.SerialNumber;
                                serial.Imei1 = serialDto.Imei1;
                                serial.Imei2 = serialDto.Imei2;
                                serial.Note = serialDto.Note;
                            }
                        }
                        else
                        {
                            var serial = new ProductSerialEntity
                            {
                                ProductId = lineDto.ProductId,
                                SerialNumber = serialDto.SerialNumber,
                                Imei1 = serialDto.Imei1,
                                Imei2 = serialDto.Imei2,
                                Status = "rma",
                                PurchaseOrderLineId = lineEntity.Id,
                                Note = serialDto.Note
                            };
                            _context.ProductSerials.Add(serial);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task CompletePurchaseOrderAsync(int id)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) throw new Exception("Purchase Order not found");
            if (po.Status != "DRAFT") throw new Exception("Only draft orders can be completed");

            // 1. Create Batch
            var batch = new BatchEntity
            {
                PurchaseOrderId = po.Id,
                BatchCode = $"BATCH-{po.Id}-{DateTime.Now:yyyyMMdd}",
                CreatedAt = DateTime.UtcNow,
                Note = $"Batch for PO #{po.Id}"
            };
            _context.Batches.Add(batch);
            await _context.SaveChangesAsync();

            // 2. Update Serials to 'in_stock' and assign BatchId
            var serials = await _context.ProductSerials
                .Where(s => s.PurchaseOrderLineId != null && po.Lines.Select(l => l.Id).Contains(s.PurchaseOrderLineId.Value))
                .ToListAsync();

            foreach (var serial in serials)
            {
                serial.Status = "in_stock";
                serial.BatchId = batch.Id;
            }

            // 3. Create BatchProduct records (Inventory)
            foreach (var line in po.Lines)
            {
                var batchProduct = new BatchProductEntity
                {
                    BatchId = batch.Id,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    CostPrice = line.UnitCost,
                    SellingPrice = 0 // TODO: Logic for selling price? Maybe leave 0 or copy cost
                };
                _context.BatchProducts.Add(batchProduct);
            }

            // 4. Update PO Status
            po.Status = "RECEIVED";
            await _context.SaveChangesAsync();
        }

        public async Task CancelPurchaseOrderAsync(int id)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) throw new Exception("Purchase Order not found");

            // Delete associated Batches
            var batches = await _context.Batches.Where(b => b.PurchaseOrderId == po.Id).ToListAsync();
            _context.Batches.RemoveRange(batches);

            // Delete associated Serials
            // We need to find serials linked to the lines of this PO
            var lineIds = po.Lines.Select(l => l.Id).ToList();
            var serials = await _context.ProductSerials
                .Where(s => s.PurchaseOrderLineId != null && lineIds.Contains(s.PurchaseOrderLineId.Value))
                .ToListAsync();
            _context.ProductSerials.RemoveRange(serials);

            po.Status = "CANCELLED";
            await _context.SaveChangesAsync();
        }
    }
}
