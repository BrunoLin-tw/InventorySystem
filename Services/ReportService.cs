using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Services
{
    public class ReportService
    {
        private readonly InventoryContext _context;

        public ReportService(InventoryContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryValueDto>> GetInventoryValuationAsync()
        {
            var inventory = await _context.Products
                .Select(p => new InventoryValueDto
                {
                    ProductId = p.Id,
                    SKU = p.SKU,
                    ProductName = p.Name,
                    QuantityOnHand = p.QuantityOnHand,
                    UnitPrice = p.UnitPrice
                })
                .ToListAsync();

            return inventory.OrderByDescending(p => p.QuantityOnHand * p.UnitPrice).ToList();
        }

        public async Task<List<ProductSalesRankDto>> GetProductSalesRankingAsync(int top = 20)
        {
            var items = await _context.SalesOrderItems
                .Include(i => i.Product)
                .Where(i => i.Product != null)
                .ToListAsync();

            var ranking = items
                .GroupBy(i => new { i.ProductId, i.Product!.SKU, i.Product.Name })
                .Select(g => new ProductSalesRankDto
                {
                    ProductId = g.Key.ProductId,
                    SKU = g.Key.SKU,
                    ProductName = g.Key.Name,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
                })
                .ToList();

            return ranking
                .OrderByDescending(dto => dto.TotalRevenue)
                .ThenByDescending(dto => dto.TotalQuantity)
                .Take(top)
                .ToList();
        }

        public async Task<List<MonthlySalesDto>> GetMonthlySalesTrendAsync(int? year = null)
        {
            var query = _context.SalesOrders
                .Include(o => o.Items)
                .AsQueryable();

            if (year.HasValue)
            {
                query = query.Where(o => o.OrderDate.Year == year.Value);
            }

            var orderItems = await query
                .SelectMany(o => o.Items, (order, item) => new { order.OrderDate, item.Quantity, item.UnitPrice })
                .ToListAsync();

            return orderItems
                .GroupBy(x => new { Year = x.OrderDate.Year, Month = x.OrderDate.Month })
                .Select(g => new MonthlySalesDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count(),
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderBy(dto => dto.Year)
                .ThenBy(dto => dto.Month)
                .ToList();
        }

        public async Task<List<CustomerContributionDto>> GetCustomerContributionAsync(int top = 20)
        {
            var orders = await _context.SalesOrders
                .Include(o => o.Customer)
                .ToListAsync();

            var contributions = orders
                .GroupBy(o => new { o.CustomerId, o.Customer.Name })
                .Select(g => new CustomerContributionDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.Name,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.Total)
                })
                .ToList();

            return contributions
                .OrderByDescending(dto => dto.TotalRevenue)
                .ThenByDescending(dto => dto.OrderCount)
                .Take(top)
                .ToList();
        }
    }
}
