using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface cho quản lý lịch sử biến động giá
    /// </summary>
    public interface IPricingHistoryRepository
    {
        /// <summary>
        /// Thêm bản ghi lịch sử giá mới
        /// </summary>
        void Insert(PricingHistory history);

        /// <summary>
        /// Lấy lịch sử giá theo sản phẩm
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <param name="limit">Số lượng bản ghi tối đa (mặc định 50)</param>
        IReadOnlyList<PricingHistory> GetByProduct(int productId, int limit = 50);

        /// <summary>
        /// Lấy các bản ghi lịch sử giá gần đây nhất
        /// </summary>
        /// <param name="limit">Số lượng bản ghi tối đa (mặc định 100)</param>
        IReadOnlyList<PricingHistory> GetRecent(int limit = 100);
    }
}
