using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace InventorySystem.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string TaxId { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? EnglishName { get; set; }

        [MaxLength(200)]
        public string? ContactName { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}
