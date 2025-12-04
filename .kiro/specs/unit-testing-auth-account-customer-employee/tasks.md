# Implementation Plan

- [x] 1. Setup Test Project






  - [x] 1.1 Create xUnit test project `PhoneStoreServices.Tests`

    - Create new xUnit project targeting net8.0
    - Add NuGet packages: xunit, xunit.runner.visualstudio, Moq, FluentAssertions, Microsoft.NET.Test.Sdk
    - Add project references to PhoneStoreServices and PhoneStoreRepository
    - _Requirements: 1.1, 1.2, 1.3_



  - [x] 1.2 Create TestDataFactory helper class
    - Create `TestHelpers/TestDataFactory.cs`
    - Implement static methods: CreateAccount, CreateCustomer, CreateEmployee, CreateEmployeeList, CreateCustomerList
    - _Requirements: 1.4_

- [x] 2. Implement AuthService Tests





  - [x] 2.1 Create AuthServiceTests class


    - Setup mock IAuthRepository
    - Implement tests for LoginAsync (valid/invalid credentials, empty inputs, repository exception)
    - Implement tests for ValidateCredentialsAsync
    - Implement tests for ChangePasswordAsync (valid/invalid current password, empty inputs)
    - Implement tests for VerifyPasswordAsync
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

- [x] 3. Implement AccountService Tests






  - [x] 3.1 Create AccountServiceTests class

    - Setup mock IAccountRepository
    - Implement tests for GetAccountByIdAsync
    - Implement tests for GetAccountByUsernameAsync (valid/empty username)
    - Implement tests for AddAccountAsync
    - Implement tests for UpdateAccountAsync (valid/null account)
    - Implement tests for UpdateAccountRolesAsync (duplicate role normalization)
    - Implement tests for IsAccountActiveAsync
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [x] 4. Implement CustomerService Tests





  - [x] 4.1 Create CustomerServiceTests class


    - Setup mock ICustomerRepository
    - Implement tests for GetCustomerByIdAsync
    - Implement tests for GetCustomerByEmailAsync/PhoneAsync/AddressAsync (valid/empty inputs)
    - Implement tests for AddCustomerAsync (valid/null, PersonType verification)
    - Implement tests for UpdateCustomerAsync (valid/null)
    - Implement tests for DeleteCustomerAsync
    - Implement tests for GetCustomersFilteredAsync
    - Implement tests for IsCustomerActiveAsync
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

- [x] 5. Implement EmployeeService Tests






  - [x] 5.1 Create EmployeeServiceTests class

    - Setup mock IEmployeeRepository
    - Implement tests for GetEmployeeByIdAsync
    - Implement tests for GetEmployeeByEmailAsync/PhoneAsync (valid/empty inputs)
    - Implement tests for AddEmployeeAsync (valid/null, PersonType verification)
    - Implement tests for UpdateEmployeeAsync (valid/null)
    - Implement tests for GetEmployeesFiltered (search term, status filter, date range, pagination)
    - Implement tests for IsEmployeeActiveAsync
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9_

- [x] 6. Verify and Run Tests







  - [ ] 6.1 Build and run all tests
    - Verify all tests pass
    - Fix any compilation or runtime errors
    - _Requirements: 1.1, 2.1-2.7, 3.1-3.6, 4.1-4.7, 5.1-5.9_
