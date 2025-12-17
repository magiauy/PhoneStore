using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IAdminCategoryService
{
    Task<PagedResult<AdminCategoryDto>> GetCategoriesAsync(int page, int pageSize, string? searchTerm = null);
    Task<List<AdminCategoryDto>> GetAllCategoriesAsync();
    Task<AdminCategoryDto?> GetCategoryByIdAsync(int id);
    Task<bool> CreateCategoryAsync(CreateCategoryDto dto);
    Task<bool> UpdateCategoryAsync(int id, UpdateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(int id);
}

public class AdminCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? Note { get; set; }
    public int ProductCount { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Note { get; set; }
}

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Note { get; set; }
}
