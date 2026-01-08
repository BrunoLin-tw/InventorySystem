using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task IncreaseStockAsync(int productId, int quantity, string reason)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                throw new InvalidOperationException($"找不到產品 ID {productId}");
            }

            product.QuantityOnHand += quantity;

            var log = new InventoryLog
            {
                ProductId = productId,
                Change = quantity,
                QuantityAfter = product.QuantityOnHand,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            _context.InventoryLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task DecreaseStockAsync(int productId, int quantity, string reason, string? referenceId = null)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                throw new InvalidOperationException($"找不到產品 ID {productId}");
            }

            if (product.QuantityOnHand < quantity)
            {
                throw new InvalidOperationException($"產品 ID {productId} 庫存不足，需求：{quantity}，目前：{product.QuantityOnHand}");
            }

            product.QuantityOnHand -= quantity;

            var log = new InventoryLog
            {
                ProductId = productId,
                Change = -quantity,
                QuantityAfter = product.QuantityOnHand,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(referenceId))
            {
                log.Reason = log.Reason is null ? referenceId : $"{log.Reason} ({referenceId})";
            }

            _context.InventoryLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
