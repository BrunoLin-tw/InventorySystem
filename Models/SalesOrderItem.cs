using System.ComponentModel.DataAnnotations.Schema;

namespace InventorySystem.Models
{
    public class SalesOrderItem
    {
        public int Id { get; set; }

        public int SalesOrderId { get; set; }
        public SalesOrder SalesOrder { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}
