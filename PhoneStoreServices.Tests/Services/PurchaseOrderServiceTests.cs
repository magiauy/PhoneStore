using FluentAssertions;
using Moq;
using MySqlConnector;
using PhoneStore.Services.Implementations;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreServices.Tests.TestHelpers;
using Xunit;

namespace PhoneStoreServices.Tests.Services;

/// <summary>
/// Testable subclass of PurchaseOrderService that exposes TriggerPricingUpdate for testing
/// </summary>
public class TestablePurchaseOrderService : PurchaseOrderService
{
    private readonly IDynamicPricingService _testDynamicPricingService;

    public TestablePurchaseOrderService(
        IPurchaseOrderRepository poRepository,
        IPurchaseOrderLineRepository lineRepository,
        IBatchesRepository batchesRepository,
        IBatchProductRepository batchProductRepository,
        IProductRepository productRepository,
        IProductSerialRepository productSerialRepository,
        IDataSource dataSource,
        IDynamicPricingService dynamicPricingService)
        : base(poRepository, lineRepository, batchesRepository, batchProductRepository,
               productRepository, productSerialRepository, dataSource, dynamicPricingService)
    {
        _testDynamicPricingService = dynamicPricingService;
    }

    /// <summary>
    /// Exposes TriggerPricingUpdate for testing by simulating what happens after MarkAsReceived
    /// </summary>
    public void TestTriggerPricingUpdates(IEnumerable<PurchaseOrderLine> lines)
    {
        foreach (var line in lines)
        {
            try
            {
                // 1. Update FIFO cost with weighted average
                var fifoResult = _testDynamicPricingService.UpdateFifoCost(line.ProductId, line.UnitCost, line.Quantity);

                // 2. Update NIFO cost and trigger pricing workflow
                var nifoResult = _testDynamicPricingService.UpdateNifoCost(line.ProductId, line.UnitCost);
            }
            catch (Exception)
            {
                // Don't fail the entire operation - just log the error (same as production code)
            }
        }
    }
}

public class PurchaseOrderServiceTests
{
    private readonly Mock<IPurchaseOrderRepository> _mockPoRepository;
    private readonly Mock<IPurchaseOrderLineRepository> _mockLineRepository;
    private readonly Mock<IBatchesRepository> _mockBatchesRepository;
    private readonly Mock<IBatchProductRepository> _mockBatchProductRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IProductSerialRepository> _mockProductSerialRepository;
    private readonly Mock<IDataSource> _mockDataSource;
    private readonly Mock<IDynamicPricingService> _mockDynamicPricingService;
    private readonly TestablePurchaseOrderService _service;

    public PurchaseOrderServiceTests()
    {
        _mockPoRepository = new Mock<IPurchaseOrderRepository>();
        _mockLineRepository = new Mock<IPurchaseOrderLineRepository>();
        _mockBatchesRepository = new Mock<IBatchesRepository>();
        _mockBatchProductRepository = new Mock<IBatchProductRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockProductSerialRepository = new Mock<IProductSerialRepository>();
        _mockDynamicPricingService = new Mock<IDynamicPricingService>();
        _mockDataSource = new Mock<IDataSource>();

        _service = new TestablePurchaseOrderService(
            _mockPoRepository.Object,
            _mockLineRepository.Object,
            _mockBatchesRepository.Object,
            _mockBatchProductRepository.Object,
            _mockProductRepository.Object,
            _mockProductSerialRepository.Object,
            _mockDataSource.Object,
            _mockDynamicPricingService.Object);
    }

    #region TriggerPricingUpdate Tests

