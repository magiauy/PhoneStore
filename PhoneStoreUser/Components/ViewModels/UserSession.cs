namespace PhoneStoreUser.Components.ViewModels;

public class UserSession
{
    public int AccountId { get; set; }
    public int PersonId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool RememberMe { get; set; }
    public DateTimeOffset LoginTime { get; set; } = DateTimeOffset.UtcNow;

    public bool IsAuthenticated => AccountId > 0;
}
