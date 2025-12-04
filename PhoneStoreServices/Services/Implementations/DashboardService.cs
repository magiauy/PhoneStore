using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;

namespace PhoneStore.Services.Implementations
{
    /// <summary>
    /// Service for dashboard statistics
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IInvoiceLineRepository _invoiceLineRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;

        public DashboardService(
            IInvoiceRepository invoiceRepository,
            IInvoiceLineRepository invoiceLineRepository,
            ICustomerRepository customerRepository,
            IProductRepository productRepository)
        {
            _invoiceRepository = invoiceRepository;
            _invoiceLineRepository = invoiceLineRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
        }

        public DashboardMonthlyData GetMonthlyData(int year, int month)
        {
            var fromDate = new DateTime(year, month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            // Get invoices for the month
            var invoices = _invoiceRepository.GetInvoicesFiltered(
                null, null, null, null, fromDate, toDate, null, null, 1, int.MaxValue)
                .Where(i => i.Status != InvoiceStatus.CANCELLED)
                .ToList();

            var data = new DashboardMonthlyData
            {
                Year = year,
                Month = month,
                TotalRevenue = invoices.Sum(i => i.FinalAmount),
                TotalInvoices = invoices.Count,
                TotalProducts = _productRepository.GetAll().Count(),
                TotalCustomers = _customerRepository.GetAll().Count(),
                DailyRevenues = GetDailyRevenue(year, month),
                TopProducts = GetTopProducts(year, month, 10),
                TopCustomers = GetTopCustomers(year, month, 10)
            };

            return data;
        }

        public List<DailyRevenueViewModel> GetDailyRevenue(int year, int month)
        {
            var fromDate = new DateTime(year, month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var result = new List<DailyRevenueViewModel>();

            var invoices = _invoiceRepository.GetInvoicesFiltered(
                null, null, null, null, fromDate, toDate, null, null, 1, int.MaxValue)
                .Where(i => i.Status != InvoiceStatus.CANCELLED)
                .ToList();

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                var dayInvoices = invoices.Where(i => i.InvoiceDate.Date == date.Date).ToList();

                result.Add(new DailyRevenueViewModel
                {
                    Date = date,
                    Revenue = dayInvoices.Sum(i => i.FinalAmount),
                    InvoiceCount = dayInvoices.Count
                });
            }

            return result;
        }

        public List<TopProductViewModel> GetTopProducts(int year, int month, int count = 10)
        {
            var fromDate = new DateTime(year, month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            var invoices = _invoiceRepository.GetInvoicesFiltered(
                null, null, null, null, fromDate, toDate, null, null, 1, int.MaxValue)
                .Where(i => i.Status != InvoiceStatus.CANCELLED)
                .ToList();

            var productStats = new Dictionary<int, (string Name, string Sku, int Qty, decimal Revenue)>();

            foreach (var invoice in invoices)
            {
                var lines = _invoiceLineRepository.GetByInvoiceId(invoice.Id);
                foreach (var line in lines)
                {
                    var product = _productRepository.GetById(line.ProductId);
                    if (product == null) continue;

                    if (productStats.ContainsKey(line.ProductId))
                    {
                        var existing = productStats[line.ProductId];
                        productStats[line.ProductId] = (
                            existing.Name,
                            existing.Sku,
                            existing.Qty + line.Quantity,
                            existing.Revenue + line.TotalPrice
                        );
                    }
                    else
                    {
                        productStats[line.ProductId] = (
                            product.Name ?? "Unknown",
                            product.Sku ?? "",
                            line.Quantity,
                            line.TotalPrice
                        );
                    }
                }
            }

            return productStats
                .OrderByDescending(p => p.Value.Revenue)
                .Take(count)
                .Select((p, index) => new TopProductViewModel
                {
                    Rank = index + 1,
                    ProductId = p.Key,
                    ProductName = p.Value.Name,
                    ProductSku = p.Value.Sku,
                    QuantitySold = p.Value.Qty,
                    TotalRevenue = p.Value.Revenue
                })
                .ToList();
        }

        public List<TopCustomerViewModel> GetTopCustomers(int year, int month, int count = 10)
        {
            var fromDate = new DateTime(year, month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            var invoices = _invoiceRepository.GetInvoicesFiltered(
                null, null, null, null, fromDate, toDate, null, null, 1, int.MaxValue)
                .Where(i => i.Status != InvoiceStatus.CANCELLED)
                .ToList();

            var customerStats = invoices
                .GroupBy(i => i.PersonId)
                .Select(g => new
                {
                    PersonId = g.Key,
                    InvoiceCount = g.Count(),
                    TotalSpent = g.Sum(i => i.FinalAmount)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(count)
                .ToList();

            var result = new List<TopCustomerViewModel>();
            int rank = 1;
            foreach (var stat in customerStats)
            {
                string customerName = "Khách lẻ";
                string customerPhone = "";
                
                if (stat.PersonId.HasValue)
                {
                    var customer = _customerRepository.GetById(stat.PersonId.Value);
                    if (customer != null)
                    {
                        customerName = customer.FullName ?? "Khách lẻ";
                        customerPhone = customer.Phone ?? "";
                    }
                }

                result.Add(new TopCustomerViewModel
                {
                    Rank = rank++,
                    PersonId = stat.PersonId,
                    CustomerName = customerName,
                    CustomerPhone = customerPhone,
                    InvoiceCount = stat.InvoiceCount,
                    TotalSpent = stat.TotalSpent
                });
            }

            return result;
        }
    }
}
