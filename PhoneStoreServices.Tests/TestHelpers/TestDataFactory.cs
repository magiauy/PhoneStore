using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStoreServices.Tests.TestHelpers;

/// <summary>
/// Factory class for creating test data objects
/// </summary>
public static class TestDataFactory
{
    #region Product Factory Methods

    /// <summary>
    /// Creates a test Product with pricing fields
    /// </summary>
    public static Product CreateProduct(
        int id = 1,
        string name = "Test Product",
        string sku = "SKU001",
        decimal price = 1000m,
        decimal cost = 800m,
        decimal costFifo = 800m,
        decimal costNifo = 850m,
        MarketTrend marketTrend = MarketTrend.STABLE,
        PricingMode pricingMode = PricingMode.AUTO_PROTECT,
        bool isSerialTracked = true,
        int categoryId = 1,
        int modelId = 1,
        int? brandId = null,
        ProductStatus status = ProductStatus.ACTIVE)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Sku = sku,
            Price = price,
            Cost = cost,
            CostFifo = costFifo,
            CostNifo = costNifo,
            MarketTrend = marketTrend,
            PricingMode = pricingMode,
            IsSerialTracked = isSerialTracked,
            CategoryId = categoryId,
            ModelId = modelId,
            BrandId = brandId,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a list of test Products
    /// </summary>
    public static List<Product> CreateProductList(int count)
    {
        var products = new List<Product>();
        for (int i = 1; i <= count; i++)
        {
            products.Add(CreateProduct(
                id: i,
                name: $"Product {i}",
                sku: $"SKU{i:D3}",
                price: 1000m + (i * 100),
                cost: 800m + (i * 80),
                costFifo: 800m + (i * 80),
                costNifo: 850m + (i * 85)
            ));
        }
        return products;
    }

    #endregion

    #region PurchaseOrder Factory Methods

    /// <summary>
    /// Creates a test PurchaseOrder with lines
    /// </summary>
    public static PurchaseOrder CreatePurchaseOrder(
        int id = 1,
        int supplierId = 1,
        int createdBy = 1,
        PoStatus status = PoStatus.DRAFT,
        decimal totalAmount = 0m,
        string? note = null,
        List<PurchaseOrderLine>? lines = null)
    {
        var po = new PurchaseOrder
        {
            Id = id,
            SupplierId = supplierId,
            CreatedBy = createdBy,
            Status = status,
            TotalAmount = totalAmount,
            Note = note,
            OrderDate = DateTime.UtcNow,
            PurchaseOrderLines = lines ?? new List<PurchaseOrderLine>()
        };

        // Calculate total amount if lines provided and totalAmount is 0
        if (totalAmount == 0m && lines != null && lines.Count > 0)
        {
            po.TotalAmount = lines.Sum(l => l.TotalCost);
        }

        return po;
    }

    /// <summary>
    /// Creates a test PurchaseOrderLine
    /// </summary>
    public static PurchaseOrderLine CreatePurchaseOrderLine(
        int id = 1,
        int purchaseOrderId = 1,
        int productId = 1,
        int quantity = 10,
        decimal unitCost = 800m,
        decimal profitMargin = 0.1m)
    {
        return new PurchaseOrderLine
        {
            Id = id,
            PurchaseOrderId = purchaseOrderId,
            ProductId = productId,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost,
            ProfitMargin = profitMargin
        };
    }

    /// <summary>
    /// Creates a list of test PurchaseOrderLines
    /// </summary>
    public static List<PurchaseOrderLine> CreatePurchaseOrderLineList(
        int count,
        int purchaseOrderId = 1,
        decimal baseUnitCost = 800m)
    {
        var lines = new List<PurchaseOrderLine>();
        for (int i = 1; i <= count; i++)
        {
            lines.Add(CreatePurchaseOrderLine(
                id: i,
                purchaseOrderId: purchaseOrderId,
                productId: i,
                quantity: 10 + i,
                unitCost: baseUnitCost + (i * 50)
            ));
        }
        return lines;
    }

    #endregion

    #region Batch Factory Methods

    /// <summary>
    /// Creates a test Batch with products
    /// </summary>
    public static Batches CreateBatch(
        int id = 1,
        int purchaseOrderId = 1,
        string? batchCode = "BATCH-001",
        string? note = null,
        List<BatchProduct>? products = null)
    {
        return new Batches
        {
            id = id,
            PurchaseOrderId = purchaseOrderId,
            BatchCode = batchCode,
            Note = note,
            CreatedAt = DateTime.UtcNow,
            BatchProducts = products ?? new List<BatchProduct>()
        };
    }

