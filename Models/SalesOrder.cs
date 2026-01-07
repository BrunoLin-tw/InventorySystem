using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System;

namespace InventorySystem.Models
{
    public class SalesOrder
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = null!;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public DateTime OrderDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
    }
}
