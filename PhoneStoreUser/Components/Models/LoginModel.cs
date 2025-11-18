using System.ComponentModel.DataAnnotations;

namespace PhoneStoreUser.Components.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Email hoặc username không được bỏ trống.")]
    public string? EmailOrUsername { get; set; }

    [Required(ErrorMessage = "Mật khẩu tối thiểu 6 ký tự."), MinLength(6)]
    public string? Password { get; set; }

    public bool RememberMe { get; set; }
}
