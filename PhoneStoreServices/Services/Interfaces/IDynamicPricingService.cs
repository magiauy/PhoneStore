using System.Collections.Generic;
using PhoneStore.Services.ViewModels;

namespace PhoneStore.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Dynamic Pricing Engine
    /// Quản lý việc tính toán và cập nhật giá bán tự động dựa trên biến động chi phí thị trường
    /// </summary>
    public interface IDynamicPricingService
    {
        /// <summary>
        /// Cập nhật NIFO cost và trigger pricing workflow
        /// Tự động tính toán variance, xác định market trend và cập nhật giá hoặc tạo alert
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <param name="newNifoCost">Giá NIFO mới từ lô hàng nhập</param>
        /// <returns>Kết quả cập nhật giá</returns>
        PricingUpdateResult UpdateNifoCost(int productId, decimal newNifoCost);

        /// <summary>
        /// Tính toán giá bán theo công thức AUTO_PROTECT
        /// Formula: MAX(FIFO, NIFO) × (1 + DesiredMargin)
        /// </summary>
        /// <param name="costFifo">Giá vốn FIFO</param>
        /// <param name="costNifo">Giá thay thế NIFO</param>
        /// <returns>Giá bán được tính toán</returns>
        decimal CalculateAutoProtectPrice(decimal costFifo, decimal costNifo);

        /// <summary>
        /// Tính toán giá bán theo công thức CLEARANCE
        /// Formula: NIFO × (1 + MinimumMargin)
        /// </summary>
        /// <param name="costNifo">Giá thay thế NIFO</param>
        /// <returns>Giá bán xả hàng</returns>
        decimal CalculateClearancePrice(decimal costNifo);

        /// <summary>
        /// Lấy cấu hình pricing hiện tại từ SettingString
        /// </summary>
        /// <returns>Cấu hình pricing</returns>
        PricingConfiguration GetConfiguration();

        /// <summary>
        /// Cập nhật cấu hình pricing vào SettingString
        /// </summary>
        /// <param name="config">Cấu hình mới</param>
        /// <returns>True nếu cập nhật thành công</returns>
        bool UpdateConfiguration(PricingConfiguration config);

        /// <summary>
        /// Lấy dữ liệu dashboard pricing
        /// Bao gồm: tổng sản phẩm, số sản phẩm tăng giá, số sản phẩm xả kho, số cảnh báo pending
        /// </summary>
        /// <returns>Dữ liệu dashboard</returns>
        PricingDashboardData GetDashboardData();

        /// <summary>
        /// Lấy lịch sử biến động giá của sản phẩm
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <returns>Danh sách lịch sử giá</returns>
        IReadOnlyList<PricingHistoryViewModel> GetPriceHistory(int productId);

        /// <summary>
        /// Tính toán variance giữa NIFO và FIFO
        /// Formula: (NIFO - FIFO) / FIFO
        /// </summary>
        /// <param name="costFifo">Giá vốn FIFO</param>
        /// <param name="costNifo">Giá thay thế NIFO</param>
        /// <returns>Tỷ lệ variance (decimal, ví dụ: 0.1 = 10%)</returns>
        decimal CalculateVariance(decimal costFifo, decimal costNifo);

        /// <summary>
        /// Cập nhật CostFifo sử dụng weighted average calculation
        /// Formula: ((OldFifo × OldStock) + (NewCost × NewQuantity)) / (OldStock + NewQuantity)
        /// Nếu OldStock = 0 hoặc OldFifo &lt;= 0, CostFifo sẽ được set bằng newCost
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <param name="newCost">Giá nhập mới</param>
        /// <param name="quantity">Số lượng nhập</param>
        /// <returns>True nếu cập nhật thành công</returns>
        bool UpdateFifoCost(int productId, decimal newCost, int quantity);
    }
}
