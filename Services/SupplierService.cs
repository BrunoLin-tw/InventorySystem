using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventorySystem.Services
{
    public class SupplierService
    {
        private readonly InventoryContext _context;

        public SupplierService(InventoryContext context)
        {
            _context = context;
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers.ToListAsync();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddSupplierAsync(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSupplierAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSupplierAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
        }
    }
}
