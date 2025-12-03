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
        RESOLVED_CLEARANCE = 2
    }
}
