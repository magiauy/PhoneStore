namespace PhoneStoreRepository.Models.Enums
{
    /// <summary>
    /// Chế độ định giá của sản phẩm
    /// </summary>
    public enum PricingMode
    {
        /// <summary>
        /// Chế độ định giá mặc định, tự động bảo vệ dòng tiền khi thị trường tăng
        /// </summary>
        AUTO_PROTECT = 0,

        /// <summary>
        /// Chế độ xả hàng, áp dụng khi Admin phê duyệt cắt lỗ
        /// </summary>
        CLEARANCE = 1
    }
}
