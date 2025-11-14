using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OfficeOpenXml;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PhoneStoreAdmin.View.Controls
{
    public sealed partial class ImportPurchaseOrderDialog : ContentDialog
    {
        private IProductRepository ProductRepository => App.GetService<IProductRepository>();
        private IProductSerialRepository ProductSerialRepository => App.GetService<IProductSerialRepository>();

        public ObservableCollection<ImportRowData> ImportedData { get; } = new();
        public List<(Product product, int quantity, List<SerialEntry> serials)> ValidatedProducts { get; private set; } = new();

        public ImportPurchaseOrderDialog()
        {
            // EPPlus license is set globally in App.xaml.cs
            this.InitializeComponent();
        }

        private async void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                var window = App.GetService<MainWindow>();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".xlsx");
                picker.FileTypeFilter.Add(".xls");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    FilePathTextBox.Text = file.Path;
                    await LoadExcelFile(file);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error browsing file: {ex.Message}");
            }
        }

        private async Task LoadExcelFile(StorageFile file)
        {
            try
            {
                ImportedData.Clear();

                using var stream = await file.OpenStreamForReadAsync();
                using var package = new ExcelPackage(stream);

                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    ShowError("No worksheet found in Excel file");
                    return;
                }

                // Expected columns: Product ID | Serial Number | IMEI1 | IMEI2
                int rowNumber = 2; // Start from row 2 (row 1 is header)

                while (rowNumber <= worksheet.Dimension?.End.Row)
                {
                    var productIdText = worksheet.Cells[rowNumber, 1].Value?.ToString()?.Trim();
                    var serialNumber = worksheet.Cells[rowNumber, 2].Value?.ToString()?.Trim();
                    var imei1 = worksheet.Cells[rowNumber, 3].Value?.ToString()?.Trim();
                    var imei2 = worksheet.Cells[rowNumber, 4].Value?.ToString()?.Trim();

                    // Skip empty rows
                    if (string.IsNullOrWhiteSpace(productIdText) && string.IsNullOrWhiteSpace(serialNumber))
                    {
                        rowNumber++;
                        continue;
                    }

                    var rowData = new ImportRowData
                    {
                        RowNumber = rowNumber,
                        Sku = productIdText ?? "", // Reuse Sku field for Product ID
                        Quantity = "1", // Always 1 per row
                        SerialNumber = serialNumber ?? "",
                        Imei1 = imei1 ?? "",
                        Imei2 = imei2
                    };

                    // Validate row
                    ValidateRow(rowData);

                    ImportedData.Add(rowData);
                    rowNumber++;
                }

                // Update UI
                EmptyStatePanel.Visibility = ImportedData.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                PreviewListView.ItemsSource = ImportedData;

                UpdateStatistics();
                ValidateAllData();
            }
            catch (Exception ex)
            {
                ShowError($"Error loading Excel file: {ex.Message}");
            }
        }

        private void ValidateRow(ImportRowData row)
        {
            var errors = new List<string>();

            // Validate Product ID
            if (string.IsNullOrWhiteSpace(row.Sku))
            {
                errors.Add("Product ID is required");
            }
            else
            {
                // Try parse as integer
                if (!int.TryParse(row.Sku, out int productId))
                {
                    errors.Add($"Product ID '{row.Sku}' must be a valid number");
                }
                else
                {
                    var product = ProductRepository.GetAll().FirstOrDefault(p => p.Id == productId);
                    if (product == null)
                    {
                        errors.Add($"Product with ID '{row.Sku}' not found");
                    }
                    else
                    {
                        row.ProductId = product.Id;
                        row.ProductName = product.Name;
                        row.IsSerialTracked = product.IsSerialTracked;
                    }
                }
            }

            // Quantity is always 1
            row.QuantityValue = 1;

            // Validate Serial/IMEI for serial-tracked products
            if (row.IsSerialTracked)
            {
                if (string.IsNullOrWhiteSpace(row.SerialNumber))
                {
                    errors.Add("Serial Number is required for this product");
                }
                else
                {
                    // Check duplicate serial in database
                    var existingSerial = ProductSerialRepository.TryGetBySerialNumber(row.SerialNumber);
                    if (existingSerial != null)
                    {
                        errors.Add($"Serial '{row.SerialNumber}' already exists in system");
                    }
                }

                if (string.IsNullOrWhiteSpace(row.Imei1))
                {
                    errors.Add("IMEI1 is required for this product");
                }
                else
                {
                    // Check duplicate IMEI1 in database
                    var existingImei = ProductSerialRepository.TryGetByImei1(row.Imei1);
                    if (existingImei != null)
                    {
                        errors.Add($"IMEI1 '{row.Imei1}' already exists in system");
                    }
                }

                // Check IMEI2 if provided
                if (!string.IsNullOrWhiteSpace(row.Imei2))
                {
                    var existingImei2 = ProductSerialRepository.TryGetByImei2(row.Imei2);
                    if (existingImei2 != null)
                    {
                        errors.Add($"IMEI2 '{row.Imei2}' already exists in system");
                    }
                }
            }

            row.ErrorMessage = errors.Any() ? string.Join("; ", errors) : "";
            row.IsValid = !errors.Any();
        }

        private void ValidateAllData()
        {
            // Check for duplicate serials within the import
            var serialGroups = ImportedData
              .Where(r => !string.IsNullOrWhiteSpace(r.SerialNumber))
                        .GroupBy(r => r.SerialNumber?.ToLower())
              .Where(g => g.Count() > 1);

            foreach (var group in serialGroups)
            {
                foreach (var row in group)
                {
                    if (string.IsNullOrEmpty(row.ErrorMessage))
                        row.ErrorMessage = $"Duplicate serial '{group.Key}' in import file";
                    else
                        row.ErrorMessage += $"; Duplicate serial '{group.Key}' in import file";
                    row.IsValid = false;
                }
            }

            // Check for duplicate IMEI1 within the import
            var imei1Groups = ImportedData
         .Where(r => !string.IsNullOrWhiteSpace(r.Imei1))
            .GroupBy(r => r.Imei1?.ToLower())
    .Where(g => g.Count() > 1);

            foreach (var group in imei1Groups)
            {
                foreach (var row in group)
                {
                    if (string.IsNullOrEmpty(row.ErrorMessage))
                        row.ErrorMessage = $"Duplicate IMEI1 '{group.Key}' in import file";
                    else
                        row.ErrorMessage += $"; Duplicate IMEI1 '{group.Key}' in import file";
                    row.IsValid = false;
                }
            }

            PreviewListView.ItemsSource = null;
            PreviewListView.ItemsSource = ImportedData;
        }

        private void UpdateStatistics()
        {
            int total = ImportedData.Count;
            int valid = ImportedData.Count(r => r.IsValid);
            int errors = total - valid;

            TotalRowsText.Text = $"Total: {total} rows";
            ValidRowsText.Text = $"Valid: {valid}";
            ErrorRowsText.Text = $"Errors: {errors}";

            if (errors > 0)
            {
                ValidationInfoBar.IsOpen = true;
                ValidationInfoBar.Message = $"Found {errors} error(s). Please fix before importing.";
                IsPrimaryButtonEnabled = false;
            }
            else
            {
                ValidationInfoBar.IsOpen = false;
                IsPrimaryButtonEnabled = total > 0;
            }
        }

        private async void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var savePicker = new FileSavePicker();
                var window = App.GetService<MainWindow>();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Excel File", new List<string> { ".xlsx" });
                savePicker.SuggestedFileName = "PurchaseOrder_Import_Template";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("PurchaseOrder");

                    // Header
                    worksheet.Cells[1, 1].Value = "Product ID";
                    worksheet.Cells[1, 2].Value = "Serial Number";
                    worksheet.Cells[1, 3].Value = "IMEI1";
                    worksheet.Cells[1, 4].Value = "IMEI2";

                    // Style header
                    using (var range = worksheet.Cells[1, 1, 1, 4])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // Example rows
                    worksheet.Cells[2, 1].Value = 1; // Product ID
                    worksheet.Cells[2, 2].Value = "SNREMOVED_SECRET789";
                    worksheet.Cells[2, 3].Value = "REMOVED_SECRET789012345";
                    worksheet.Cells[2, 4].Value = "REMOVED_SECRET789012346";

                    worksheet.Cells[3, 1].Value = 1; // Same product, different serial
                    worksheet.Cells[3, 2].Value = "SN987654321";
                    worksheet.Cells[3, 3].Value = "987654321012345";
                    worksheet.Cells[3, 4].Value = "";

                    worksheet.Cells[4, 1].Value = 2; // Different product
                    worksheet.Cells[4, 2].Value = "SN555666777";
                    worksheet.Cells[4, 3].Value = "555666777888999";
                    worksheet.Cells[4, 4].Value = "";

                    worksheet.Cells.AutoFitColumns();

                    using var stream = await file.OpenStreamForWriteAsync();
                    stream.SetLength(0);
                    package.SaveAs(stream);

                    ShowSuccess("Template downloaded successfully!");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error downloading template: {ex.Message}");
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                // Group by Product ID and aggregate
                ValidatedProducts = ImportedData
    .Where(r => r.IsValid)
        .GroupBy(r => r.ProductId) // Group by Product ID instead of SKU
      .Select(g =>
     {
        var firstRow = g.First();
      var product = ProductRepository.GetAll().First(p => p.Id == firstRow.ProductId);
      
       var serials = g.Select(r => new SerialEntry
      {
         SerialNumber = r.SerialNumber,
    Imei1 = r.Imei1,
    Imei2 = r.Imei2
       }).ToList();

           return (product, quantity: g.Count(), serials);
  })
        .ToList();
}
  catch (Exception ex)
 {
           args.Cancel = true;
       ShowError($"Error processing import: {ex.Message}");
     }
     }

        private void ShowError(string message)
        {
            ValidationInfoBar.Severity = InfoBarSeverity.Error;
            ValidationInfoBar.Message = message;
            ValidationInfoBar.IsOpen = true;
        }

        private void ShowSuccess(string message)
        {
            ValidationInfoBar.Severity = InfoBarSeverity.Success;
            ValidationInfoBar.Message = message;
            ValidationInfoBar.IsOpen = true;
        }
    }

    public class ImportRowData
    {
        public int RowNumber { get; set; }
        public string Sku { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string Imei1 { get; set; } = "";
        public string? Imei2 { get; set; }
        public string ErrorMessage { get; set; } = "";
        public bool IsValid { get; set; }

        // Parsed values
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int QuantityValue { get; set; }
        public bool IsSerialTracked { get; set; }
    }
}
