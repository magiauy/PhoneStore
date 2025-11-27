using System.Collections.Generic;
using MySqlConnector;
using PhoneStoreRepository.Models;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IBatchProductRepository
    {
        IEnumerable<BatchProduct> GetAll();
        BatchProduct GetById(int id);
        void Insert(BatchProduct entity);
        void Update(BatchProduct entity);
        void Delete(int id);
        ICollection<BatchProduct> GetByBatchId(int batchId);
        void DeleteByBatchId(int batchId);

        #region Filtered Search
        int GetTotalRecords(int? batchId, int? productId, int? minQuantity, int? maxQuantity, decimal? minCostPrice, decimal? maxCostPrice, decimal? minSellingPrice, decimal? maxSellingPrice);
        IEnumerable<BatchProduct> GetBatchProductsFiltered(int? batchId, int? productId, int? minQuantity, int? maxQuantity, decimal? minCostPrice, decimal? maxCostPrice, decimal? minSellingPrice, decimal? maxSellingPrice, int page = 1, int pageSize = 20);
        int GetTotalPages(int? batchId, int? productId, int? minQuantity, int? maxQuantity, decimal? minCostPrice, decimal? maxCostPrice, decimal? minSellingPrice, decimal? maxSellingPrice, int pageSize);
        #endregion

        /// <summary>
        /// Insert a batch product using an existing transaction
        /// </summary>
        void Insert(BatchProduct entity, MySqlConnection connection, MySqlTransaction transaction);

        /// <summary>
        /// Delete batch products by batch ID using an existing transaction
        /// </summary>
        void DeleteByBatchId(int batchId, MySqlConnection connection, MySqlTransaction transaction);

        /// <summary>
        /// Decrease quantity for a product in a batch (used after sale)
        /// </summary>
        void DecreaseQuantity(int batchId, int productId, int amount = 1);

        IDictionary<int, int> GetQuantitiesByProductIds(IEnumerable<int> productIds);
    }
}
