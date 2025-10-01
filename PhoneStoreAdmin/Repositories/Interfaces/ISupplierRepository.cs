using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        /// Lấy danh sách nhà cung cấp theo bộ lọc + phân trang.
        IEnumerable<Supplier> GetSuppliersFiltered(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int page = 1,
            int pageSize = 20);

        /// Tính tổng số trang dựa trên bộ lọc và kích thước trang.
        int GetTotalPages(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int pageSize);

        /// Tính tổng số bản ghi dựa trên bộ lọc.
        int GetTotalRecords(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive);
    }
}