    /// <summary>
    /// Creates a test BatchProduct
    /// </summary>
    public static BatchProduct CreateBatchProduct(
        int id = 1,
        int batchId = 1,
        int productId = 1,
        int quantity = 10,
        decimal costPrice = 800m,
        decimal profitMargin = 0.1m,
        decimal? sellingPrice = null)
    {
        return new BatchProduct
        {
            Id = id,
            BatchId = batchId,
            ProductId = productId,
            Quantity = quantity,
            CostPrice = costPrice,
            ProfitMargin = profitMargin,
            SellingPrice = sellingPrice ?? costPrice * (1 + profitMargin)
        };
    }

    /// <summary>
    /// Creates a list of test BatchProducts
    /// </summary>
    public static List<BatchProduct> CreateBatchProductList(
        int count,
        int batchId = 1,
        decimal baseCostPrice = 800m)
    {
        var products = new List<BatchProduct>();
        for (int i = 1; i <= count; i++)
        {
            products.Add(CreateBatchProduct(
                id: i,
                batchId: batchId,
                productId: i,
                quantity: 10 + i,
                costPrice: baseCostPrice + (i * 50)
            ));
        }
        return products;
    }

    #endregion

    #region Account Factory Methods

    /// <summary>
    /// Creates a test Account with default or specified values
    /// </summary>
    public static Account CreateAccount(
        int id = 1,
        string username = "testuser",
        string passwordHash = "hashedpassword123",
        int personId = 1,
        bool isActive = true)
    {
        return new Account
        {
            Id = id,
            Username = username,
            PasswordHash = passwordHash,
            PersonId = personId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            LastLogin = null,
            Person = CreateEmployee(personId, "Test Person")
        };
    }

    /// <summary>
    /// Creates a test Customer with default or specified values
    /// </summary>
    public static Customer CreateCustomer(
        int id = 1,
        string fullName = "Test Customer",
        string? email = "customer@test.com",
        string? phone = "0REMOVED_SECRET789",
        string? address = "123 Test Street",
        bool isActive = true)
    {
        return new Customer
        {
            Id = id,
            FullName = fullName,
            Email = email,
            Phone = phone,
            Address = address,
            PersonType = PersonType.CUSTOMER,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            Code = $"CUS{id:D5}"
        };
    }


    /// <summary>
    /// Creates a test Employee with default or specified values
    /// </summary>
    public static Employee CreateEmployee(
        int id = 1,
        string fullName = "Test Employee",
        string? email = "employee@test.com",
        string? phone = "0987654321",
        DateTime? hireDate = null,
        bool isActive = true)
    {
        return new Employee
        {
            Id = id,
            FullName = fullName,
            Email = email,
            Phone = phone,
            HireDate = hireDate ?? DateTime.UtcNow.AddMonths(-6),
            PersonType = PersonType.EMPLOYEE,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            Code = $"EMP{id:D5}"
        };
    }

    /// <summary>
    /// Creates a list of test Employees
    /// </summary>
    public static List<Employee> CreateEmployeeList(int count)
    {
        var employees = new List<Employee>();
        for (int i = 1; i <= count; i++)
        {
            employees.Add(CreateEmployee(
                id: i,
                fullName: $"Employee {i}",
                email: $"employee{i}@test.com",
                phone: $"098765432{i % 10}",
                hireDate: DateTime.UtcNow.AddMonths(-i),
                isActive: i % 3 != 0 // Every 3rd employee is inactive
            ));
        }
        return employees;
    }

    /// <summary>
    /// Creates a list of test Customers
    /// </summary>
    public static List<Customer> CreateCustomerList(int count)
    {
        var customers = new List<Customer>();
        for (int i = 1; i <= count; i++)
        {
            customers.Add(CreateCustomer(
                id: i,
                fullName: $"Customer {i}",
                email: $"customer{i}@test.com",
                phone: $"0REMOVED_SECRET78{i % 10}",
                address: $"{i} Test Street",
                isActive: i % 3 != 0 // Every 3rd customer is inactive
            ));
        }
        return customers;
    }

    #endregion
}
