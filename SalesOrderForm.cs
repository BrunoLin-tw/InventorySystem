using InventorySystem.Models;
using InventorySystem.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InventorySystem;

public partial class SalesOrderForm : Form
{
    private readonly OrderService _service;
    private readonly ExcelService _excelService;
    private readonly SystemService _systemService;
    private List<SalesOrder> _orders = new();
    private PrintDocument _printDocument = null!;
    private PrintPreviewDialog _printPreviewDialog = null!;
    private SystemSettings _printSettings = new();
    private int _currentPrintIndex;
    private int _currentPageNumber;
    private SalesOrder? _currentPrintOrder;
    private List<SalesOrderItem> _currentPrintOrderItems = new();

    public SalesOrderForm(OrderService service, ExcelService excelService, SystemService systemService)
    {
        _service = service;
        _excelService = excelService;
        _systemService = systemService;
        InitializeComponent();
        this.Load += SalesOrderForm_Load;
    }

    private void InitializeComponent()
    {
        // Designer placeholder - UI created in Load event
    }

    private async void SalesOrderForm_Load(object sender, EventArgs e)
    {
        this.Text = "銷貨管理";
        this.Size = new Size(900, 650);
        this.StartPosition = FormStartPosition.CenterParent;
        SetupUI();
        await LoadOrders();
        _printSettings = await _systemService.GetSettingsAsync();
        _printDocument = CreatePrintDocument();
        _printPreviewDialog = new PrintPreviewDialog
        {
            Icon = this.Icon,
            Document = _printDocument,
            Width = 900,
            Height = 700
        };
    }

    private void SetupUI()
    {
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

        // Ensure row sizing: top row autosizes for buttons, bottom row fills remaining space for grid
        mainPanel.RowStyles.Clear();
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true
        };

        var btnAdd = new Button { Text = "新增訂單", Width = 100 };
        btnAdd.Click += (s, e) => OpenSalesOrderDialog(null);

        var btnDelete = new Button { Text = "刪除", Width = 80 };
        btnDelete.Click += (s, e) => DeleteSelectedOrder();

        var btnRefresh = new Button { Text = "刷新", Width = 80 };
        btnRefresh.Click += async (s, e) => await LoadOrders();

        var btnImport = new Button { Text = "匯入 Excel", Width = 110 };
        btnImport.Click += async (s, e) => await ImportExcelAsync();

        var btnExport = new Button { Text = "匯出 Excel", Width = 110 };
        btnExport.Click += async (s, e) => await ExportExcelAsync();

        var btnPrint = new Button { Text = "列印銷貨單", Width = 130 };
        btnPrint.Click += (s, e) => PrintOrders();

