using System.ComponentModel.DataAnnotations;

namespace PhoneStoreUser.Components.Models;

public class ProfileModel
{
    public int PersonId { get; set; }

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    public string? Address { get; set; }
}
