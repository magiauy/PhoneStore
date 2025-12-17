namespace PhoneStoreUser.Services;

public interface IDisplaySettingsService
{
    // Navbar Items (Categories + Brands)
    Task<List<NavbarItemDto>> GetNavbarItemsAsync();
    Task<List<NavbarItemDto>> GetAllItemsForNavbarSettingsAsync();
    Task SaveNavbarItemsAsync(List<NavbarItemSaveDto> items);

    // Home Brands
    Task<List<BrandDisplayDto>> GetHomeBrandsAsync();
    Task<List<BrandDisplayDto>> GetAllBrandsForSettingsAsync();
    Task SaveHomeBrandsAsync(List<int> brandIds);
}

public enum NavbarItemType
{
    Category,
    Brand
}

public class NavbarItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NavbarItemType ItemType { get; set; }
    public int SortOrder { get; set; }
    public bool IsSelected { get; set; }
    public string Href => ItemType == NavbarItemType.Category
        ? "/products?catalog=" + Id
        : "/products?branch=" + Id;
}

public class NavbarItemSaveDto
{
    public int Id { get; set; }
    public NavbarItemType ItemType { get; set; }
    public int SortOrder { get; set; }
}

public class CategoryDisplayDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsSelected { get; set; }
}

public class BrandDisplayDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsSelected { get; set; }
}
