namespace PhoneStoreRepository.Models.Enums
{
    /// <summary>
    /// Trạng thái của cảnh báo định giá
    /// </summary>
    public enum AlertStatus
    {
        /// <summary>
        /// Cảnh báo đang chờ xử lý
        /// </summary>
        PENDING = 0,

        /// <summary>
        /// Đã xử lý - Admin chọn giữ giá
        /// </summary>
        RESOLVED_HOLD = 1,

        /// <summary>
        /// Đã xử lý - Admin kích hoạt chế độ xả hàng
        /// </summary>
        RESOLVED_CLEARANCE = 2,

        /// <summary>
        /// Đã xử lý - Admin reset từ CLEARANCE về AUTO_PROTECT
        /// Khi market hồi phục và admin quyết định thoát chế độ xả hàng
        /// </summary>
        RESOLVED_RESET_AUTO = 3
    }
}
