using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface cho quản lý cảnh báo định giá
    /// </summary>
    public interface IPricingAlertRepository
    {
        /// <summary>
        /// Lấy cảnh báo theo ID
        /// </summary>
        PricingAlert? GetById(int id);

        /// <summary>
        /// Lấy tất cả cảnh báo đang chờ xử lý (PENDING)
        /// </summary>
        IReadOnlyList<PricingAlert> GetPendingAlerts();

        /// <summary>
        /// Lấy danh sách cảnh báo theo sản phẩm
        /// </summary>
        IReadOnlyList<PricingAlert> GetAlertsByProduct(int productId);

        /// <summary>
        /// Thêm cảnh báo mới
        /// </summary>
        void Insert(PricingAlert alert);

        /// <summary>
        /// Cập nhật cảnh báo
        /// </summary>
        void Update(PricingAlert alert);
    }
}
