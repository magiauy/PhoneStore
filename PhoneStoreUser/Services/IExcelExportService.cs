using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

/// <summary>
/// Service interface for exporting reports to Excel format
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Export purchase order report data to Excel
    /// </summary>
    /// <returns>Excel file as byte array</returns>
    byte[] ExportPurchaseOrderReport(List<PurchaseOrderReportItem> data, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Export invoice report data to Excel
    /// </summary>
    /// <returns>Excel file as byte array</returns>
    byte[] ExportInvoiceReport(List<InvoiceReportItem> data, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Generate file name for purchase order report
    /// </summary>
    string GetPurchaseOrderReportFileName(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Generate file name for invoice report
    /// </summary>
    string GetInvoiceReportFileName(DateTime startDate, DateTime endDate);
}
