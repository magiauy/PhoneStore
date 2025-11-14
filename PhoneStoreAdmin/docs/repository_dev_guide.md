# 📘 Repository Development Guide  
**Dành cho dự án: PhoneStoreAdmin**

## 🧩 1. Mục đích
Repository chịu trách nhiệm **truy cập và thao tác dữ liệu với MySQL**.  
Nó **ẩn toàn bộ logic SQL, transaction, và connection management**, giúp tầng `Service` chỉ tập trung xử lý nghiệp vụ.

---

## 🧱 2. Cấu trúc thư mục
```
PhoneStoreRepository/
├── Data/
│   └── DataSource.cs
├── Models/
│   ├── Account.cs
│   ├── Role.cs
│   ├── Permission.cs
│   └── Person.cs
├── Repositories/
│   ├── Interfaces/
│   │   └── IAccountRepository.cs
│   └── Implementations/
│       └── AccountRepository.cs
└── Utils/
    ├── Logger.cs
    └── PasswordHasher.cs
```

**Quy ước:**
- Interface đặt trong `Repositories/Interfaces` với tên `I<Entity>Repository`.
- Implementation đặt trong `Repositories/Implementations` với tên `<Entity>Repository`.
- Tất cả đều inject `DataSource` (hoặc `ConnectionFactory`) qua constructor.

---

## ⚙️ 3. Cấu trúc chuẩn của Repository

```csharp
public class <Entity>Repository(DataSource dataSource) : I<Entity>Repository
{
    private readonly DataSource _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

    #region Sync (Optional Legacy)
    public <Entity> GetById(int id) => GetByIdAsync(id).GetAwaiter().GetResult();
    public void Insert(<Entity> entity) => AddAsync(entity).GetAwaiter().GetResult();
    #endregion

    #region Async CRUD
    public async Task<<Entity>?> GetByIdAsync(int id) { /* SELECT ... WHERE id = @id */ }
    public async Task<List<<Entity>>?> GetAllAsync() { /* SELECT * FROM ... */ }
    public async Task<<Entity>?> AddAsync(<Entity> entity) { /* INSERT ... RETURN ID */ }
    public async Task UpdateAsync(<Entity> entity) { /* UPDATE ... */ }
    public async Task DeleteAsync(int id) { /* Soft delete (is_active = 0) */ }
    #endregion

    #region Extra Query (Optional)
    // Ví dụ: GetByNameAsync(), GetWithRelationsAsync(), GetPagedAsync(), v.v.
    #endregion
}
```

---

## 🧩 4. Quy ước đặt tên & nguyên tắc

| Thành phần | Quy ước |
|-------------|----------|
| Interface | `I<Entity>Repository` |
| Implementation | `<Entity>Repository` |
| Biến kết nối | `_dataSource` |
| Transaction | Sử dụng `await conn.BeginTransactionAsync()` khi có thao tác nhiều bảng |
| Logging | Dùng `Logger.Info()`, `Logger.Error()` trong mọi thao tác I/O |
| Exception | Không throw lỗi DB lên tầng trên, chỉ throw khi logic nghiệp vụ cần |
| Soft delete | Không xoá vật lý (`DELETE`), mà `UPDATE ... SET is_active = 0` |

---

## 🧠 5. Nguyên tắc triển khai

### 🔹 5.1. Kết nối Database
Mọi repository phải sử dụng `DataSource`:
```csharp
await using var conn = _dataSource.GetConnection();
await using var cmd = conn.CreateCommand();
```

### 🔹 5.2. Tránh leak connection
- Dùng `await using` để auto-dispose connection, command, reader.
- Không giữ kết nối lâu hoặc mở xuyên hàm.

### 🔹 5.3. Transaction logic
Khi có liên quan nhiều bảng (vd: `Persons`, `Accounts`), luôn bọc trong `MySqlTransaction`:
```csharp
var conn = _dataSource.GetConnection();
var transaction = await conn.BeginTransactionAsync();
try {
    // Thực hiện nhiều command
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
    throw;
}
```

---

## 🧰 6. Quy ước CRUD chi tiết

| Hàm | Mục đích | Ghi chú |
|------|-----------|---------|
| `GetByIdAsync(int id)` | Lấy entity theo ID | Dùng `LIMIT 1;`, return `null` nếu không có |
| `GetAllAsync()` | Lấy toàn bộ entity | Có thể join bảng liên quan |
| `AddAsync(T entity)` | Thêm mới | Nếu có quan hệ (Person/Account) → insert parent trước |
| `UpdateAsync(T entity)` | Cập nhật | Cập nhật cột cần thiết |
| `DeleteAsync(int id)` | Xóa mềm | `UPDATE ... SET is_active = 0` |

---

## 🔍 7. Quy tắc đọc dữ liệu (`DataReader`)
Luôn dùng `reader.GetOrdinal()` để tối ưu performance và tránh lỗi index:
```csharp
Id = reader.GetInt32(reader.GetOrdinal("id")),
Username = reader.GetString(reader.GetOrdinal("username")),
```

Để tránh lỗi “0000-00-00 00:00:00”, dùng helper:
```csharp
DateTime? GetSafeDateTime(int ordinal)
{
    if (reader.IsDBNull(ordinal)) return null;
    try { return reader.GetDateTime(ordinal); }
    catch { return null; }
}
```

---

## 🧾 8. Logging chuẩn
- `Logger.Info(...)`: khi thao tác thành công (INSERT/UPDATE/DELETE).
- `Logger.Warning(...)`: khi dữ liệu trùng lặp hoặc rollback.
- `Logger.Error(...)`: khi có exception, ghi kèm message + stack trace.

---

## 📦 9. Mẫu mở rộng Repository mới

### **1️⃣ Tạo Interface**
```csharp
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<List<Customer>?> GetAllAsync();
    Task<Customer?> AddAsync(Customer entity);
    Task UpdateAsync(Customer entity);
    Task DeleteAsync(int id);
}
```

### **2️⃣ Tạo Implementation**
```csharp
public class CustomerRepository(DataSource dataSource) : ICustomerRepository
{
    private readonly DataSource _dataSource = dataSource;

    public async Task<Customer?> GetByIdAsync(int id)
    {
        await using var conn = _dataSource.GetConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, email FROM Customers WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Customer
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Email = reader.IsDBNull("email") ? null : reader.GetString("email")
            };
        }
        return null;
    }
}
```

---

## 📈 10. Khi nào cần `GetPagedAsync`
Nếu bảng lớn (Accounts, Invoices, Products...), hãy viết hàm `GetPagedAsync(int pageIndex, int pageSize, ...)`:
- Kết hợp `LIMIT @limit OFFSET @offset`.
- Tách riêng count tổng (`COUNT(*)`) và danh sách.
- Cho phép filter linh hoạt qua `Dictionary<string, object>`.

---

## 🧩 11. Checklist khi tạo Repository mới
✅ Inject `DataSource`  
✅ Dùng async/await, tránh block thread  
✅ Có log cho mọi thao tác chính  
✅ Soft delete thay vì hard delete  
✅ Sử dụng parameterized query (`@param`) để chống SQL Injection  
✅ Xử lý `DBNull` khi đọc dữ liệu  
✅ Có unit test cơ bản nếu cần  

---

