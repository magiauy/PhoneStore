namespace PhoneStoreRepository.Models.Enums
{
    /// <summary>
    /// Loại cảnh báo định giá
    /// </summary>
    public enum AlertType
    {
        /// <summary>
        /// Cảnh báo giá nhập giảm (Market DOWN)
        /// Khi variance <= VarianceThreshold (-10%)
        /// </summary>
        PRICE_DROP = 0,

        /// <summary>
        /// Cảnh báo có thể thoát chế độ xả hàng (Market Recovery)
        /// Khi đang CLEARANCE mode và variance >= RecoveryThreshold (+10%)
        /// </summary>
        CLEARANCE_RECOVERY = 1
    }
}