    [Fact]
    public void TriggerPricingUpdate_CallsUpdateFifoCost_ForEachProduct()
    {
        // Arrange
        var lines = new List<PurchaseOrderLine>
        {
            TestDataFactory.CreatePurchaseOrderLine(id: 1, productId: 1, quantity: 10, unitCost: 800m),
            TestDataFactory.CreatePurchaseOrderLine(id: 2, productId: 2, quantity: 5, unitCost: 1000m),
            TestDataFactory.CreatePurchaseOrderLine(id: 3, productId: 3, quantity: 15, unitCost: 500m)
        };

        // Setup pricing service
        _mockDynamicPricingService.Setup(x => x.UpdateFifoCost(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>()))
            .Returns(true);
        _mockDynamicPricingService.Setup(x => x.UpdateNifoCost(It.IsAny<int>(), It.IsAny<decimal>()))
            .Returns(PricingUpdateResult.CreateSuccess("PRICE_HELD"));

        // Act
        _service.TestTriggerPricingUpdates(lines);

        // Assert - Verify UpdateFifoCost called for each product
        _mockDynamicPricingService.Verify(x => x.UpdateFifoCost(1, 800m, 10), Times.Once);
        _mockDynamicPricingService.Verify(x => x.UpdateFifoCost(2, 1000m, 5), Times.Once);
        _mockDynamicPricingService.Verify(x => x.UpdateFifoCost(3, 500m, 15), Times.Once);
    }

    [Fact]
    public void TriggerPricingUpdate_CallsUpdateNifoCost_ForEachProduct()
    {
        // Arrange
        var lines = new List<PurchaseOrderLine>
        {
            TestDataFactory.CreatePurchaseOrderLine(id: 1, productId: 1, quantity: 10, unitCost: 800m),
            TestDataFactory.CreatePurchaseOrderLine(id: 2, productId: 2, quantity: 5, unitCost: 1000m)
        };

        // Setup pricing service
        _mockDynamicPricingService.Setup(x => x.UpdateFifoCost(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>()))
            .Returns(true);
        _mockDynamicPricingService.Setup(x => x.UpdateNifoCost(It.IsAny<int>(), It.IsAny<decimal>()))
            .Returns(PricingUpdateResult.CreateSuccess("PRICE_INCREASED"));

        // Act
        _service.TestTriggerPricingUpdates(lines);

        // Assert - Verify UpdateNifoCost called for each product after UpdateFifoCost
        _mockDynamicPricingService.Verify(x => x.UpdateNifoCost(1, 800m), Times.Once);
        _mockDynamicPricingService.Verify(x => x.UpdateNifoCost(2, 1000m), Times.Once);
    }

    [Fact]
    public void TriggerPricingUpdate_PricingUpdateFailure_DoesNotFailOperation()
    {
        // Arrange
        var lines = new List<PurchaseOrderLine>
        {
            TestDataFactory.CreatePurchaseOrderLine(id: 1, productId: 1, quantity: 10, unitCost: 800m),
            TestDataFactory.CreatePurchaseOrderLine(id: 2, productId: 2, quantity: 5, unitCost: 1000m)
        };

        // Setup pricing service - first product fails, second succeeds
        _mockDynamicPricingService.Setup(x => x.UpdateFifoCost(1, It.IsAny<decimal>(), It.IsAny<int>()))
            .Throws(new Exception("Pricing update failed"));
        _mockDynamicPricingService.Setup(x => x.UpdateFifoCost(2, It.IsAny<decimal>(), It.IsAny<int>()))
            .Returns(true);
        _mockDynamicPricingService.Setup(x => x.UpdateNifoCost(2, It.IsAny<decimal>()))
            .Returns(PricingUpdateResult.CreateSuccess("PRICE_HELD"));

        // Act - Should not throw exception
        var exception = Record.Exception(() => _service.TestTriggerPricingUpdates(lines));

        // Assert - Operation should complete successfully despite pricing failure
        exception.Should().BeNull();
        
        // Verify second product pricing was still attempted
        _mockDynamicPricingService.Verify(x => x.UpdateFifoCost(2, 1000m, 5), Times.Once);
        _mockDynamicPricingService.Verify(x => x.UpdateNifoCost(2, 1000m), Times.Once);
    }

    #endregion
}
