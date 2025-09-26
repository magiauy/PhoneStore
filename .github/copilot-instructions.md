# PhoneStore Project - AI Coding Instructions

## Architecture Overview

This is a **dual-application phone store management system** with:
- **PhoneStoreAdmin**: WinUI3 desktop admin app using .NET 8.0-windows
- **PhoneStoreUserWebApp**: Blazor Server/WebAssembly hybrid customer-facing web app

Both applications share a common MySQL database (`bandienthoai`) and follow similar architectural patterns.

## Key Architectural Patterns

### Repository Pattern with Custom DI Container
- **DO NOT** use built-in .NET DI - this project uses a custom `ServiceContainer` in `Services/ServiceContainer.cs`
- All repositories implement `IRepository<T>` base interface with standard CRUD operations
- Services are registered using `ServiceContainer.Initialize()` and retrieved via `ServiceContainer.GetService<T>()`
- MySQL connectivity through `DataSource` class with manual connection management

### Model Structure with Backing Fields
**Critical Pattern**: All models use private backing fields with public properties:
```csharp
private string _name = string.Empty;
public string Name 
{
    get => _name;
    set => _name = value ?? string.Empty;
}
```
- Always follow this pattern when creating/modifying models
- Use `[Table("table_name")]` attributes for database mapping
- Navigation properties are collections: `ICollection<T>`

### WinUI3 Admin App Conventions
- **Navigation**: Uses `NavigationView` with page-based routing in `MainWindow.xaml`
- **Theming**: Custom color scheme in `Theme/Colors.xaml` with `BrushPrimary`, `BrushBackground`, etc.
- **No ViewModels**: Pages use code-behind pattern, not MVVM
- **Asset Management**: Logo assets with multiple scale factors (100%, 125%, 150%, 200%, 400%)

### Configuration Management
```json
// appsettings.json structure
{
  "Database": {
    "Host": "localhost",
    "Port": 3306,
    "User": "root", 
    "Password": "",
    "Database": "bandienthoai"
  }
}
```

## Development Workflows

### Building Projects
```powershell
# Admin app (WinUI3) - requires specific platform
dotnet build PhoneStoreAdmin/PhoneStoreAdmin.csproj -r win-x64

# Web app (standard)
dotnet build PhoneStoreUserWebApp/PhoneStoreUserWebApp/PhoneStoreUserWebApp.csproj
```

### Database Integration
- **Connection**: Manual MySQL connections via `DataSource.GetConnection()`
- **ORM**: Entity Framework Core with Pomelo MySQL provider
- **Migrations**: No automatic migrations - database schema managed externally

### Authentication & Session Management
- Custom authentication service implementing `IAuthService`
- Session management through `ISessionService` and `UserSession` model
- Role-based permissions via `AccountRole`, `Role`, `Permission` entities

## Project-Specific Conventions

### Service Layer Pattern
```csharp
// Services are interfaces with async patterns
public interface IAuthService
{
    Task<Account?> LoginAsync(string username, string password);
    Task<bool> ValidateCredentialsAsync(string username, string password);
}
```

### Error Handling & Logging
- Custom `Logger` utility in `Utils/Logger.cs`
- No global exception handling - handle at service/repository level

### File Organization
- **Models**: Domain entities with enums in `Models/Enums/`
- **Repositories**: Interface/Implementation split pattern
- **Services**: Interface/Implementation split pattern  
- **Views**: WinUI3 pages in `View/` with code-behind
- **Assets**: Multi-scale image assets for Windows packaging

## Common Gotchas

1. **Platform Targeting**: Admin app requires Windows-specific targeting (`net8.0-windows10.0.19041.0`)
2. **MySQL Connection**: Always dispose connections manually - no connection pooling configured
3. **Asset References**: Use proper scale-aware asset naming for WinUI3 deployment
4. **Service Registration**: Services must be explicitly registered in `ServiceContainer.Initialize()`
5. **Navigation**: Admin app navigation requires updating both XAML menu items and code-behind routing

## Key Domain Entities

**Core Business Objects**: `Product`, `Customer`, `Invoice`, `PurchaseOrder`, `Supplier`
**Inventory**: `ProductSerial`, `Batches`, `BatchProduct` for serialized tracking  
**Auth**: `Account`, `Person`, `Role`, `Permission` hierarchy
**Commerce**: `Promotion`, `PromotionCode`, `Payment` for sales flow

When working with products, note the serial tracking capability (`IsSerialTracked` property) affects inventory management workflows.