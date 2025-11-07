using PhoneStoreAdmin.Models;
using MySqlConnector;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IBatchesRepository
    {
        IEnumerable<Batches> GetAll();
        Batches GetById(int id);
        void Insert(Batches entity);
        void Update(Batches entity);
        void Delete(int id);
        ICollection<Batches> GetByPurchaseOrderId(int purchaseOrderId);
        void DeleteByPurchaseOrderId(int purchaseOrderId);

        #region Filtered Search
        int GetTotalRecords(int? purchaseOrderId, int? supplierId, string? batchCode, DateTime? fromDate, DateTime? toDate, string? note);
        IEnumerable<Batches> GetBatchesFiltered(int? purchaseOrderId, int? supplierId, string? batchCode, DateTime? fromDate, DateTime? toDate, string? note, int page = 1, int pageSize = 20);
        int GetTotalPages(int? purchaseOrderId, int? supplierId, string? batchCode, DateTime? fromDate, DateTime? toDate, string? note, int pageSize);
        #endregion

        /// <summary>
        /// Insert a batch using an existing transaction
        /// </summary>
        void Insert(Batches entity, MySqlConnection connection, MySqlTransaction transaction);
 
        /// <summary>
 /// Delete batches by purchase order ID using an existing transaction
        /// </summary>
   void DeleteByPurchaseOrderId(int purchaseOrderId, MySqlConnection connection, MySqlTransaction transaction);
    }
}