        btnPanel.Controls.Add(btnAdd);
        btnPanel.Controls.Add(btnDelete);
        btnPanel.Controls.Add(btnRefresh);
        btnPanel.Controls.Add(btnImport);
        btnPanel.Controls.Add(btnExport);
        btnPanel.Controls.Add(btnPrint);

        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            Name = "dgvOrders"
        };

        // Selection behaviour: select full row and only one row at a time
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;

        dgv.CellDoubleClick += DgvOrders_CellDoubleClick;
        
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Width = 50 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OrderNumber", HeaderText = "訂單號", Width = 120 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "客戶", Width = 200 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OrderDate", HeaderText = "日期", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Quantity",
            HeaderText = "數量",
            Width = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
            Visible = false,
            Name = "colQuantity"
        });
        dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "UnitPrice",
            HeaderText = "產品單價",
            Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
            Visible = false,
            Name = "colUnitPrice"
        });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "總金額", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });

        mainPanel.Controls.Add(btnPanel, 0, 0);
            mainPanel.Controls.Add(dgv, 0, 1);

            this.Controls.Add(mainPanel);
        }

    private PrintDocument CreatePrintDocument()
    {
        var document = new PrintDocument();
        document.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
        document.PrintPage += PrintDocument_PrintPage;
        return document;
    }

    private void PrintOrders()
    {
        var dgv = this.Controls.Find("dgvOrders", true).FirstOrDefault() as DataGridView;
        if (dgv == null || dgv.SelectedRows.Count == 0)
        {
            MessageBox.Show("請先選擇一筆訂單再進行列印。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedItem = dgv.SelectedRows[0].DataBoundItem;
        if (selectedItem == null)
        {
            MessageBox.Show("選取的訂單資料無法讀取，請重新選擇。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var idProperty = selectedItem.GetType().GetProperty("Id");
        if (idProperty == null)
        {
            MessageBox.Show("找不到訂單識別欄位。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var idValue = idProperty.GetValue(selectedItem);
        if (!int.TryParse(idValue?.ToString(), out var orderId))
        {
            MessageBox.Show("訂單 ID 格式錯誤，無法列印。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var order = _orders.FirstOrDefault(o => o.Id == orderId);
        if (order == null)
        {
            MessageBox.Show("對應的訂單資料不存在，請重新整理。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _currentPrintOrder = order;
        _currentPrintOrderItems = (order.Items ?? Enumerable.Empty<SalesOrderItem>()).ToList();
        _currentPrintIndex = 0;
        _currentPageNumber = 0;
        _printDocument.DocumentName = string.IsNullOrWhiteSpace(order.OrderNumber)
            ? _printSettings.ReportTitle ?? "銷貨報表"
            : $"銷貨單 {order.OrderNumber}";
        _printPreviewDialog.Document = _printDocument;
        _printPreviewDialog.ShowDialog(this);
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (_currentPrintOrder == null)
        {
            e.HasMorePages = false;
            return;
        }

        _currentPageNumber++;
        var graphics = e.Graphics;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        var marginBounds = e.MarginBounds;
        var titleFont = new Font("微軟正黑體", 16, FontStyle.Bold);
        var headerFont = new Font("微軟正黑體", 11, FontStyle.Regular);
        var tableHeaderFont = new Font("微軟正黑體", 11, FontStyle.Bold);
        var tableFont = new Font("微軟正黑體", 11);

        float y = marginBounds.Top;
        graphics.DrawString(_printSettings.ReportTitle ?? "銷貨報表", titleFont, Brushes.Black, new PointF(marginBounds.Left, y));
        y += 30;

        var companyInfo = string.Join("\n", new[]
        {
            _printSettings.CompanyName,
            _printSettings.CompanyAddress,
            $"電話：{_printSettings.CompanyPhone}   統編：{_printSettings.CompanyTaxId}"
        });
        float infoWidth = Math.Min(300f, marginBounds.Width * 0.5f);
        var infoRect = new RectangleF(marginBounds.Right - infoWidth, y, infoWidth, 60);
        var infoFormat = new StringFormat
        {
            Alignment = StringAlignment.Far,      // 右對齊
            LineAlignment = StringAlignment.Near, // 置頂對齊（可改為 Center）
            Trimming = StringTrimming.EllipsisCharacter
        };
        graphics.DrawString(companyInfo, headerFont, Brushes.Gray, infoRect, infoFormat);
        y += infoRect.Height + 10;

        using var orderInfoFont = new Font("微軟正黑體", 12, FontStyle.Bold);
        var customer = _currentPrintOrder.Customer;
        var customerName = string.IsNullOrWhiteSpace(customer?.Name) ? "無客戶" : customer!.Name;
        var customerPhone = string.IsNullOrWhiteSpace(customer?.Phone) ? "無" : customer!.Phone;
        var customerAddress = string.IsNullOrWhiteSpace(customer?.Address) ? "無" : customer!.Address;
        var orderLines = new[]
        {
            $"訂單號：{_currentPrintOrder.OrderNumber}",
            $"日期：{_currentPrintOrder.OrderDate:yyyy/MM/dd}",
            $"客戶：{customerName}",
            $"電話：{customerPhone}",
            $"地址：{customerAddress}"
        };

        foreach (var line in orderLines)
        {
            graphics.DrawString(line, orderInfoFont, Brushes.Black, new RectangleF(marginBounds.Left, y, marginBounds.Width, 25), new StringFormat { LineAlignment = StringAlignment.Near });
            y += 25;
        }
        y += 5;

        var tableTop = y;
        var itemColumnWidths = new[] { marginBounds.Width * 0.55f, marginBounds.Width * 0.2f, marginBounds.Width * 0.25f };
        var itemColumnHeaders = new[] { "產品名稱", "數量", "單價" };
        float x = marginBounds.Left;
        for (int i = 0; i < itemColumnHeaders.Length; i++)
        {
            var headerRect = new RectangleF(x, tableTop, itemColumnWidths[i], 26);
            graphics.FillRectangle(Brushes.LightGray, headerRect);
            graphics.DrawRectangle(Pens.DarkGray, headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height);
            graphics.DrawString(itemColumnHeaders[i], tableHeaderFont, Brushes.Black, headerRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            x += itemColumnWidths[i];
        }

        var rowHeight = 28f;
        var rowY = tableTop + 26;
        var reservedFooterHeight = 100;
        var availableHeight = marginBounds.Bottom - rowY - reservedFooterHeight;
        var rowsPerPage = Math.Max(1, (int)(availableHeight / rowHeight));
        var rowsPrinted = 0;

        if (_currentPrintOrderItems.Count == 0)
        {
            var emptyRect = new RectangleF(marginBounds.Left, rowY, marginBounds.Width, rowHeight);
            graphics.DrawRectangle(Pens.LightGray, emptyRect.X, emptyRect.Y, emptyRect.Width, emptyRect.Height);
            graphics.DrawString("無銷貨項目", tableFont, Brushes.Black, emptyRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            rowY += rowHeight;
            rowsPrinted++;
        }
        else
        {
            while (_currentPrintIndex < _currentPrintOrderItems.Count && rowsPrinted < rowsPerPage)
            {
                var item = _currentPrintOrderItems[_currentPrintIndex];
                x = marginBounds.Left;
                var rowRect = new RectangleF(x, rowY, marginBounds.Width, rowHeight);
                graphics.DrawRectangle(Pens.LightGray, rowRect.X, rowRect.Y, rowRect.Width, rowRect.Height);

                var productName = item.Product?.Name ?? "未命名產品";
                graphics.DrawString(productName, tableFont, Brushes.Black, new RectangleF(x + 4, rowY, itemColumnWidths[0] - 8, rowHeight), new StringFormat { LineAlignment = StringAlignment.Center });
                x += itemColumnWidths[0];
                graphics.DrawString(item.Quantity.ToString(CultureInfo.CurrentCulture), tableFont, Brushes.Black, new RectangleF(x + 4, rowY, itemColumnWidths[1] - 8, rowHeight), new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
                x += itemColumnWidths[1];
                graphics.DrawString(item.UnitPrice.ToString("N2", CultureInfo.CurrentCulture), tableFont, Brushes.Black, new RectangleF(x + 4, rowY, itemColumnWidths[2] - 8, rowHeight), new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });

                rowY += rowHeight;
                _currentPrintIndex++;
                rowsPrinted++;
            }
        }

        var hasMoreItems = _currentPrintIndex < _currentPrintOrderItems.Count;
        e.HasMorePages = hasMoreItems;

        if (!e.HasMorePages)
        {
            var summaryY = marginBounds.Bottom - 80;
            var orderTotalText = $"訂單總計：NT$ {_currentPrintOrder.Total:N2}";
            var printTimeText = $"列印時間：{DateTime.Now:yyyy/MM/dd HH:mm}";
            graphics.DrawString(orderTotalText, tableHeaderFont, Brushes.Black, new RectangleF(marginBounds.Left, summaryY, marginBounds.Width, 28), new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            graphics.DrawString(printTimeText, headerFont, Brushes.Gray, new RectangleF(marginBounds.Left, summaryY + 30, marginBounds.Width, 24), new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            _currentPrintOrder = null;
            _currentPrintIndex = 0;
        }

        var pageNumberText = $"第 {_currentPageNumber} 頁";
        var pageNumberSize = graphics.MeasureString(pageNumberText, tableFont);
        graphics.DrawString(pageNumberText, tableFont, Brushes.Gray, new PointF(marginBounds.Right - pageNumberSize.Width, marginBounds.Bottom + 5 - pageNumberSize.Height));
    }

    private async Task LoadOrders()
    {
        try
        {
            _orders = await _service.GetAllSalesOrdersAsync();
            var dgv = this.Controls.Find("dgvOrders", true).FirstOrDefault() as DataGridView;
            if (dgv != null)
            {
                dgv.DataSource = _orders.Select(o =>
                {
        var items = o.Items;
        var totalQuantity = items?.Sum(i => i.Quantity) ?? 0;
        var totalAmount = items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0m;
                    var averageUnitPrice = totalQuantity > 0 ? totalAmount / totalQuantity : 0m;

                    return new
                    {
                        o.Id,
                        o.OrderNumber,
                        CustomerName = o.Customer?.Name ?? "無",
                        o.OrderDate,
                        Quantity = totalQuantity,
                        UnitPrice = averageUnitPrice,
                        o.Total
                    };
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void DeleteSelectedOrder()
    {
        var dgv = this.Controls.Find("dgvOrders", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0)
        {
            var rowIndex = dgv.SelectedRows[0].Index;
            var order = _orders[rowIndex];
            if (MessageBox.Show($"確定刪除訂單 {order.OrderNumber} 嗎？", "確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    await _service.DeleteSalesOrderAsync(order.Id);
                    await LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"刪除失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("請選擇一個訂單", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void OpenSalesOrderDialog(SalesOrder? order)
    {
        var dialog = new SalesOrderEditDialog(_service, order);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadOrders();
        }
    }



    private void DgvOrders_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        if (sender is not DataGridView dgv)
            return;

        var dataItem = dgv.Rows[e.RowIndex].DataBoundItem;
        if (dataItem == null)
            return;

        var idProperty = dataItem.GetType().GetProperty("Id");
        if (idProperty == null)
            return;

        var idValue = idProperty.GetValue(dataItem);
        if (!int.TryParse(idValue?.ToString(), out var orderId))
            return;

        var order = _orders.FirstOrDefault(o => o.Id == orderId);
        if (order is null)
            return;

        OpenSalesOrderDialog(order);
    }

    private async Task ImportExcelAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
            Title = "選擇要匯入的銷貨資料"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var result = await _excelService.ImportSalesAsync(dialog.FileName);
            var message = $"新增 {result.Added} 筆，更新 {result.Updated} 筆";
            if (result.Errors.Any())
            {
                message += $"\n發現錯誤：\n{string.Join("\n", result.Errors)}";
            }

            MessageBox.Show(message, "匯入完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadOrders();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"匯入失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            Title = "儲存銷貨報表"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            await _excelService.ExportSalesAsync(dialog.FileName);
            MessageBox.Show("匯出完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"匯出失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
