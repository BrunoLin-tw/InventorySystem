namespace InventorySystem.Models
{
    public class InventorySummaryDto
    {
        public int ProductId { get; set; }

        public string SKU { get; set; } = null!;

        public string ProductName { get; set; } = null!;

        public int CurrentStock { get; set; }

        public int TotalPurchased { get; set; }

        public int TotalSold { get; set; }

        public int Available => CurrentStock; // 目前已由即時計算提供，保留欄位以便未來調整
    }
}
