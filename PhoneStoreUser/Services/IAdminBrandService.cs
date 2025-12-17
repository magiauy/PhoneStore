using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IAdminBrandService
{
    Task<PagedResult<AdminBrandDto>> GetBrandsAsync(int page, int pageSize, string? searchTerm = null);
    Task<AdminBrandDto?> GetBrandByIdAsync(int id);
    Task<bool> CreateBrandAsync(CreateBrandDto dto);
    Task<bool> UpdateBrandAsync(int id, UpdateBrandDto dto);
    Task<bool> DeleteBrandAsync(int id);
    Task<int> GetProductCountByBrandAsync(int brandId);
}

public class AdminBrandDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}

public class CreateBrandDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateBrandDto
{
    public string Name { get; set; } = string.Empty;
}
