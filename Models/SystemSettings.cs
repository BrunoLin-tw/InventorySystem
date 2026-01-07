namespace InventorySystem.Models;

public class SystemSettings
{
    public string CompanyName { get; set; } = "未命名公司";
    public string ReportTitle { get; set; } = "進銷存報表";
    public string CompanyAddress { get; set; } = "";
    public string CompanyPhone { get; set; } = "";
    public string CompanyTaxId { get; set; } = "";
    public string CompanyOwner { get; set; } = "";
    public string BankInfo { get; set; } = "";
    public int InventoryAlertThreshold { get; set; } = 10;
    public DateTime LastBackup { get; set; }
    public string? LastBackupFile { get; set; }
    public DateTime LastVacuum { get; set; }
    public string AdminUsername { get; set; } = "admin";
    public string AdminPasswordHash { get; set; } = string.Empty;
}
