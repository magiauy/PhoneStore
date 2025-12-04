# Requirements Document

## Introduction

Triển khai Unit Testing cho các service layers của PhoneStoreAdmin bao gồm: AuthService, AccountService, CustomerService và EmployeeService. Sử dụng xUnit framework với Moq để mock dependencies (repositories). Tests sẽ verify business logic, input validation, error handling và data transformation.

## Glossary

- **AuthService**: Service xử lý authentication (login, logout, change password)
- **AccountService**: Service quản lý tài khoản người dùng (CRUD, roles, permissions)
- **CustomerService**: Service quản lý khách hàng (CRUD, search, filter)
- **EmployeeService**: Service quản lý nhân viên (CRUD, search, filter)
- **Repository**: Data access layer được mock trong tests
- **xUnit**: Testing framework cho .NET
- **Moq**: Mocking library để tạo mock objects

## Requirements

### Requirement 1: Test Project Setup

**User Story:** As a developer, I want a properly configured test project, so that I can write and run unit tests for the services.

#### Acceptance Criteria

1. THE Test_Project SHALL be created as a separate xUnit project named `PhoneStoreServices.Tests`
2. THE Test_Project SHALL reference `PhoneStoreServices` and `PhoneStoreRepository` projects
3. THE Test_Project SHALL include xUnit, Moq, and FluentAssertions NuGet packages
4. THE Test_Project SHALL follow the naming convention `{ServiceName}Tests.cs` for test files

### Requirement 2: AuthService Unit Tests

**User Story:** As a developer, I want unit tests for AuthService, so that I can verify authentication logic works correctly.

#### Acceptance Criteria

1. WHEN valid credentials are provided, THE AuthService SHALL return the authenticated Account
2. WHEN invalid credentials are provided, THE AuthService SHALL return null
3. WHEN empty username or password is provided, THE AuthService SHALL return null without calling repository
4. WHEN login is successful, THE AuthService SHALL update last login time
5. WHEN changing password, THE AuthService SHALL verify current password before updating
6. WHEN current password is incorrect during password change, THE AuthService SHALL return null
7. WHEN repository throws exception, THE AuthService SHALL catch exception and return null/false

### Requirement 3: AccountService Unit Tests

**User Story:** As a developer, I want unit tests for AccountService, so that I can verify account management logic works correctly.

#### Acceptance Criteria

1. WHEN getting account by ID, THE AccountService SHALL return Account from repository
2. WHEN getting account by empty username, THE AccountService SHALL return null without calling repository
3. WHEN adding new account, THE AccountService SHALL return created account ID
4. WHEN updating account with null, THE AccountService SHALL throw ArgumentNullException
5. WHEN updating account roles, THE AccountService SHALL normalize role IDs (remove duplicates)
6. WHEN repository throws exception, THE AccountService SHALL catch exception and return null/false/0

### Requirement 4: CustomerService Unit Tests

**User Story:** As a developer, I want unit tests for CustomerService, so that I can verify customer management logic works correctly.

#### Acceptance Criteria

1. WHEN getting customer by ID, THE CustomerService SHALL return Customer from repository
2. WHEN getting customer by empty email/phone/address, THE CustomerService SHALL return null without calling repository
3. WHEN adding customer, THE CustomerService SHALL set PersonType to CUSTOMER
4. WHEN adding null customer, THE CustomerService SHALL return null
5. WHEN updating null customer, THE CustomerService SHALL return false
6. WHEN filtering customers, THE CustomerService SHALL apply search and filter criteria correctly
7. WHEN repository throws exception, THE CustomerService SHALL catch exception and return null/false/empty list

### Requirement 5: EmployeeService Unit Tests

**User Story:** As a developer, I want unit tests for EmployeeService, so that I can verify employee management logic works correctly.

#### Acceptance Criteria

1. WHEN getting employee by ID, THE EmployeeService SHALL return Employee from repository
2. WHEN getting employee by empty email/phone, THE EmployeeService SHALL return null without calling repository
3. WHEN adding employee, THE EmployeeService SHALL set PersonType to EMPLOYEE
4. WHEN adding null employee, THE EmployeeService SHALL return null
5. WHEN updating null employee, THE EmployeeService SHALL return false
6. WHEN filtering employees with search term, THE EmployeeService SHALL filter by FullName, Email, Phone, and Code
7. WHEN filtering employees with status filter, THE EmployeeService SHALL filter by IsActive property
8. WHEN filtering employees with date range, THE EmployeeService SHALL filter by HireDate
9. WHEN repository throws exception, THE EmployeeService SHALL catch exception and return null/false/empty list
