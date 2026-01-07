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
        private ComboBox _quickFilterCombo = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private ComboBox _searchFieldCombo = null!;
        private TextBox _keywordTextBox = null!;

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
            await ExecuteSearchAsync();
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

        mainPanel.RowStyles.Clear();
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false
        };

        var btnAdd = new Button { Text = "新增", Width = 60 };
        btnAdd.Click += (s, e) => OpenSalesOrderDialog(null);

        var btnDelete = new Button { Text = "刪除", Width = 60 };
        btnDelete.Click += (s, e) => DeleteSelectedOrder();

        var btnRefresh = new Button { Text = "刷新", Width = 60 };
        btnRefresh.Click += async (s, e) => await ExecuteSearchAsync();

        var btnImport = new Button { Text = "匯入", Width = 60 };
        btnImport.Click += async (s, e) => await ImportExcelAsync();

        var btnExport = new Button { Text = "匯出", Width = 60 };
        btnExport.Click += async (s, e) => await ExportExcelAsync();

        var btnPrint = new Button { Text = "列印", Width = 60 };
        btnPrint.Click += (s, e) => PrintOrders();

        var queryPanel = CreateQueryPanel();

        toolbar.Controls.Add(btnAdd);
        toolbar.Controls.Add(queryPanel);
        toolbar.Controls.Add(btnDelete);
        toolbar.Controls.Add(btnRefresh);
        toolbar.Controls.Add(btnImport);
        toolbar.Controls.Add(btnExport);
        toolbar.Controls.Add(btnPrint);

        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            Name = "dgvOrders"
        };

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

        mainPanel.Controls.Add(toolbar, 0, 0);
        mainPanel.Controls.Add(dgv, 0, 1);

        this.Controls.Add(mainPanel);
    }

    private FlowLayoutPanel CreateQueryPanel()
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(5, 0, 0, 0)
        };

        panel.Controls.Add(new Label { Text = "速查", AutoSize = true, Margin = new Padding(5, 7, 3, 0) });

        _quickFilterCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 80,
            Margin = new Padding(0, 3, 5, 3)
        };
        _quickFilterCombo.Items.AddRange(new[] { "今日", "昨日", "本週", "上週", "本月", "上月" });
        _quickFilterCombo.SelectedIndexChanged += QuickFilterCombo_SelectedIndexChanged;
        panel.Controls.Add(_quickFilterCombo);

        panel.Controls.Add(new Label { Text = "開始", AutoSize = true, Margin = new Padding(5, 7, 3, 0) });
        _startDatePicker = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100, Margin = new Padding(0, 3, 5, 3) };
        panel.Controls.Add(_startDatePicker);

        panel.Controls.Add(new Label { Text = "截止", AutoSize = true, Margin = new Padding(5, 7, 3, 0) });
        _endDatePicker = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100, Margin = new Padding(0, 3, 5, 3) };
        panel.Controls.Add(_endDatePicker);

        panel.Controls.Add(new Label { Text = "欄位", AutoSize = true, Margin = new Padding(5, 7, 3, 0) });
        _searchFieldCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 90,
            Margin = new Padding(0, 3, 5, 3)
        };
        _searchFieldCombo.Items.AddRange(new[] { "訂單號", "客戶名稱" });
        panel.Controls.Add(_searchFieldCombo);

        panel.Controls.Add(new Label { Text = "關鍵字", AutoSize = true, Margin = new Padding(5, 7, 3, 0) });
        _keywordTextBox = new TextBox { Width = 140, Margin = new Padding(0, 3, 5, 3) };
        panel.Controls.Add(_keywordTextBox);

        var btnSearch = new Button { Text = "查詢", Width = 60, Margin = new Padding(0, 3, 5, 3) };
        btnSearch.Click += async (s, e) => await ExecuteSearchAsync();
        panel.Controls.Add(btnSearch);

        _searchFieldCombo.SelectedIndex = 0;
        _quickFilterCombo.SelectedItem = "本月";
        ApplyQuickFilterDates();
        return panel;
    }

    private void QuickFilterCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        ApplyQuickFilterDates();
    }

    private void ApplyQuickFilterDates()
    {
        if (_quickFilterCombo.SelectedItem is not string selection)
        {
            return;
        }

        var today = DateTime.Today;
        var culture = CultureInfo.CurrentCulture;
        var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
        DateTime start;
        DateTime end;

        switch (selection)
        {
            case "今日":
                start = end = today;
                break;
            case "昨日":
                start = end = today.AddDays(-1);
                break;
            case "本週":
                start = GetStartOfWeek(today, firstDayOfWeek);
                end = start.AddDays(6);
                break;
            case "上週":
                var currentWeekStart = GetStartOfWeek(today, firstDayOfWeek);
                start = currentWeekStart.AddDays(-7);
                end = currentWeekStart.AddDays(-1);
                break;
            case "本月":
                start = new DateTime(today.Year, today.Month, 1);
                end = start.AddMonths(1).AddDays(-1);
                break;
            case "上月":
                var firstOfMonth = new DateTime(today.Year, today.Month, 1);
                start = firstOfMonth.AddMonths(-1);
                end = firstOfMonth.AddDays(-1);
                break;
            default:
                start = today;
                end = today;
                break;
        }

        _startDatePicker.Value = start;
        _endDatePicker.Value = end;
    }

    private static DateTime GetStartOfWeek(DateTime date, DayOfWeek firstDayOfWeek)
    {
        while (date.DayOfWeek != firstDayOfWeek)
        {
            date = date.AddDays(-1);
        }

        return date;
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

    private async Task ExecuteSearchAsync()
    {
        try
        {
            var start = _startDatePicker?.Value.Date;
            var end = _endDatePicker?.Value.Date;
            var field = _searchFieldCombo?.SelectedItem as string ?? "訂單號";
            var keyword = _keywordTextBox?.Text;

            _orders = await _service.SearchSalesOrdersAsync(start, end, field, keyword);
            BindOrdersToGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查詢失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindOrdersToGrid()
    {
        var dgv = this.Controls.Find("dgvOrders", true).FirstOrDefault() as DataGridView;
        if (dgv == null) 
            return;

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
                    await ExecuteSearchAsync();
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
            await ExecuteSearchAsync();
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
            await ExecuteSearchAsync();
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
