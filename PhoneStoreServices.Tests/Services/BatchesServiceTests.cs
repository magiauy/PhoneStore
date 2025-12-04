using FluentAssertions;
using Moq;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models;
using PhoneStoreServices.Tests.TestHelpers;
using Xunit;

namespace PhoneStoreServices.Tests.Services;

/// <summary>
/// Test helper class that simulates the TriggerPricingUpdates logic from BatchesService
/// This allows testing the pricing update behavior without requiring database dependencies
/// </summary>
public class BatchesPricingUpdateHelper
{
    private readonly IDynamicPricingService _dynamicPricingService;

    public BatchesPricingUpdateHelper(IDynamicPricingService dynamicPricingService)
    {
        _dynamicPricingService = dynamicPricingService;
    }

    /// <summary>
    /// Simulates TriggerPricingUpdates from BatchesService
    /// Updates FIFO cost first (weighted average), then NIFO cost
    /// </summary>
    public void TriggerPricingUpdates(IEnumerable<BatchProduct> batchProducts)
    {
        foreach (var batchProduct in batchProducts)
        {
            try
            {
                // Step 1: Update FIFO cost first (weighted average calculation)
                var fifoResult = _dynamicPricingService.UpdateFifoCost(
                    batchProduct.ProductId,
                    batchProduct.CostPrice,
                    batchProduct.Quantity);

                // Step 2: Update NIFO cost and trigger pricing workflow
                var nifoResult = _dynamicPricingService.UpdateNifoCost(
                    batchProduct.ProductId,
                    batchProduct.CostPrice);
            }
            catch (Exception)
            {
                // Don't fail the entire operation - same as production code
            }
        }
    }
}

public class BatchesServiceTests
{
    private readonly Mock<IDynamicPricingService> _mockDynamicPricingService;
    private readonly BatchesPricingUpdateHelper _pricingHelper;

    public BatchesServiceTests()
    {
        _mockDynamicPricingService = new Mock<IDynamicPricingService>();
        _pricingHelper = new BatchesPricingUpdateHelper(_mockDynamicPricingService.Object);
    }

    #region TriggerPricingUpdates Tests

    [Fact]
    public void TriggerPricingUpdates_CallsUpdateFifoCost_ForEachBatchProduct()
    {
        // Arrange
        var batchProducts = new List<BatchProduct>
        {
            TestDataFactory.CreateBatchProduct(id: 1, productId: 1, quantity: 10, costPrice: 800m),
            TestDataFactory.CreateBatchProduct(id: 2, productId: 2, quantity: 5, costPrice: 1000m),
            TestDataFactory.CreateBatchProduct(id: 3, productId: 3, quantity: 15, costPrice: 500m)
        };

        // Setup pricing service
        _mockDynamicPricingService.Setup(x => x.UpdateFifoCost(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>()))
            .Returns(true);
        _mockDynamicPricingService.Setup(x => x.UpdateNifoCost(It.IsAny<int>(), It.IsAny<decimal>()))
            .Returns(PricingUpdateResult.CreateSuccess("PRICE_HELD"));

        // Act
        _pricingHelper.TriggerPricingUpdates(batchProducts);

        // Assert - Verify UpdateFifoCost called for each product
        _mockDynamicPricingService.Verify(x => x.UpdateFifoCost(1, 800m, 10), Times.Once);
        _mockDynamicPricingService.Verify(x => x.UpdateFifoCost(2, 1000m, 5), Times.Once);
        _mockDynamicPricingService.Verify(x => x.UpdateFifoCost(3, 500m, 15), Times.Once);
    }

    [Fact]
    public void TriggerPricingUpdates_CallsUpdateNifoCost_ForEachBatchProduct()
    {
        // Arrange
        var batchProducts = new List<BatchProduct>
        {
            TestDataFactory.CreateBatchProduct(id: 1, productId: 1, quantity: 10, costPrice: 800m),
            TestDataFactory.CreateBatchProduct(id: 2, productId: 2, quantity: 5, costPrice: 1000m)
        };

        // Setup pricing service
        _mockDynamicPricingService.Setup(x => x.UpdateFifoCost(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>()))
            .Returns(true);
        _mockDynamicPricingService.Setup(x => x.UpdateNifoCost(It.IsAny<int>(), It.IsAny<decimal>()))
            .Returns(PricingUpdateResult.CreateSuccess("PRICE_INCREASED"));

        // Act
        _pricingHelper.TriggerPricingUpdates(batchProducts);

        // Assert - Verify UpdateNifoCost called for each product after UpdateFifoCost
        _mockDynamicPricingService.Verify(x => x.UpdateNifoCost(1, 800m), Times.Once);
        _mockDynamicPricingService.Verify(x => x.UpdateNifoCost(2, 1000m), Times.Once);
    }

    [Fact]
    public void TriggerPricingUpdates_PricingUpdateFailure_DoesNotFailBatchCreation()
    {
        // Arrange
        var batchProducts = new List<BatchProduct>
        {
            TestDataFactory.CreateBatchProduct(id: 1, productId: 1, quantity: 10, costPrice: 800m),
            TestDataFactory.CreateBatchProduct(id: 2, productId: 2, quantity: 5, costPrice: 1000m)
        };

        // Setup pricing service - first product fails, second succeeds
        _mockDynamicPricingService.Setup(x => x.UpdateFifoCost(1, It.IsAny<decimal>(), It.IsAny<int>()))
            .Throws(new Exception("Pricing update failed"));
        _mockDynamicPricingService.Setup(x => x.UpdateFifoCost(2, It.IsAny<decimal>(), It.IsAny<int>()))
            .Returns(true);
        _mockDynamicPricingService.Setup(x => x.UpdateNifoCost(2, It.IsAny<decimal>()))
            .Returns(PricingUpdateResult.CreateSuccess("PRICE_HELD"));

        // Act - Should not throw exception
        var exception = Record.Exception(() => _pricingHelper.TriggerPricingUpdates(batchProducts));

        // Assert - Operation should complete successfully despite pricing failure
        exception.Should().BeNull();

        // Verify second product pricing was still attempted
        _mockDynamicPricingService.Verify(x => x.UpdateFifoCost(2, 1000m, 5), Times.Once);
        _mockDynamicPricingService.Verify(x => x.UpdateNifoCost(2, 1000m), Times.Once);
    }

    #endregion
}
