// Helpers/OrderUIHelper.cs
using System.Globalization;

namespace PhoneStoreUser.Helper;
public static class OrderUIHelper
{
    public static string FormatCurrency(decimal value) => 
        string.Format(new CultureInfo("vi-VN"), "{0:C0}", value);

    public static string GetStatusLabel(string status) => status switch {
        "unpaid" => "Chưa thanh toán",
        "pending" => "Chờ duyệt (COD)",
        "paid" => "Đã thanh toán",
        "delivering" => "Đang giao hàng",
        "completed" => "Hoàn tất",
        "cancelled" => "Đã hủy",
        "refunded" => "Đã hoàn tiền",
        _ => status
    };

    public static (string Class, string TextClass, string Icon) GetStatusStyle(string status) => status switch {
        "unpaid" => ("bg-slate-100 text-slate-600", "text-slate-600", "fa-solid fa-circle-notch"),
        "pending" => ("bg-amber-50 text-amber-600 border border-amber-100", "text-amber-600", "fa-solid fa-clock"),
        "paid" => ("bg-cyan-50 text-cyan-700 border border-cyan-100", "text-cyan-700", "fa-solid fa-circle-dollar-to-slot"),
        "delivering" => ("bg-blue-50 text-blue-600 border border-blue-100", "text-blue-600", "fa-solid fa-truck-fast"),
        "completed" => ("bg-emerald-50 text-emerald-600 border border-emerald-100", "text-emerald-600", "fa-solid fa-check-double"),
        "cancelled" => ("bg-rose-50 text-rose-600 border border-rose-100", "text-rose-600", "fa-solid fa-ban"),
        _ => ("bg-gray-50 text-gray-600", "text-gray-600", "fa-solid fa-circle")
    };
}