using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

/// <summary>
/// Service interface for generating business reports
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Get purchase order report data for the specified date range
    /// </summary>
    Task<PurchaseOrderReportData> GetPurchaseOrderReportAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get invoice report data for the specified date range
    /// </summary>
    Task<InvoiceReportData> GetInvoiceReportAsync(DateTime startDate, DateTime endDate);
}
