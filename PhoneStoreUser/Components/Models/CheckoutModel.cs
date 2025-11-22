using System.ComponentModel.DataAnnotations;

namespace PhoneStoreUser.Components.Models;

public class CheckoutModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    public string? Address { get; set; }
}
