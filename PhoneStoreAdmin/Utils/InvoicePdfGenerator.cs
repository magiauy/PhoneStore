using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Utils;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Linq;

namespace PhoneStoreAdmin.Utils
{
    /// <summary>
    /// Model for proforma invoice items (before actual sale)
    /// </summary>
    public class ProformaInvoiceItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalPrice { get; set; }
        public List<string> SerialNumbers { get; set; } = new();
    }

    /// <summary>
    /// Model for proforma invoice data
    /// </summary>
    public class ProformaInvoiceData
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public List<ProformaInvoiceItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal VatPercent { get; set; } = 10;
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public string? Note { get; set; }
        public bool IsProforma { get; set; } = true;
        public string? InvoiceNumber { get; set; }
        public string? PromotionCode { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Generates PDF invoices for both proforma (temporary) and actual sales invoices
    /// </summary>
    public class InvoicePdfGenerator
    {
        private const string STORE_NAME = "PHONE STORE";
        private const string STORE_ADDRESS = "123 Main St, District 1, Ho Chi Minh City";
        private const string STORE_PHONE = "(028) 1234-5678";
        private const string STORE_EMAIL = "contact@phonestore.vn";

        /// <summary>
        /// Generate PDF for a proforma (temporary) invoice
        /// </summary>
        public static void GenerateProformaPdf(ProformaInvoiceData data, string filePath)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            data.IsProforma = true;

            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Element(c => ComposeHeader(c, data.IsProforma));
                        page.Content().Element(c => ComposeContent(c, data));
                        page.Footer().Element(ComposeFooter);
                    });
                });

                document.GeneratePdf(filePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to generate proforma PDF: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generate PDF for an actual invoice from database
        /// </summary>
        public static void GenerateInvoicePdf(Invoice invoice, string filePath)
        {
            if (invoice == null)
                throw new ArgumentNullException(nameof(invoice));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            var data = ConvertInvoiceToData(invoice);
            data.IsProforma = false;

            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Element(c => ComposeHeader(c, data.IsProforma));
                        page.Content().Element(c => ComposeContent(c, data));
                        page.Footer().Element(ComposeFooter);
                    });
                });

                document.GeneratePdf(filePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to generate invoice PDF: {ex.Message}", ex);
                throw;
            }
        }

        private static ProformaInvoiceData ConvertInvoiceToData(Invoice invoice)
        {
            var productRepo = App.GetService<IProductRepository>();
            var data = new ProformaInvoiceData
            {
                CustomerName = invoice.Customer?.FullName ?? "Khach le",
                CustomerPhone = invoice.Customer?.Phone ?? "",
                Date = invoice.InvoiceDate,
                InvoiceNumber = $"INV-{invoice.Id:D6}",
                Subtotal = invoice.TotalAmount,
                DiscountAmount = invoice.DiscountAmount,
                Total = invoice.FinalAmount,
                VatAmount = 0, // Calculate if needed
                Note = invoice.Note,
                PromotionCode = invoice.PromotionCode?.Code,
                PaymentMethod = GetPaymentMethodText(invoice.PaymentMethod),
                CreatedBy = invoice.Creator?.FullName ?? "System",
                Items = new List<ProformaInvoiceItem>()
            };

            foreach (var line in invoice.InvoiceLines ?? new List<InvoiceLine>())
            {
                var productName = line.Product?.Name ?? "Unknown Product";
                if (productRepo != null && line.Product == null)
                {
                    try
                    {
                        var product = productRepo.GetById(line.ProductId);
                        productName = product?.Name ?? $"Product #{line.ProductId}";
                    }
                    catch
                    {
                        productName = $"Product #{line.ProductId}";
                    }
                }

                var item = new ProformaInvoiceItem
                {
                    ProductName = productName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    DiscountPercent = line.DiscountPct,
                    TotalPrice = line.TotalPrice,
                    SerialNumbers = new List<string>() // Serial numbers not available in InvoiceLineSerial model
                };

                data.Items.Add(item);
            }

            return data;
        }

        private static void ComposeHeader(IContainer container, bool isProforma)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(STORE_NAME).FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text(STORE_ADDRESS).FontSize(9);
                        col.Item().Text($"DT: {STORE_PHONE} | Email: {STORE_EMAIL}").FontSize(9);
                    });

                    row.ConstantItem(150).AlignRight().Column(col =>
                    {
                        if (isProforma)
                        {
                            col.Item().Background(Colors.Orange.Lighten3).Padding(5)
                                .Text("HOA DON TAM TINH").FontSize(12).Bold().FontColor(Colors.Orange.Darken3);
                            col.Item().Text("PROFORMA INVOICE").FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            col.Item().Background(Colors.Green.Lighten3).Padding(5)
                                .Text("HOA DON BAN HANG").FontSize(12).Bold().FontColor(Colors.Green.Darken3);
                            col.Item().Text("SALES INVOICE").FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
            });
        }

        private static void ComposeContent(IContainer container, ProformaInvoiceData data)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Spacing(8);

                // Invoice info and customer info
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("THONG TIN HOA DON").Bold().FontSize(11);
                        if (!string.IsNullOrEmpty(data.InvoiceNumber))
                        {
                            col.Item().Text($"So HD: {data.InvoiceNumber}");
                        }
                        col.Item().Text($"Ngay: {data.Date:dd/MM/yyyy HH:mm}");
                        if (!string.IsNullOrEmpty(data.PaymentMethod))
                        {
                            col.Item().Text($"Thanh toan: {RemoveVietnamese(data.PaymentMethod)}");
                        }
                        if (!string.IsNullOrEmpty(data.CreatedBy))
                        {
                            col.Item().Text($"NV ban hang: {RemoveVietnamese(data.CreatedBy)}");
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("THONG TIN KHACH HANG").Bold().FontSize(11);
                        col.Item().Text($"Ten: {RemoveVietnamese(data.CustomerName)}");
                        if (!string.IsNullOrEmpty(data.CustomerPhone))
                        {
                            col.Item().Text($"SDT: {data.CustomerPhone}");
                        }
                        if (!string.IsNullOrEmpty(data.PromotionCode))
                        {
                            col.Item().Text($"Ma KM: {data.PromotionCode}");
                        }
                    });
                });

                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Products table
                column.Item().Text("CHI TIET SAN PHAM").Bold().FontSize(11);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);  // STT
                        columns.RelativeColumn(3);   // Product name
                        columns.ConstantColumn(40);  // Qty
                        columns.RelativeColumn(1.2f);  // Unit price
                        columns.ConstantColumn(40);  // Discount
                        columns.RelativeColumn(1.2f);  // Total
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("STT").Bold();
                        header.Cell().Element(HeaderCellStyle).Text("San pham").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignCenter().Text("SL").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Don gia").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignCenter().Text("CK%").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Thanh tien").Bold();

                        static IContainer HeaderCellStyle(IContainer c)
                        {
                            return c.Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Blue.Lighten4).Padding(4);
                        }
                    });

                    // Items
                    int index = 1;
                    foreach (var item in data.Items)
                    {
                        table.Cell().Element(CellStyle).Text(index.ToString());
                        
                        // Product name with serials if any
                        table.Cell().Element(CellStyle).Column(col =>
                        {
                            col.Item().Text(RemoveVietnamese(item.ProductName));
                            if (item.SerialNumbers.Any())
                            {
                                foreach (var serial in item.SerialNumbers)
                                {
                                    col.Item().Text($"  SN: {serial}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                }
                            }
                        });
                        
                        table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(item.UnitPrice));
                        table.Cell().Element(CellStyle).AlignCenter().Text(item.DiscountPercent > 0 ? $"{item.DiscountPercent}%" : "-");
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(item.TotalPrice));
                        
                        index++;

                        static IContainer CellStyle(IContainer c)
                        {
                            return c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
                        }
                    }
                });

                // Totals section
                column.Item().PaddingTop(10).AlignRight().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(150).Text("Tam tinh:").SemiBold();
                        row.ConstantItem(120).AlignRight().Text(FormatCurrency(data.Subtotal));
                    });

                    if (data.VatAmount > 0)
                    {
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text($"VAT ({data.VatPercent}%):").SemiBold();
                            row.ConstantItem(120).AlignRight().Text(FormatCurrency(data.VatAmount));
                        });
                    }

                    if (data.DiscountAmount > 0)
                    {
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(150).Text("Giam gia:").SemiBold();
                            row.ConstantItem(120).AlignRight().Text($"-{FormatCurrency(data.DiscountAmount)}")
                                .FontColor(Colors.Red.Darken1);
                        });
                    }

                    col.Item().PaddingVertical(3).LineHorizontal(1);

                    col.Item().Row(row =>
                    {
                        row.ConstantItem(150).Text("TONG CONG:").Bold().FontSize(13);
                        row.ConstantItem(120).AlignRight().Text(FormatCurrency(data.Total))
                            .Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                    });
                });

                // Note section
                if (!string.IsNullOrWhiteSpace(data.Note))
                {
                    column.Item().PaddingTop(15).Column(col =>
                    {
                        col.Item().Text("Ghi chu:").Bold();
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                            .Text(RemoveVietnamese(data.Note) ?? string.Empty);
                    });
                }

                // Proforma warning
                if (data.IsProforma)
                {
                    column.Item().PaddingTop(15).Background(Colors.Orange.Lighten4).Padding(10).Column(col =>
                    {
                        col.Item().Text("LUU Y:").Bold().FontColor(Colors.Orange.Darken3);
                        col.Item().Text("Day la hoa don tam tinh, chua co gia tri thanh toan.")
                            .FontColor(Colors.Orange.Darken2);
                        col.Item().Text("Gia va khuyen mai co the thay doi khi thanh toan chinh thuc.")
                            .FontColor(Colors.Orange.Darken2);
                    });
                }

                // Thank you message
                column.Item().PaddingTop(20).AlignCenter().Column(col =>
                {
                    col.Item().Text("Cam on quy khach da mua hang!").Bold().FontSize(12);
                    col.Item().Text("Thank you for your purchase!").FontSize(10).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text($"In ngay: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                    row.RelativeItem().AlignRight().Text("Phone Store - Uy tin tao nen thuong hieu")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private static string RemoveVietnamese(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text ?? string.Empty;

            try
            {
                var normalizedString = text.Normalize(NormalizationForm.FormD);
                var stringBuilder = new StringBuilder();

                foreach (var c in normalizedString)
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    {
                        stringBuilder.Append(c);
                    }
                }

                // Handle special Vietnamese characters
                var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
                result = result.Replace("đ", "d").Replace("Đ", "D");
                return result;
            }
            catch
            {
                return text;
            }
        }

        private static string FormatCurrency(decimal amount)
        {
            return $"{amount:N0} VND";
        }

        private static string GetPaymentMethodText(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.CASH => "Tien mat",
                PaymentMethod.CARD => "The",
                PaymentMethod.BANK => "Chuyen khoan",
                PaymentMethod.EWALLET => "Vi dien tu",
                _ => "Khac"
            };
        }
    }
}
