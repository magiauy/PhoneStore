using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class InventoryService : IInventoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public InventoryService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<InventoryPageResult> GetInventorySummaryAsync(int page, int pageSize, string? searchTerm = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var productsQuery = dbContext.Products
            .Include(p => p.Model)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = $"%{searchTerm.Trim()}%";
            productsQuery = productsQuery.Where(p =>
                EF.Functions.Like(p.Name, keyword) ||
                EF.Functions.Like(p.Sku, keyword));
        }

        productsQuery = productsQuery.OrderBy(p => p.Name);

        var totalProducts = await productsQuery.CountAsync();
        if (totalProducts == 0)
        {
            return new InventoryPageResult
            {
                Items = new List<InventorySummaryDto>(),
                Page = 1,
                PageSize = pageSize,
                TotalItems = 0
            };
        }

        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var products = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();

        var purchaseSnapshots = await (from pol in dbContext.PurchaseOrderLines
                                       join po in dbContext.PurchaseOrders on pol.PurchaseOrderId equals po.Id
                                       where productIds.Contains(pol.ProductId)
                                             && (po.Status == "received" || po.Status == "RECEIVED")
                                       select new
                                       {
                                           pol.ProductId,
                                           pol.Quantity,
                                           po.SupplierId,
                                           po.OrderDate
                                       }).ToListAsync();

        var latestSupplierLookup = purchaseSnapshots
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.SupplierId).First());

        var supplierIds = latestSupplierLookup.Values
            .Select(x => x.SupplierId)
            .Distinct()
            .ToList();

        var supplierNames = supplierIds.Any()
            ? await dbContext.Suppliers
                .Where(s => supplierIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name ?? $"Nhà cung cấp #{s.Id}")
            : new Dictionary<int, string>();

        var serialInStockCounts = await dbContext.ProductSerials
            .Where(ps => productIds.Contains(ps.ProductId) && ps.Status == "in_stock")
            .GroupBy(ps => ps.ProductId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var batchCurrentLookup = await dbContext.BatchProducts
            .Where(bp => productIds.Contains(bp.ProductId))
            .GroupBy(bp => bp.ProductId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(bp => bp.Quantity));

        var inventoryItems = products.Select(product =>
        {
            var hasSupplier = latestSupplierLookup.TryGetValue(product.Id, out var info);
            var supplierId = hasSupplier ? info.SupplierId : 0;
            var supplierName = hasSupplier && supplierNames.TryGetValue(supplierId, out var name)
                ? name
                : "Chưa có nhà cung cấp";

            var totalQuantity = product.IsSerialTracked
                ? (serialInStockCounts.TryGetValue(product.Id, out var serialQty) ? serialQty : 0)
                : (batchCurrentLookup.TryGetValue(product.Id, out var currentQty) ? currentQty : 0);

            return new InventorySummaryDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.Model?.DefaultImageUrl,
                TotalQuantity = totalQuantity,
                IsSerialTracked = product.IsSerialTracked,
                SupplierId = supplierId,
                SupplierName = supplierName
            };
        }).ToList();

        return new InventoryPageResult
        {
            Items = inventoryItems,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalProducts
        };
    }

    public async Task<List<BatchDetailDto>> GetProductBatchDetailsAsync(int productId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var product = await dbContext.Products.FindAsync(productId);
        if (product == null)
        {
            return new List<BatchDetailDto>();
        }

        var result = new List<BatchDetailDto>();
        if (product.IsSerialTracked)
        {
            var serials = await dbContext.ProductSerials
                .Where(ps => ps.ProductId == productId)
                .ToListAsync();

            if (!serials.Any())
            {
                return result;
            }

            var batchIds = serials
                .Where(s => s.BatchId.HasValue)
                .Select(s => s.BatchId!.Value)
                .Distinct()
                .ToList();

            var batches = await dbContext.Batches
                .Where(b => batchIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id);

            var purchaseOrderIds = batches.Values
                .Select(b => b.PurchaseOrderId)
                .Distinct()
                .ToList();

            var poLinesLookup = await dbContext.PurchaseOrderLines
                .Where(pol => purchaseOrderIds.Contains(pol.PurchaseOrderId) && pol.ProductId == productId)
                .GroupBy(pol => new { pol.PurchaseOrderId, pol.ProductId })
                .ToDictionaryAsync(g => (g.Key.PurchaseOrderId, g.Key.ProductId), g => g.Sum(pol => pol.Quantity));

            var grouped = serials.GroupBy(s => s.BatchId);
            foreach (var group in grouped)
            {
                if (group.Key.HasValue && batches.TryGetValue(group.Key.Value, out var batch))
                {
                    var importedQty = poLinesLookup.TryGetValue((batch.PurchaseOrderId, productId), out var qty)
                        ? qty
                        : group.Count();

                    result.Add(new BatchDetailDto
                    {
                        BatchId = batch.Id,
                        BatchCode = batch.BatchCode,
                        CreatedAt = batch.CreatedAt,
                        ImportedQuantity = importedQty,
                        CurrentQuantity = group.Count(s => s.Status == "in_stock")
                    });
                }
                else
                {
                    var total = group.Count();
                    result.Add(new BatchDetailDto
                    {
                        BatchId = 0,
                        BatchCode = "Unknown Batch",
                        CreatedAt = DateTime.MinValue,
                        ImportedQuantity = total,
                        CurrentQuantity = group.Count(s => s.Status == "in_stock")
                    });
                }
            }
        }
        else
        {
            var batchProducts = await dbContext.BatchProducts
                .Where(bp => bp.ProductId == productId)
                .Include(bp => bp.Batch)
                .ToListAsync();

            if (!batchProducts.Any())
            {
                return result;
            }

            var purchaseOrderIds = batchProducts
                .Where(bp => bp.Batch != null)
                .Select(bp => bp.Batch!.PurchaseOrderId)
                .Distinct()
                .ToList();

            var poLinesLookup = await dbContext.PurchaseOrderLines
                .Where(pol => purchaseOrderIds.Contains(pol.PurchaseOrderId) && pol.ProductId == productId)
                .GroupBy(pol => new { pol.PurchaseOrderId, pol.ProductId })
                .ToDictionaryAsync(g => (g.Key.PurchaseOrderId, g.Key.ProductId), g => g.Sum(pol => pol.Quantity));

            foreach (var bp in batchProducts)
            {
                var batch = bp.Batch;
                if (batch == null)
                {
                    continue;
                }

                var importedQty = poLinesLookup.TryGetValue((batch.PurchaseOrderId, productId), out var qty)
                    ? qty
                    : bp.Quantity;

                result.Add(new BatchDetailDto
                {
                    BatchId = batch.Id,
                    BatchCode = batch.BatchCode,
                    CreatedAt = batch.CreatedAt,
                    ImportedQuantity = importedQty,
                    CurrentQuantity = bp.Quantity
                });
            }
        }

        return result
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.BatchCode)
            .ToList();
    }

    public async Task<Dictionary<int, ProductAvailabilitySnapshot>> GetAvailabilityForProductsAsync(IEnumerable<int> productIds)
    {
        var idList = productIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList() ?? new List<int>();

        if (idList.Count == 0)
        {
            return new Dictionary<int, ProductAvailabilitySnapshot>();
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var products = await dbContext.Products
            .Where(p => idList.Contains(p.Id))
            .Select(p => new { p.Id, p.IsSerialTracked })
            .ToListAsync();

        var snapshots = products.ToDictionary(
            p => p.Id,
            p => new ProductAvailabilitySnapshot
            {
                ProductId = p.Id,
                IsSerialTracked = p.IsSerialTracked,
                AvailableQuantity = 0
            });

        foreach (var missingId in idList.Where(id => !snapshots.ContainsKey(id)))
        {
            snapshots[missingId] = new ProductAvailabilitySnapshot
            {
                ProductId = missingId,
                IsSerialTracked = false,
                AvailableQuantity = 0
            };
        }

        var serialCounts = await dbContext.ProductSerials
            .Where(ps => idList.Contains(ps.ProductId) && ps.Status == "in_stock")
            .GroupBy(ps => ps.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Count()
            })
            .ToListAsync();

        foreach (var item in serialCounts)
        {
            if (snapshots.TryGetValue(item.ProductId, out var snapshot) && snapshot.IsSerialTracked)
            {
                snapshot.AvailableQuantity = item.Quantity;
            }
        }

        var batchTotals = await dbContext.BatchProducts
            .Where(bp => idList.Contains(bp.ProductId))
            .GroupBy(bp => bp.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(bp => bp.Quantity)
            })
            .ToListAsync();

        foreach (var item in batchTotals)
        {
            if (snapshots.TryGetValue(item.ProductId, out var snapshot) && !snapshot.IsSerialTracked)
            {
                snapshot.AvailableQuantity = item.Quantity;
            }
        }

        return snapshots;
    }

    public async Task<List<string>> GetSuitableSerialsAsync(int productId, int count)
    {
        if (count <= 0) return new List<string>();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // 1. Get all In_Stock serials for the product
        // 2. Join with Batch to get CreatedAt
        // 3. Order by Batch.CreatedAt ASC, then Serial.Id
        // 4. Take count

        // Note: Some serials might not have a batch (e.g. manually added without batch, though rare in this flow). 
        // We treat null batch as "very old" or "very new"? 
        // Let's treat null batch as "Unknown date", maybe put them at the end or beginning. 
        // Requirement says "from oldest batch".


        var query = from s in dbContext.ProductSerials
                    join b in dbContext.Batches on s.BatchId equals b.Id into bj
                    from batch in bj.DefaultIfEmpty()

                    where s.ProductId == productId && s.Status == "in_stock"
                    orderby batch.CreatedAt ascending, s.Id ascending
                    select s.SerialNumber;

        var result = await query.Take(count).ToListAsync();
        return result;
    }
}
