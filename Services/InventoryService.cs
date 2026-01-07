using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventorySystem.Services
{
    public class InventoryService
    {
        private readonly InventoryContext _context;

        public InventoryService(InventoryContext context)
        {
            _context = context;
        }

        public async Task<List<InventorySummaryDto>> GetInventorySummaryAsync()
        {
            var products = await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.InventoryLogs)
                .Include(p => p.PurchaseOrderItems)
                .Include(p => p.SalesOrderItems)
                .ToListAsync();

            return products.Select(p => new InventorySummaryDto
            {
                ProductId = p.Id,
                SKU = p.SKU,
                ProductName = p.Name,
                CurrentStock = p.QuantityOnHand,
                TotalPurchased = p.PurchaseOrderItems.Sum(i => i.Quantity),
                TotalSold = p.SalesOrderItems.Sum(i => i.Quantity)
            }).ToList();
        }

        public async Task<List<InventoryLog>> GetProductHistoryAsync(int productId)
        {
            return await _context.InventoryLogs
                .Where(l => l.ProductId == productId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
