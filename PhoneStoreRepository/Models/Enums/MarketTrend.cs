namespace PhoneStoreRepository.Models.Enums
{
    /// <summary>
    /// Xu hướng thị trường được xác định qua so sánh FIFO và NIFO
    /// </summary>
    public enum MarketTrend
    {
        /// <summary>
        /// Thị trường ổn định (Variance từ -5% đến 0%)
        /// </summary>
        STABLE = 0,

        /// <summary>
        /// Thị trường tăng giá (Variance > 0%)
        /// </summary>
        UP = 1,

        /// <summary>
        /// Thị trường giảm giá (Variance <= Variance_Threshold)
        /// </summary>
        DOWN = 2
    }
}
