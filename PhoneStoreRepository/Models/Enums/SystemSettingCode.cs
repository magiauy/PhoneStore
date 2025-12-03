namespace PhoneStoreRepository.Models.Enums
{
    /// <summary>
    /// Enum định nghĩa tất cả các mã cấu hình hệ thống.
    /// Các setting này được khởi tạo sẵn trong database và chỉ cho phép chỉnh sửa giá trị.
    /// </summary>
    public enum SystemSettingCode
    {
        /// <summary>
        /// Biên độ lợi nhuận mặc định (percentage, ví dụ: 0.20 = 20%)
        /// </summary>
        PROFIT_MARGIN,
    }
}
