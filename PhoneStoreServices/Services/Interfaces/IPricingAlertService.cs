using System.Collections.Generic;
using PhoneStore.Services.ViewModels;

namespace PhoneStore.Services.Interfaces
{
    /// <summary>
    /// Service interface cho quản lý cảnh báo định giá
    /// Xử lý các quyết định của Admin khi thị trường giảm giá
    /// </summary>
    public interface IPricingAlertService
    {
        /// <summary>
        /// Lấy danh sách cảnh báo đang chờ xử lý
        /// </summary>
        /// <returns>Danh sách cảnh báo pending</returns>
        IReadOnlyList<PricingAlertViewModel> GetPendingAlerts();

        /// <summary>
        /// Xử lý quyết định "Giữ giá" - Admin chọn giữ nguyên giá bán hiện tại
        /// Cập nhật status thành RESOLVED_HOLD, không thay đổi giá sản phẩm
        /// </summary>
        /// <param name="alertId">ID cảnh báo</param>
        /// <param name="adminId">ID Admin xử lý</param>
        /// <param name="note">Ghi chú (tùy chọn)</param>
        /// <returns>True nếu xử lý thành công</returns>
        bool ResolveAsHold(int alertId, int adminId, string? note);

        /// <summary>
        /// Xử lý quyết định "Kích hoạt xả hàng" - Admin chọn chuyển sang chế độ CLEARANCE
        /// Cập nhật status thành RESOLVED_CLEARANCE, chuyển PricingMode và tính lại giá
        /// </summary>
        /// <param name="alertId">ID cảnh báo</param>
        /// <param name="adminId">ID Admin xử lý</param>
        /// <param name="note">Ghi chú (tùy chọn)</param>
        /// <returns>True nếu xử lý thành công</returns>
        bool ResolveAsClearance(int alertId, int adminId, string? note);

        /// <summary>
        /// Tạo cảnh báo mới khi phát hiện thị trường giảm giá
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <param name="variance">Tỷ lệ chênh lệch (%)</param>
        /// <param name="costFifo">Giá vốn FIFO tại thời điểm tạo alert</param>
        /// <param name="costNifo">Giá NIFO tại thời điểm tạo alert</param>
        /// <param name="stock">Số lượng tồn kho</param>
        void CreateAlert(int productId, decimal variance, decimal costFifo, decimal costNifo, int stock);

        /// <summary>
        /// Lấy cảnh báo theo ID
        /// </summary>
        /// <param name="alertId">ID cảnh báo</param>
        /// <returns>ViewModel cảnh báo hoặc null nếu không tìm thấy</returns>
        PricingAlertViewModel? GetAlertById(int alertId);

        /// <summary>
        /// Lấy danh sách cảnh báo theo sản phẩm
        /// </summary>
        /// <param name="productId">ID sản phẩm</param>
        /// <returns>Danh sách cảnh báo của sản phẩm</returns>
        IReadOnlyList<PricingAlertViewModel> GetAlertsByProduct(int productId);

        /// <summary>
        /// Lấy preview giá mới khi reset từ CLEARANCE về AUTO_PROTECT
        /// Cho phép admin xem trước giá mới trước khi quyết định
        /// </summary>
        /// <param name="alertId">ID cảnh báo (phải là loại CLEARANCE_RECOVERY)</param>
        /// <returns>Preview với giá hiện tại và giá mới, hoặc null nếu không hợp lệ</returns>
        RecoveryPreviewViewModel? GetRecoveryPreview(int alertId);

        /// <summary>
        /// Xử lý quyết định "Reset về Auto" - Admin chọn thoát chế độ CLEARANCE
        /// Reset CostFifo = CostNifo, chuyển PricingMode về AUTO_PROTECT và tính lại giá
        /// </summary>
        /// <param name="alertId">ID cảnh báo (phải là loại CLEARANCE_RECOVERY)</param>
        /// <param name="adminId">ID Admin xử lý</param>
        /// <param name="note">Ghi chú (tùy chọn)</param>
        /// <returns>True nếu xử lý thành công</returns>
        bool ResolveAsResetAuto(int alertId, int adminId, string? note);
    }
}
