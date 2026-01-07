using System;
using System.ComponentModel.DataAnnotations;

namespace InventorySystem.Models
{
    public class InventoryLog
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Change { get; set; }

        public int QuantityAfter { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
