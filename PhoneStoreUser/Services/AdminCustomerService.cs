using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;


namespace PhoneStoreUser.Services;

public class AdminCustomerService : IAdminCustomerService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminCustomerService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

public async Task<PagedResult<AdminCustomerDto>> GetCustomersAsync(int page, int pageSize, string? searchTerm = null)
{
    // Bắt đầu đồng hồ tổng
    var totalStopwatch = Stopwatch.StartNew();
    
    await using var context = await _dbContextFactory.CreateDbContextAsync();

    page = page < 1 ? 1 : page;
    pageSize = pageSize <= 0 ? 10 : pageSize;

    // Normalize search term
    var normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim().ToLower();

    // --- 1. COUNT (Đã tối ưu) ---
    var countStopwatch = Stopwatch.StartNew();

    var customerCountQuery = context.Persons
        .AsNoTracking()
        .Where(p => p.PersonType == "CUSTOMER" && (p.IsActive ?? true));

    // Apply search filter if searchTerm is provided
    if (!string.IsNullOrEmpty(normalizedSearchTerm))
    {
        customerCountQuery = customerCountQuery.Where(p =>
            (p.FullName != null && p.FullName.ToLower().Contains(normalizedSearchTerm)) ||
            (p.Phone != null && p.Phone.ToLower().Contains(normalizedSearchTerm)) ||
            (p.Email != null && p.Email.ToLower().Contains(normalizedSearchTerm)));
    }

    var totalCount = await customerCountQuery.CountAsync();

    countStopwatch.Stop();
    Console.WriteLine($"[Performance] Count Query took: {countStopwatch.ElapsedMilliseconds} ms"); 

    if (totalCount == 0)
    {
        return new PagedResult<AdminCustomerDto>(new List<AdminCustomerDto>(), 0, page, pageSize);
    }

    // --- 2. FETCH DATA (Đã sửa lỗi logic) ---
    var fetchStopwatch = Stopwatch.StartNew();

    // Query cơ sở: Chỉ Join Person và Customer
    // LƯU Ý: Không còn Join vào Invoices nữa -> Tốc độ cực nhanh
    var baseQuery = context.Persons.AsNoTracking()
        .Where(p => p.PersonType == "CUSTOMER" && (p.IsActive ?? true));

    // Apply search filter if searchTerm is provided
    if (!string.IsNullOrEmpty(normalizedSearchTerm))
    {
        baseQuery = baseQuery.Where(p =>
            (p.FullName != null && p.FullName.ToLower().Contains(normalizedSearchTerm)) ||
            (p.Phone != null && p.Phone.ToLower().Contains(normalizedSearchTerm)) ||
            (p.Email != null && p.Email.ToLower().Contains(normalizedSearchTerm)));
    }

    var query = 
        from person in baseQuery
        // Left Join Customer để lấy thông tin TotalSpend/LastOrderDate có sẵn
        join customer in context.Customers.AsNoTracking() 
            on person.Id equals customer.PersonId into customerGroup
        from customer in customerGroup.DefaultIfEmpty()
        
        select new 
        {
            Person = person,
            Customer = customer
        };

    // Sắp xếp và Phân trang
    // Vì đã có cột LastOrderDate trong bảng Customer, việc sort này cực nhẹ
    var orderedQuery = query
        .OrderByDescending(x => x.Customer.LastOrderDate) 
        .ThenByDescending(x => x.Person.CreatedAt ?? DateTime.MinValue);

    var items = await orderedQuery
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new AdminCustomerDto
        {
            Id = x.Person.Id,
            Code = x.Person.Code ?? $"KH{x.Person.Id:D6}",
            Name = x.Person.FullName,
            Phone = x.Person.Phone ?? "",
            Email = x.Person.Email ?? "",
            // Null check cho Customer vì là Left Join
            Address = x.Customer != null ? (x.Customer.Address ?? "") : "",
            
            // LẤY TRỰC TIẾP TỪ CỘT MỚI (Không tính toán lại)
            TotalSpend = x.Customer != null ? (x.Customer.TotalSpend ?? 0m) : 0m,
            LastOrderDate = x.Customer != null ? x.Customer.LastOrderDate : null,
            
            Status = (x.Person.IsActive ?? true) ? "Active" : "Inactive"
        })
        .ToListAsync();

    fetchStopwatch.Stop();
    totalStopwatch.Stop();

    Console.WriteLine($"[Performance] Data Fetch took: {fetchStopwatch.ElapsedMilliseconds} ms");
    Console.WriteLine($"[Performance] TOTAL Request took: {totalStopwatch.ElapsedMilliseconds} ms");

    return new PagedResult<AdminCustomerDto>(items, totalCount, page, pageSize);
}
    public async Task CreateCustomerAsync(CreateCustomerDto dto)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create Person
                var person = new PersonEntity
                {
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    PersonType = "CUSTOMER",
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    Code = await GenerateCustomerCode(context)
                };

                context.Persons.Add(person);
                await context.SaveChangesAsync();

                // 2. Create Customer detail
                var customer = new CustomerEntity
                {
                    PersonId = person.Id,
                    Address = dto.Address
                };

                context.Customers.Add(customer);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<AdminCustomerDto?> GetCustomerByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var result = await (
            from person in context.Persons.AsNoTracking()
            where person.Id == id && person.PersonType == "CUSTOMER"
            join customer in context.Customers.AsNoTracking()
                on person.Id equals customer.PersonId into customerGroup
            from customer in customerGroup.DefaultIfEmpty()
            select new AdminCustomerDto
            {
                Id = person.Id,
                Code = person.Code ?? $"KH{person.Id:D6}",
                Name = person.FullName,
                Phone = person.Phone ?? "",
                Email = person.Email ?? "",
                Address = customer != null ? (customer.Address ?? "") : "",
                TotalSpend = customer != null ? (customer.TotalSpend ?? 0m) : 0m,
                LastOrderDate = customer != null ? customer.LastOrderDate : null,
                Status = (person.IsActive ?? true) ? "Active" : "Inactive"
            }
        ).FirstOrDefaultAsync();

        return result;
    }

    public async Task UpdateCustomerAsync(int id, UpdateCustomerDto dto)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // 1. Update Person
                var person = await context.Persons
                    .FirstOrDefaultAsync(p => p.Id == id && p.PersonType == "CUSTOMER");

                if (person == null)
                {
                    throw new InvalidOperationException($"Customer with ID {id} not found.");
                }

                person.FullName = dto.FullName;
                person.Phone = dto.Phone;
                person.Email = dto.Email;

                // 2. Update or Create Customer detail
                var customer = await context.Customers
                    .FirstOrDefaultAsync(c => c.PersonId == id);

                if (customer != null)
                {
                    customer.Address = dto.Address;
                }
                else
                {
                    // Create Customer detail if it doesn't exist
                    customer = new CustomerEntity
                    {
                        PersonId = id,
                        Address = dto.Address
                    };
                    context.Customers.Add(customer);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    private async Task<string> GenerateCustomerCode(AppDbContext context)
    {
        // Simple generation: KH + timestamp or increment
        // For better approach, we could check max code. 
        // Here using a simple random/time based for uniqueness or just KH{Id} after save? 
        // But we need code before save if it's required unique.
        // Let's try KH + Random for now or check latest.
        
        var lastPerson = await context.Persons
            .Where(p => p.PersonType == "CUSTOMER" && p.Code != null && p.Code.StartsWith("KH"))
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        int nextNum = 1;
        if (lastPerson != null && !string.IsNullOrEmpty(lastPerson.Code) && lastPerson.Code.Length > 2)
        {
            if (int.TryParse(lastPerson.Code.Substring(2), out int currentNum))
            {
                nextNum = currentNum + 1;
            }
        }

        return $"KH{nextNum:D6}";
    }
}
