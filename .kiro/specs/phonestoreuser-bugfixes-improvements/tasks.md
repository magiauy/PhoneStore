# Implementation Plan

- [x] 1. Sửa lỗi đăng ký tài khoản





  - [x] 1.1 Implement registration logic in Register.razor.cs


    - Add IDbContextFactory injection
    - Implement HandleValidSubmit to validate fields, check duplicates, hash password, create Person and Account entities
    - Handle success redirect and error messages
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  - [ ]* 1.2 Write property test for password hash consistency
    - **Property 1: Password Hash Consistency**
    - **Validates: Requirements 1.1**
  - [ ]* 1.3 Write property test for duplicate registration prevention
    - **Property 2: Registration Duplicate Prevention**
    - **Validates: Requirements 1.4**


- [x] 2. Sửa lỗi đăng nhập - Single Cookie Authentication




  - [x] 2.1 Simplify authentication to single cookie scheme


    - Modify Program.cs to use single cookie scheme for both user and admin
    - Update login endpoint to include all claims (ID, username, role, permissions) in single cookie
    - Update admin login endpoint to use same cookie scheme with admin role
    - _Requirements: 2.1, 2.4_
  - [x] 2.2 Update CookieAuthStateProvider for single cookie


    - Modify GetAuthenticationStateAsync to work with single cookie
    - Update IsAdminContext to check role from claims instead of cookie scheme
    - _Requirements: 2.2_
  - [x] 2.3 Update logout endpoints


    - Modify both user and admin logout to clear single cookie
    - _Requirements: 2.3_
  - [ ]* 2.4 Write property test for single cookie authentication
    - **Property 3: Single Cookie Authentication**
    - **Validates: Requirements 2.1**
  - [ ]* 2.5 Write property test for permission-based authorization
    - **Property 4: Permission-Based Authorization**
    - **Validates: Requirements 2.2**

- [x] 3. Checkpoint - Ensure all tests pass





  - Ensure all tests pass, ask the user if questions arise.


- [x] 4. Sửa UI tạo hóa đơn - Thêm scroll





  - [x] 4.1 Update CreateOrderModal.razor for scrollable content

    - Add overflow-y-auto to modal body container
    - Keep header and footer fixed
    - Add max-height constraint to product list section
    - _Requirements: 3.1, 3.2, 3.3_

- [ ] 5. Thêm thống kê xuất hóa đơn
  - [ ] 5.1 Create OrderStatisticsDto if not exists
    - Define properties for total invoices, revenue, average value
    - Add daily, weekly, monthly statistics
    - _Requirements: 4.1, 4.3_
  - [ ] 5.2 Implement GetOrderStatisticsAsync in AdminOrderService
    - Calculate total invoices count
    - Calculate total revenue
    - Calculate average order value
    - Support date range filtering
    - _Requirements: 4.1, 4.2_
  - [ ] 5.3 Update Orders.razor to display statistics
    - Add statistics summary section at top of page
    - Display total invoices, revenue, average value
    - _Requirements: 4.1_
  - [ ]* 5.4 Write property test for order statistics accuracy
    - **Property 5: Order Statistics Accuracy**
    - **Validates: Requirements 4.1, 4.2**

- [x] 6. Sửa lỗi tìm kiếm và sửa khách hàng





  - [x] 6.1 Add search parameter to GetCustomersAsync


    - Update IAdminCustomerService interface to include searchTerm parameter
    - Implement search logic in AdminCustomerService to filter by name, phone, email
    - _Requirements: 5.1, 5.4_
  - [x] 6.2 Add GetCustomerByIdAsync and UpdateCustomerAsync methods

    - Add methods to IAdminCustomerService interface
    - Implement in AdminCustomerService
    - _Requirements: 5.2, 5.3_
  - [x] 6.3 Update Customers.razor for search functionality


    - Bind search input to search term
    - Call LoadCustomers with search term on input change
    - _Requirements: 5.1_
  - [x] 6.4 Add edit customer modal/form to Customers.razor


    - Create edit form with customer data binding
    - Implement edit button click handler to open form
    - Implement save handler to call UpdateCustomerAsync
    - _Requirements: 5.2, 5.3_
  - [ ]* 6.5 Write property test for customer search completeness
    - **Property 6: Customer Search Completeness**
    - **Validates: Requirements 5.1**
  - [ ]* 6.6 Write property test for customer update persistence
    - **Property 7: Customer Update Persistence**
    - **Validates: Requirements 5.3**

