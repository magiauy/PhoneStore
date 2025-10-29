using PhoneStoreAdmin.Models;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPromotionRepository : IRepository<Promotion>
    {
        Promotion GetByName(string name);
        
        IEnumerable<Promotion> GetPromotionsFiltered(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize);

        int GetTotalRecords(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate);

        int GetTotalPages(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int pageSize);
    }
}
