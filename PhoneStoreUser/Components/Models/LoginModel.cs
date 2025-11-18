using System.ComponentModel.DataAnnotations;

namespace PhoneStoreUser.Components.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Email hoặc username không được bỏ trống.")]
    [MinLength(3, ErrorMessage = "Email hoặc username tối thiểu 3 ký tự.")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được bỏ trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
