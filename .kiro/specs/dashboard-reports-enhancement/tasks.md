# Implementation Plan

- [x] 1. Set up DTOs and Service Interfaces





  - [x] 1.1 Create Dashboard DTOs (MonthlyStatistics, TopProductDto, TopCustomerDto)


    - Create file `PhoneStoreUser/Components/ViewModels/DashboardViewModels.cs`
    - Define MonthlyStatistics, TopProductDto, TopCustomerDto classes
    - _Requirements: 1.1, 1.3, 2.2, 3.2_
  - [x] 1.2 Create Report DTOs (PurchaseOrderReportData, InvoiceReportData, ChartDataPoint)


    - Create file `PhoneStoreUser/Components/ViewModels/ReportViewModels.cs`
    - Define all report-related DTOs
    - _Requirements: 5.1, 5.4, 6.1, 6.4_
  - [x] 1.3 Create IDashboardService interface


    - Create file `PhoneStoreUser/Services/IDashboardService.cs`
    - Define GetMonthlyStatisticsAsync, GetTopSellingProductsAsync, GetTopCustomersAsync methods
    - _Requirements: 1.1, 2.1, 3.1_
  - [x] 1.4 Create IReportService interface


    - Create file `PhoneStoreUser/Services/IReportService.cs`
    - Define GetPurchaseOrderReportAsync, GetInvoiceReportAsync methods
    - _Requirements: 5.1, 6.1_
  - [x] 1.5 Create IExcelExportService interface


    - Create file `PhoneStoreUser/Services/IExcelExportService.cs`
    - Define ExportPurchaseOrderReport, ExportInvoiceReport methods
    - _Requirements: 7.1, 7.2_

- [-] 2. Implement Dashboard Service



  - [x] 2.1 Implement DashboardService class



    - Create file `PhoneStoreUser/Services/DashboardService.cs`
    - Implement GetMonthlyStatisticsAsync with month filtering
    - Implement GetTopSellingProductsAsync with sorting by quantity
    - Implement GetTopCustomersAsync with sorting by purchase value
    - _Requirements: 1.1, 1.2, 2.1, 3.1_
  - [ ]* 2.2 Write property test for month-based data filtering
    - **Property 1: Month-based data filtering consistency**
    - **Validates: Requirements 1.2, 2.3, 3.3**
  - [ ]* 2.3 Write property test for top products sorting
    - **Property 2: Top products sorting correctness**
    - **Validates: Requirements 2.1**
  - [ ]* 2.4 Write property test for top customers sorting
    - **Property 3: Top customers sorting correctness**
    - **Validates: Requirements 3.1**


- [x] 3. Implement Report Service





  - [x] 3.1 Implement ReportService class

    - Create file `PhoneStoreUser/Services/ReportService.cs`
    - Implement GetPurchaseOrderReportAsync with date range filtering
    - Implement GetInvoiceReportAsync with date range filtering
    - Generate chart data points grouped by day/week/month
    - _Requirements: 5.1, 5.3, 6.1, 6.3_
  - [ ]* 3.2 Write property test for date range filtering
    - **Property 4: Date range filtering for reports**
    - **Validates: Requirements 5.3, 6.3**


- [x] 4. Implement Excel Export Service




  - [x] 4.1 Add ClosedXML NuGet package


    - Add ClosedXML package to PhoneStoreUser.csproj
    - _Requirements: 7.1, 7.2_
  - [x] 4.2 Implement ExcelExportService class


    - Create file `PhoneStoreUser/Services/ExcelExportService.cs`
    - Implement ExportPurchaseOrderReport method
    - Implement ExportInvoiceReport method
    - Include proper file naming with report type and date range
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  - [ ]* 4.3 Write property test for Excel export completeness
    - **Property 5: Excel export data completeness**
    - **Validates: Requirements 7.3**


- [x] 5. Register Services in DI Container




  - [x] 5.1 Register all new services in Program.cs


    - Register IDashboardService, IReportService, IExcelExportService
    - _Requirements: 1.1, 5.1, 7.1_


- [x] 6. Checkpoint - Ensure all service tests pass




  - Ensure all tests pass, ask the user if questions arise.


- [x] 7. Update Dashboard Component


  - [x] 7.1 Add month picker to Dashboard


    - Add month/year selector UI component
    - Bind to selected month state
    - _Requirements: 1.2, 1.4_

  - [x] 7.2 Update Dashboard statistics section
    - Inject IDashboardService
    - Load monthly statistics on month change
    - Display total revenue, orders, new customers
    - _Requirements: 1.1, 1.3_
  - [x] 7.3 Add Top 10 Products section

    - Create table/list component for top products
    - Display product name, quantity sold, total revenue
    - Update on month change
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 7.4 Add Top 10 Customers section

    - Create table/list component for top customers
    - Display customer name, order count, total purchase value
    - Update on month change
    - _Requirements: 3.1, 3.2, 3.3_

- [x] 8. Create Reports Page





  - [x] 8.1 Create Reports.razor component


    - Create file `PhoneStoreUser/Components/Pages/Admin/Reports.razor`
    - Add @page "/admin/reports" directive
    - Add @layout AdminLayout
    - Create tab navigation UI (Báo cáo nhập / Báo cáo hóa đơn)
    - _Requirements: 4.2, 4.3_

  - [x] 8.2 Implement Purchase Order Report tab

    - Add date range picker
    - Display summary cards (total count, total value)
    - Add data table with columns: order date, supplier, items, value, status
    - Add "Xuất Excel" button
    - _Requirements: 5.1, 5.3, 5.4, 7.1_

  - [x] 8.3 Implement Invoice Report tab

    - Add date range picker
    - Display summary cards (total count, total revenue)
    - Add data table with columns: invoice date, customer, items, value, status
    - Add "Xuất Excel" button
    - _Requirements: 6.1, 6.3, 6.4, 7.2_

  - [x] 8.4 Implement Excel download functionality

    - Handle button click to call ExcelExportService
    - Trigger file download in browser
    - _Requirements: 7.1, 7.2, 7.4_

- [x] 9. Add Charts to Reports Page





  - [x] 9.1 Add chart library reference


    - Add Blazor chart library (e.g., Radzen.Blazor or ChartJs.Blazor)
    - _Requirements: 5.2, 6.2_

  - [x] 9.2 Implement chart for Purchase Order report

    - Create bar/line chart showing purchase orders over time
    - _Requirements: 5.2_

  - [x] 9.3 Implement chart for Invoice report

    - Create bar/line chart showing invoices over time
    - _Requirements: 6.2_


- [x] 10. Update AdminLayout




  - [x] 10.1 Add "Báo cáo" menu item to sidebar


    - Add NavLink to /admin/reports with chart icon
    - Update CurrentTitle switch for reports route
    - _Requirements: 4.1, 4.2_

  - [x] 10.2 Add Logout button to header

    - Add logout button with icon in header section
    - _Requirements: 8.1_

  - [x] 10.3 Implement logout functionality

    - Handle logout button click
    - Clear admin session data
    - Redirect to login page
    - _Requirements: 8.2, 8.3_

- [ ] 11. Final Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
