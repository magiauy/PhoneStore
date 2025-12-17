using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class DisplaySettingsService : IDisplaySettingsService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private const string NavbarCategoryKey = "navbar_category";
    private const string NavbarBrandKey = "navbar_brand";
    private const string HomeBrandKey = "home_brand";

    public DisplaySettingsService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Navbar Items (Categories + Brands)

    public async Task<List<NavbarItemDto>> GetNavbarItemsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var categorySettings = await context.DisplaySettings
            .Where(s => s.SettingKey == NavbarCategoryKey && s.IsActive && s.EntityId.HasValue)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        var brandSettings = await context.DisplaySettings
            .Where(s => s.SettingKey == NavbarBrandKey && s.IsActive && s.EntityId.HasValue)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        // Nếu chưa có cài đặt nào, trả về fallback
        if (!categorySettings.Any() && !brandSettings.Any())
        {
            var fallbackCategories = await context.Categories
                .OrderBy(c => c.Name)
                .Take(6)
                .Select(c => new NavbarItemDto
                {
                    Id = c.Id,
                    Name = c.Name ?? string.Empty,
                    ItemType = NavbarItemType.Category,
                    SortOrder = 0,
                    IsSelected = true
                })
                .ToListAsync();
            return fallbackCategories;
        }

        var result = new List<NavbarItemDto>();

        // Lấy categories đã chọn
        if (categorySettings.Any())
        {
            var categoryIds = categorySettings.Select(s => s.EntityId!.Value).ToList();
            var categories = await context.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name ?? string.Empty);

            result.AddRange(categorySettings
                .Where(s => categories.ContainsKey(s.EntityId!.Value))
                .Select(s => new NavbarItemDto
                {
                    Id = s.EntityId!.Value,
                    Name = categories[s.EntityId!.Value],
                    ItemType = NavbarItemType.Category,
                    SortOrder = s.SortOrder,
                    IsSelected = true
                }));
        }

        // Lấy brands đã chọn
        if (brandSettings.Any())
        {
            var brandIds = brandSettings.Select(s => s.EntityId!.Value).ToList();
            var brands = await context.Brands
                .Where(b => brandIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Name);

            result.AddRange(brandSettings
                .Where(s => brands.ContainsKey(s.EntityId!.Value))
                .Select(s => new NavbarItemDto
                {
                    Id = s.EntityId!.Value,
                    Name = brands[s.EntityId!.Value],
                    ItemType = NavbarItemType.Brand,
                    SortOrder = s.SortOrder,
                    IsSelected = true
                }));
        }

        return result.OrderBy(x => x.SortOrder).ToList();
    }

    public async Task<List<NavbarItemDto>> GetAllItemsForNavbarSettingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Lấy settings hiện tại
        var categorySettings = await context.DisplaySettings
            .Where(s => s.SettingKey == NavbarCategoryKey && s.IsActive && s.EntityId.HasValue)
            .ToDictionaryAsync(s => s.EntityId!.Value, s => s.SortOrder);

        var brandSettings = await context.DisplaySettings
            .Where(s => s.SettingKey == NavbarBrandKey && s.IsActive && s.EntityId.HasValue)
            .ToDictionaryAsync(s => s.EntityId!.Value, s => s.SortOrder);

        var result = new List<NavbarItemDto>();

        // Thêm tất cả categories
        var categories = await context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        result.AddRange(categories.Select(c => new NavbarItemDto
        {
            Id = c.Id,
            Name = c.Name ?? string.Empty,
            ItemType = NavbarItemType.Category,
            SortOrder = categorySettings.TryGetValue(c.Id, out var order) ? order : 999,
            IsSelected = categorySettings.ContainsKey(c.Id)
        }));

        // Thêm tất cả brands
        var brands = await context.Brands
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();

        result.AddRange(brands.Select(b => new NavbarItemDto
        {
            Id = b.Id,
            Name = b.Name,
            ItemType = NavbarItemType.Brand,
            SortOrder = brandSettings.TryGetValue(b.Id, out var order) ? order : 999,
            IsSelected = brandSettings.ContainsKey(b.Id)
        }));

        // Sắp xếp: đã chọn lên trước theo thứ tự, sau đó là chưa chọn
        return result
            .OrderBy(x => x.IsSelected ? 0 : 1)
            .ThenBy(x => x.IsSelected ? x.SortOrder : 1000)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public async Task SaveNavbarItemsAsync(List<NavbarItemSaveDto> items)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Xóa settings cũ của cả categories và brands
        var existingSettings = await context.DisplaySettings
            .Where(s => s.SettingKey == NavbarCategoryKey || s.SettingKey == NavbarBrandKey)
            .ToListAsync();
        context.DisplaySettings.RemoveRange(existingSettings);

        // Thêm settings mới
        foreach (var item in items)
        {
            var settingKey = item.ItemType == NavbarItemType.Category ? NavbarCategoryKey : NavbarBrandKey;
            context.DisplaySettings.Add(new DisplaySettingEntity
            {
                SettingKey = settingKey,
                EntityId = item.Id,
                SortOrder = item.SortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    #endregion

    #region Home Brands

    public async Task<List<BrandDisplayDto>> GetHomeBrandsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.DisplaySettings
            .Where(s => s.SettingKey == HomeBrandKey && s.IsActive && s.EntityId.HasValue)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        if (!settings.Any())
        {
            // Fallback: trả về tất cả brands nếu chưa có cài đặt
            return await context.Brands
                .OrderBy(b => b.Name)
                .Take(6)
                .Select(b => new BrandDisplayDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    SortOrder = 0,
                    IsSelected = true
                })
                .ToListAsync();
        }

        var brandIds = settings.Select(s => s.EntityId!.Value).ToList();
        var brands = await context.Brands
            .Where(b => brandIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name);

        return settings
            .Where(s => brands.ContainsKey(s.EntityId!.Value))
            .Select(s => new BrandDisplayDto
            {
                Id = s.EntityId!.Value,
                Name = brands[s.EntityId!.Value],
                SortOrder = s.SortOrder,
                IsSelected = true
            })
            .ToList();
    }

    public async Task<List<BrandDisplayDto>> GetAllBrandsForSettingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.DisplaySettings
            .Where(s => s.SettingKey == HomeBrandKey && s.IsActive && s.EntityId.HasValue)
            .ToDictionaryAsync(s => s.EntityId!.Value, s => s.SortOrder);

        var brands = await context.Brands
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();

        return brands.Select(b => new BrandDisplayDto
        {
            Id = b.Id,
            Name = b.Name,
            SortOrder = settings.TryGetValue(b.Id, out var order) ? order : 999,
            IsSelected = settings.ContainsKey(b.Id)
        })
        .OrderBy(b => b.IsSelected ? b.SortOrder : 1000 + b.Id)
        .ToList();
    }

    public async Task SaveHomeBrandsAsync(List<int> brandIds)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Xóa settings cũ
        var existingSettings = await context.DisplaySettings
            .Where(s => s.SettingKey == HomeBrandKey)
            .ToListAsync();
        context.DisplaySettings.RemoveRange(existingSettings);

        // Thêm settings mới
        for (int i = 0; i < brandIds.Count; i++)
        {
            context.DisplaySettings.Add(new DisplaySettingEntity
            {
                SettingKey = HomeBrandKey,
                EntityId = brandIds[i],
                SortOrder = i,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    #endregion
}