- [ ] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Loại bỏ action gửi mail khách hàng
  - [ ] 8.1 Remove email action button from Customers.razor
    - Remove the envelope icon button from customer row actions
    - _Requirements: 6.1_
  - [ ] 8.2 Remove SendWelcomeEmail from CreateCustomerDto and form
    - Remove SendWelcomeEmail property from CreateCustomerDto
    - Remove checkbox from customer creation form in Customers.razor
    - _Requirements: 6.2_

- [x] 9. Sửa lỗi thêm thuộc tính sản phẩm





  - [x] 9.1 Fix attribute saving for new products in ProductEditor.razor


    - Modify HandleSave to properly save attributes for new products
    - Ensure CreateProductAsync returns the new product ID
    - Call SaveProductAttributeValuesAsync with correct product ID after creation
    - _Requirements: 7.1, 7.2, 7.3_
  - [x] 9.2 Update IProductService.CreateProductAsync to return product ID


    - Change return type from bool to int (product ID)
    - Update implementation in ProductService
    - _Requirements: 7.3_
  - [ ]* 9.3 Write property test for product attribute persistence
    - **Property 8: Product Attribute Persistence**
    - **Validates: Requirements 7.2, 7.3**

- [x] 10. Validate Serial khi xử lý hóa đơn






  - [x] 10.1 Add serial validation to Order Processing page

    - Implement ValidateSerial method similar to CreateOrderModal logic
    - Check serial exists in database
    - Check serial belongs to correct product (ProductId matches)
    - Check serial status is 'in_stock'
    - Display specific error messages for each validation failure
    - _Requirements: 8.1, 8.2, 8.3, 8.4_
  - [ ]* 10.2 Write property test for serial validation completeness
    - **Property 9: Serial Validation for Order Processing**
    - **Validates: Requirements 8.1, 8.2, 8.3**

- [ ] 11. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Giới hạn tồn kho khi thêm vào giỏ hàng






  - [x] 12.1 Add inventory check methods to CartService

    - Implement AddToCartWithInventoryCheck method
    - Implement UpdateQuantityWithInventoryCheck method
    - Inject IInventoryService to get availability
    - _Requirements: 9.1, 9.2, 9.3_
  - [x] 12.2 Add checkout validation method to CartService

    - Implement ValidateCartAtCheckout method
    - Return list of validation results for each cart item
    - _Requirements: 9.4_

  - [x] 12.3 Update ProductDetail.razor to use inventory-checked add to cart

    - Replace AddToCart call with AddToCartWithInventoryCheck
    - Display warning message if quantity limited
    - _Requirements: 9.1, 9.2_

  - [x] 12.4 Update Cart.razor to use inventory-checked quantity update

    - Replace UpdateQuantity call with UpdateQuantityWithInventoryCheck
    - Display warning message if quantity limited
    - _Requirements: 9.3_


  - [x] 12.5 Update Checkout.razor to validate inventory before processing
    - Call ValidateCartAtCheckout before processing payment
    - Display validation errors for unavailable items
    - Block checkout if validation fails
    - _Requirements: 9.4_
  - [ ]* 12.6 Write property test for cart inventory limit
    - **Property 10: Cart Inventory Limit**
    - **Validates: Requirements 9.1, 9.2, 9.3**
  - [ ]* 12.7 Write property test for checkout inventory validation
    - **Property 11: Checkout Inventory Validation**
    - **Validates: Requirements 9.4**

- [ ] 13. Final Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
