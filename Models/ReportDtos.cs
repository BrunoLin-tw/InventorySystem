namespace InventorySystem.Models
{
    public class InventoryValueDto
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int QuantityOnHand { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal InventoryValue => QuantityOnHand * UnitPrice;
    }

    public class ProductSalesRankDto
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class MonthlySalesDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Period => $"{Year:D4}/{Month:D2}";
        public int OrderCount { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class CustomerContributionDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
