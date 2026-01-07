using ClosedXML.Excel;
using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Services;

public class ExcelImportResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; } = new();
}

public class ExcelService
{
    private readonly InventoryContext _context;

    public ExcelService(InventoryContext context)
    {
        _context = context;
    }

    public async Task<ExcelImportResult> ImportCustomersAsync(string filePath)
    {
        var result = new ExcelImportResult();
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            result.Errors.Add("找不到工作表。" );
            return result;
        }

        var headers = ParseHeaders(worksheet.Row(1));
        if (!ValidateHeaders(headers, result, new[] {"TaxId", "Name"}))
        {
            return result;
        }

        var rows = worksheet.RowsUsed().Skip(1);
        foreach (var row in rows)
        {
            var rowIndex = row.RowNumber();
            var taxId = GetCellValue(row, headers, "TaxId");
            var name = GetCellValue(row, headers, "Name");
            if (string.IsNullOrWhiteSpace(taxId) || string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add($"第 {rowIndex} 列缺少統一編號或名稱。");
                continue;
            }

            var trimmedTaxId = taxId.Trim();
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.TaxId == trimmedTaxId);
            if (existing == null)
            {
                var customer = new Customer
                {
                    TaxId = trimmedTaxId,
                    Name = name.Trim(),
                    EnglishName = GetCellValue(row, headers, "EnglishName"),
                    Email = GetCellValue(row, headers, "Email"),
                    Phone = GetCellValue(row, headers, "Phone"),
                    Address = GetCellValue(row, headers, "Address")
                };
                await _context.Customers.AddAsync(customer);
                result.Added++;
            }
            else
            {
                existing.Name = name.Trim();
                existing.EnglishName = GetCellValue(row, headers, "EnglishName");
                existing.Email = GetCellValue(row, headers, "Email");
                existing.Phone = GetCellValue(row, headers, "Phone");
                existing.Address = GetCellValue(row, headers, "Address");
                result.Updated++;
            }
        }

        if (result.Added + result.Updated > 0)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<ExcelImportResult> ImportSuppliersAsync(string filePath)
    {
        var result = new ExcelImportResult();
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            result.Errors.Add("找不到工作表。");
            return result;
        }

        var headers = ParseHeaders(worksheet.Row(1));
        if (!ValidateHeaders(headers, result, new[] {"TaxId", "Name"}))
        {
            return result;
        }

        var rows = worksheet.RowsUsed().Skip(1);
        foreach (var row in rows)
        {
            var rowIndex = row.RowNumber();
            var taxId = GetCellValue(row, headers, "TaxId");
            var name = GetCellValue(row, headers, "Name");
            if (string.IsNullOrWhiteSpace(taxId) || string.IsNullOrWhiteSpace(name))
            {
                result.Errors.Add($"第 {rowIndex} 列缺少統一編號或名稱。");
                continue;
            }

            var trimmedTaxId = taxId.Trim();
            var existing = await _context.Suppliers.FirstOrDefaultAsync(s => s.TaxId == trimmedTaxId);
            if (existing == null)
            {
                var supplier = new Supplier
                {
                    TaxId = trimmedTaxId,
                    Name = name.Trim(),
                    EnglishName = GetCellValue(row, headers, "EnglishName"),
                    ContactName = GetCellValue(row, headers, "ContactName"),
                    Email = GetCellValue(row, headers, "Email"),
                    Phone = GetCellValue(row, headers, "Phone"),
                    Address = GetCellValue(row, headers, "Address")
                };
                await _context.Suppliers.AddAsync(supplier);
                result.Added++;
            }
            else
            {
                existing.Name = name.Trim();
                existing.EnglishName = GetCellValue(row, headers, "EnglishName");
                existing.ContactName = GetCellValue(row, headers, "ContactName");
                existing.Email = GetCellValue(row, headers, "Email");
                existing.Phone = GetCellValue(row, headers, "Phone");
                existing.Address = GetCellValue(row, headers, "Address");
                result.Updated++;
            }
        }

        if (result.Added + result.Updated > 0)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<ExcelImportResult> ImportPurchasesAsync(string filePath)
    {
        var result = new ExcelImportResult();
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            result.Errors.Add("找不到工作表。");
            return result;
        }

        var headers = ParseHeaders(worksheet.Row(1));
        if (!ValidateHeaders(headers, result, new[] { "OrderNumber", "OrderDate", "TaxId", "Quantity", "UnitPrice" }))
        {
            return result;
        }

        var rows = worksheet.RowsUsed().Skip(1);
        foreach (var row in rows)
        {
            var rowIndex = row.RowNumber();
            var orderNumber = GetCellValue(row, headers, "OrderNumber");
            var taxId = GetCellValue(row, headers, "TaxId");
            var orderDateValue = GetCellValue(row, headers, "OrderDate");
            var quantityValue = GetCellValue(row, headers, "Quantity");
            var unitPriceValue = GetCellValue(row, headers, "UnitPrice");
            if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(taxId) || string.IsNullOrWhiteSpace(orderDateValue))
            {
                result.Errors.Add($"第 {rowIndex} 列缺少必要欄位。OrderNumber/TaxId/OrderDate 皆須存在。" );
                continue;
            }

            if (!DateTime.TryParse(orderDateValue, out var orderDate))
            {
                result.Errors.Add($"第 {rowIndex} 列日期格式錯誤：{orderDateValue}");
                continue;
            }

            if (!int.TryParse(quantityValue, out var quantity))
            {
                result.Errors.Add($"第 {rowIndex} 列數量須為整數：{quantityValue}");
                continue;
            }

            if (!decimal.TryParse(unitPriceValue, out var unitPrice))
            {
                result.Errors.Add($"第 {rowIndex} 列單價須為數字：{unitPriceValue}");
                continue;
            }

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.TaxId == taxId.Trim());
            if (supplier == null)
            {
                result.Errors.Add($"第 {rowIndex} 列找不到統一編號為 {taxId} 的供應商。");
                continue;
            }

            var existing = await _context.PurchaseOrders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber.Trim());
            if (existing == null)
            {
                var order = new PurchaseOrder
                {
                    OrderNumber = orderNumber.Trim(),
                    SupplierId = supplier.Id,
                    OrderDate = orderDate,
                    Items = new List<PurchaseOrderItem>
                    {
                        new PurchaseOrderItem
                        {
                            ProductId = 0,
                            Quantity = quantity,
                            UnitPrice = unitPrice
                        }
                    }
                };
                _context.PurchaseOrders.Add(order);
                result.Added++;
            }
            else
            {
                existing.OrderDate = orderDate;
                existing.SupplierId = supplier.Id;
                var item = existing.Items.FirstOrDefault();
                if (item is null)
                {
                    existing.Items.Add(new PurchaseOrderItem { ProductId = 0, Quantity = quantity, UnitPrice = unitPrice });
                }
                else
                {
                    item.Quantity = quantity;
                    item.UnitPrice = unitPrice;
                }
                result.Updated++;
            }
        }

        if (result.Added + result.Updated > 0)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<ExcelImportResult> ImportSalesAsync(string filePath)
    {
        var result = new ExcelImportResult();
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            result.Errors.Add("找不到工作表。");
            return result;
        }

        var headers = ParseHeaders(worksheet.Row(1));
        if (!ValidateHeaders(headers, result, new[] { "OrderNumber", "OrderDate", "TaxId", "Quantity", "UnitPrice" }))
        {
            return result;
        }

        var rows = worksheet.RowsUsed().Skip(1);
        foreach (var row in rows)
        {
            var rowIndex = row.RowNumber();
            var orderNumber = GetCellValue(row, headers, "OrderNumber");
            var taxId = GetCellValue(row, headers, "TaxId");
            var orderDateValue = GetCellValue(row, headers, "OrderDate");
            var quantityValue = GetCellValue(row, headers, "Quantity");
            var unitPriceValue = GetCellValue(row, headers, "UnitPrice");
            if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(taxId) || string.IsNullOrWhiteSpace(orderDateValue))
            {
                result.Errors.Add($"第 {rowIndex} 列缺少必要欄位。OrderNumber/TaxId/OrderDate 皆須存在。" );
                continue;
            }

            if (!DateTime.TryParse(orderDateValue, out var orderDate))
            {
                result.Errors.Add($"第 {rowIndex} 列日期格式錯誤：{orderDateValue}");
                continue;
            }

            if (!int.TryParse(quantityValue, out var quantity))
            {
                result.Errors.Add($"第 {rowIndex} 列數量須為整數：{quantityValue}");
                continue;
            }

            if (!decimal.TryParse(unitPriceValue, out var unitPrice))
            {
                result.Errors.Add($"第 {rowIndex} 列單價須為數字：{unitPriceValue}");
                continue;
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.TaxId == taxId.Trim());
            if (customer == null)
            {
                result.Errors.Add($"第 {rowIndex} 列找不到統一編號為 {taxId} 的客戶。");
                continue;
            }

            var existing = await _context.SalesOrders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber.Trim());
            if (existing == null)
            {
                var order = new SalesOrder
                {
                    OrderNumber = orderNumber.Trim(),
                    CustomerId = customer.Id,
                    OrderDate = orderDate,
                    Items = new List<SalesOrderItem>
                    {
                        new SalesOrderItem
                        {
                            ProductId = 0,
                            Quantity = quantity,
                            UnitPrice = unitPrice
                        }
                    }
                };
                _context.SalesOrders.Add(order);
                result.Added++;
            }
            else
            {
                existing.OrderDate = orderDate;
                existing.CustomerId = customer.Id;
                var item = existing.Items.FirstOrDefault();
                if (item is null)
                {
                    existing.Items.Add(new SalesOrderItem { ProductId = 0, Quantity = quantity, UnitPrice = unitPrice });
                }
                else
                {
                    item.Quantity = quantity;
                    item.UnitPrice = unitPrice;
                }
                result.Updated++;
            }
        }

        if (result.Added + result.Updated > 0)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task ExportPurchasesAsync(string filePath)
    {
        var orders = await _context.PurchaseOrders.Include(o => o.Supplier).Include(o => o.Items).ToListAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Purchases");
        worksheet.Cell(1, 1).Value = "OrderNumber";
        worksheet.Cell(1, 2).Value = "SupplierTaxId";
        worksheet.Cell(1, 3).Value = "OrderDate";
        worksheet.Cell(1, 4).Value = "Quantity";
        worksheet.Cell(1, 5).Value = "UnitPrice";
        worksheet.Cell(1, 6).Value = "Total";

        for (var i = 0; i < orders.Count; i++)
        {
            var row = i + 2;
            var order = orders[i];
            var items = order.Items;
            var totalQuantity = items?.Sum(i => i.Quantity) ?? 0;
            var totalAmount = items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0m;
            var averageUnitPrice = totalQuantity > 0 ? totalAmount / totalQuantity : 0m;
            worksheet.Cell(row, 1).Value = order.OrderNumber;
            worksheet.Cell(row, 2).Value = order.Supplier?.TaxId;
            worksheet.Cell(row, 3).Value = order.OrderDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 4).Value = totalQuantity;
            worksheet.Cell(row, 5).Value = averageUnitPrice;
            worksheet.Cell(row, 6).Value = totalAmount;
        }

        workbook.SaveAs(filePath);
    }

    public async Task ExportSalesAsync(string filePath)
    {
        var orders = await _context.SalesOrders.Include(o => o.Customer).Include(o => o.Items).ToListAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sales");
        worksheet.Cell(1, 1).Value = "OrderNumber";
        worksheet.Cell(1, 2).Value = "CustomerTaxId";
        worksheet.Cell(1, 3).Value = "OrderDate";
        worksheet.Cell(1, 4).Value = "Quantity";
        worksheet.Cell(1, 5).Value = "UnitPrice";
        worksheet.Cell(1, 6).Value = "Total";

        for (var i = 0; i < orders.Count; i++)
        {
            var row = i + 2;
            var order = orders[i];
            var items = order.Items;
            var totalQuantity = items?.Sum(i => i.Quantity) ?? 0;
            var totalAmount = items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0m;
            var averageUnitPrice = totalQuantity > 0 ? totalAmount / totalQuantity : 0m;
            worksheet.Cell(row, 1).Value = order.OrderNumber;
            worksheet.Cell(row, 2).Value = order.Customer?.TaxId;
            worksheet.Cell(row, 3).Value = order.OrderDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 4).Value = totalQuantity;
            worksheet.Cell(row, 5).Value = averageUnitPrice;
            worksheet.Cell(row, 6).Value = totalAmount;
        }

        workbook.SaveAs(filePath);
    }

    public async Task ExportCustomersAsync(string filePath)
    {
        var customers = await _context.Customers.AsNoTracking().ToListAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Customers");
        worksheet.Cell(1, 1).Value = "TaxId";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "EnglishName";
        worksheet.Cell(1, 4).Value = "Email";
        worksheet.Cell(1, 5).Value = "Phone";
        worksheet.Cell(1, 6).Value = "Address";

        for (var i = 0; i < customers.Count; i++)
        {
            var row = i + 2;
            var customer = customers[i];
            worksheet.Cell(row, 1).Value = customer.TaxId;
            worksheet.Cell(row, 2).Value = customer.Name;
            worksheet.Cell(row, 3).Value = customer.EnglishName;
            worksheet.Cell(row, 4).Value = customer.Email;
            worksheet.Cell(row, 5).Value = customer.Phone;
            worksheet.Cell(row, 6).Value = customer.Address;
        }

        workbook.SaveAs(filePath);
    }

    public async Task ExportSuppliersAsync(string filePath)
    {
        var suppliers = await _context.Suppliers.AsNoTracking().ToListAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Suppliers");
        worksheet.Cell(1, 1).Value = "TaxId";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "EnglishName";
        worksheet.Cell(1, 4).Value = "ContactName";
        worksheet.Cell(1, 5).Value = "Email";
        worksheet.Cell(1, 6).Value = "Phone";
        worksheet.Cell(1, 7).Value = "Address";

        for (var i = 0; i < suppliers.Count; i++)
        {
            var row = i + 2;
            var supplier = suppliers[i];
            worksheet.Cell(row, 1).Value = supplier.TaxId;
            worksheet.Cell(row, 2).Value = supplier.Name;
            worksheet.Cell(row, 3).Value = supplier.EnglishName;
            worksheet.Cell(row, 4).Value = supplier.ContactName;
            worksheet.Cell(row, 5).Value = supplier.Email;
            worksheet.Cell(row, 6).Value = supplier.Phone;
            worksheet.Cell(row, 7).Value = supplier.Address;
        }

        workbook.SaveAs(filePath);
    }

    private static Dictionary<string, int> ParseHeaders(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.Cells())
        {
            var key = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(key))
            {
                map[key] = cell.Address.ColumnNumber;
            }
        }
        return map;
    }

    private static bool ValidateHeaders(Dictionary<string, int> headers, ExcelImportResult result, string[] required)
    {
        foreach (var key in required)
        {
            if (!headers.ContainsKey(key))
            {
                result.Errors.Add($"缺少欄位：{key}");
            }
        }

        return result.Errors.Count == 0;
    }

    private static string? GetCellValue(IXLRow row, Dictionary<string, int> headers, string key)
    {
        if (!headers.TryGetValue(key, out var column))
        {
            return null;
        }

        return row.Cell(column).GetString()?.Trim();
    }
}
