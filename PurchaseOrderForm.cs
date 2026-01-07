using InventorySystem.Models;
using InventorySystem.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InventorySystem;

public partial class PurchaseOrderForm : Form
{
    private readonly OrderService _service;
    private readonly ExcelService _excelService;
    private List<PurchaseOrder> _orders = new();

    public PurchaseOrderForm(OrderService service, ExcelService excelService)
    {
        _service = service;
        _excelService = excelService;
        InitializeComponent();
        this.Load += PurchaseOrderForm_Load;
    }

    private void InitializeComponent()
    {
        // Designer placeholder - UI created in Load event
    }

    private async void PurchaseOrderForm_Load(object sender, EventArgs e)
    {
        this.Text = "進貨管理";
        this.Size = new Size(900, 650);
        this.StartPosition = FormStartPosition.CenterParent;
        SetupUI();
        await LoadOrders();
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
        btnAdd.Click += (s, e) => OpenPurchaseOrderDialog(null);

        var btnDelete = new Button { Text = "刪除", Width = 80 };
        btnDelete.Click += (s, e) => DeleteSelectedOrder();

        var btnRefresh = new Button { Text = "刷新", Width = 80 };
        btnRefresh.Click += async (s, e) => await LoadOrders();

        var btnImport = new Button { Text = "匯入 Excel", Width = 110 };
        btnImport.Click += async (s, e) => await ImportExcelAsync();

        var btnExport = new Button { Text = "匯出 Excel", Width = 110 };
        btnExport.Click += async (s, e) => await ExportExcelAsync();

        btnPanel.Controls.Add(btnAdd);
        btnPanel.Controls.Add(btnDelete);
        btnPanel.Controls.Add(btnRefresh);
        btnPanel.Controls.Add(btnImport);
        btnPanel.Controls.Add(btnExport);

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
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "供應商", Width = 200 });
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

    private async Task LoadOrders()
    {
        try
        {
            _orders = await _service.GetAllPurchaseOrdersAsync();
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
                        SupplierName = o.Supplier?.Name ?? "無",
                        o.OrderDate,
                        Quantity = totalQuantity,
                        UnitPrice = averageUnitPrice,
                        Total = totalAmount
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
                    await _service.DeletePurchaseOrderAsync(order.Id);
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

    private async void OpenPurchaseOrderDialog(PurchaseOrder? order)
    {
        var dialog = new PurchaseOrderEditDialog(_service, order);
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

        OpenPurchaseOrderDialog(order);
    }

    private async Task ImportExcelAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
            Title = "選擇要匯入的進貨資料"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var result = await _excelService.ImportPurchasesAsync(dialog.FileName);
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
            Title = "儲存進貨報表"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            await _excelService.ExportPurchasesAsync(dialog.FileName);
            MessageBox.Show("匯出完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"匯出失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
