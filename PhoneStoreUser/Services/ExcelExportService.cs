using ClosedXML.Excel;
using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

/// <summary>
/// Service for exporting reports to Excel format using ClosedXML
/// </summary>
public class ExcelExportService : IExcelExportService
{
    /// <summary>
    /// Export purchase order report data to Excel
    /// File naming format: BaoCaoNhap_YYYY-MM-DD_YYYY-MM-DD.xlsx
    /// </summary>
    public byte[] ExportPurchaseOrderReport(List<PurchaseOrderReportItem> data, DateTime startDate, DateTime endDate)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Báo cáo nhập");

        // Add header row
        worksheet.Cell(1, 1).Value = "Mã đơn";
        worksheet.Cell(1, 2).Value = "Ngày đặt";
        worksheet.Cell(1, 3).Value = "Nhà cung cấp";
        worksheet.Cell(1, 4).Value = "Số lượng";
        worksheet.Cell(1, 5).Value = "Tổng giá trị";
        worksheet.Cell(1, 6).Value = "Trạng thái";

        // Style header row
        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Add data rows
        for (int i = 0; i < data.Count; i++)
        {
            var item = data[i];
            int row = i + 2;
            worksheet.Cell(row, 1).Value = item.Id;
            worksheet.Cell(row, 2).Value = item.OrderDate.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 3).Value = item.SupplierName;
            worksheet.Cell(row, 4).Value = item.TotalItems;
            worksheet.Cell(row, 5).Value = item.TotalValue;
            worksheet.Cell(row, 6).Value = item.Status;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Format currency column
        worksheet.Column(5).Style.NumberFormat.Format = "#,##0";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }


    /// <summary>
    /// Export invoice report data to Excel
    /// File naming format: BaoCaoHoaDon_YYYY-MM-DD_YYYY-MM-DD.xlsx
    /// </summary>
    public byte[] ExportInvoiceReport(List<InvoiceReportItem> data, DateTime startDate, DateTime endDate)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Báo cáo hóa đơn");

        // Add header row
        worksheet.Cell(1, 1).Value = "Mã hóa đơn";
        worksheet.Cell(1, 2).Value = "Ngày lập";
        worksheet.Cell(1, 3).Value = "Khách hàng";
        worksheet.Cell(1, 4).Value = "Số lượng";
        worksheet.Cell(1, 5).Value = "Tổng giá trị";
        worksheet.Cell(1, 6).Value = "Trạng thái";

        // Style header row
        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Add data rows
        for (int i = 0; i < data.Count; i++)
        {
            var item = data[i];
            int row = i + 2;
            worksheet.Cell(row, 1).Value = item.Id;
            worksheet.Cell(row, 2).Value = item.InvoiceDate.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 3).Value = item.CustomerName;
            worksheet.Cell(row, 4).Value = item.TotalItems;
            worksheet.Cell(row, 5).Value = item.TotalValue;
            worksheet.Cell(row, 6).Value = item.Status;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Format currency column
        worksheet.Column(5).Style.NumberFormat.Format = "#,##0";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Generate file name for purchase order report
    /// Format: BaoCaoNhap_YYYY-MM-DD_YYYY-MM-DD.xlsx
    /// </summary>
    public string GetPurchaseOrderReportFileName(DateTime startDate, DateTime endDate)
    {
        return $"BaoCaoNhap_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.xlsx";
    }

    /// <summary>
    /// Generate file name for invoice report
    /// Format: BaoCaoHoaDon_YYYY-MM-DD_YYYY-MM-DD.xlsx
    /// </summary>
    public string GetInvoiceReportFileName(DateTime startDate, DateTime endDate)
    {
        return $"BaoCaoHoaDon_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.xlsx";
    }
}
