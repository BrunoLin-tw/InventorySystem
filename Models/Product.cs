using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace InventorySystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string SKU { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int QuantityOnHand { get; set; }

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    }
}
