using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventorySystem.Services
{
    public class OrderService
    {
        private readonly InventoryContext _context;
        private readonly InventoryService _inventoryService;

        public OrderService(InventoryContext context, InventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        // PurchaseOrder methods
        public async Task<List<PurchaseOrder>> GetAllPurchaseOrdersAsync()
        {
            return await _context.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ToListAsync();
        }

        public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id)
        {
            return await _context.PurchaseOrders
                .Include(o => o.Supplier)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task AddPurchaseOrderAsync(PurchaseOrder order)
        {
            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();

            if (order.Items != null && order.Items.Any())
            {
                foreach (var item in order.Items)
                {
                    await _inventoryService.IncreaseStockAsync(item.ProductId, item.Quantity, $"Purchase Order #{order.Id}");
                }
            }
        }

        public async Task UpdatePurchaseOrderAsync(PurchaseOrder order)
        {
            _context.PurchaseOrders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePurchaseOrderAsync(int id)
        {
            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order != null)
            {
                _context.PurchaseOrders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        // SalesOrder methods
        public async Task<List<SalesOrder>> GetAllSalesOrdersAsync()
        {
            return await _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ToListAsync();
        }

        public async Task<List<SalesOrder>> SearchSalesOrdersAsync(
            DateTime? startDate,
            DateTime? endDate,
            string? searchField,
            string? keyword)
        {
            var query = _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var nextDay = endDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < nextDay);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmed = keyword.Trim();
                if (searchField == "客戶名稱")
                {
                    query = query.Where(o => EF.Functions.Like(o.Customer!.Name ?? string.Empty, $"%{trimmed}%"));
                }
                else
                {
                    query = query.Where(o => EF.Functions.Like(o.OrderNumber ?? string.Empty, $"%{trimmed}%"));
                }
            }

            return await query.ToListAsync();
        }

        public async Task<SalesOrder?> GetSalesOrderByIdAsync(int id)
        {
            return await _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task AddSalesOrderAsync(SalesOrder order)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.SalesOrders.Add(order);
                await _context.SaveChangesAsync();

                if (order.Items != null && order.Items.Any())
                {
                    foreach (var item in order.Items)
                    {
                        await _inventoryService.DecreaseStockAsync(
                            item.ProductId,
                            item.Quantity,
                            "Sales Order",
                            order.OrderNumber);
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateSalesOrderAsync(SalesOrder order)
        {
            _context.SalesOrders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSalesOrderAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.SalesOrders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order != null)
                {
                    if (order.Items != null && order.Items.Any())
                    {
                        foreach (var item in order.Items)
                        {
                            await _inventoryService.IncreaseStockAsync(
                                item.ProductId,
                                item.Quantity,
                                $"Delete Sales Order #{order.OrderNumber}");
                        }
                    }

                    _context.SalesOrders.Remove(order);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Get suppliers and customers
        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers.ToListAsync();
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<int> UpdateAllTotalsAsync()
        {
            var updated = 0;

            var purchaseOrders = await _context.PurchaseOrders
                .Include(o => o.Items)
                .ToListAsync();

            foreach (var po in purchaseOrders)
            {
                var total = (po.Items != null && po.Items.Any()) ? po.Items.Sum(i => i.Quantity * i.UnitPrice) : 0m;
                if (po.Total != total)
                {
                    po.Total = total;
                    updated++;
                }
            }

            var salesOrders = await _context.SalesOrders
                .Include(o => o.Items)
                .ToListAsync();

            foreach (var so in salesOrders)
            {
                var total = (so.Items != null && so.Items.Any()) ? so.Items.Sum(i => i.Quantity * i.UnitPrice) : 0m;
                if (so.Total != total)
                {
                    so.Total = total;
                    updated++;
                }
            }

            if (updated > 0)
            {
                await _context.SaveChangesAsync();
            }

            return updated;
        }
    }
}
