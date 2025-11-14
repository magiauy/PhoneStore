using PhoneStoreRepository.Models;
using PhoneStore.Services.ViewModels;
using System;

namespace PhoneStore.Services.Interfaces
{
    public interface IBatchesService
    {
        Batches? GetById(int id);

        void Insert(Batches batch);

        void Update(Batches batch);

        void Delete(int id);

        BatchesResult GetBatchesFiltered(
            int? purchaseOrderId,
            int? supplierId,
            string? batchCode,
            DateTime? fromDate,
            DateTime? toDate,
            string? note,
            int page = 1,
            int pageSize = 10);
        void MarkAsProcessed(int id);
    }
}