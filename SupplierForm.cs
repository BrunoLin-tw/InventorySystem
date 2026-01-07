using InventorySystem.Models;
using InventorySystem.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InventorySystem;

public partial class SupplierForm : Form
{
    private readonly SupplierService _service;
    private readonly ExcelService _excelService;
    private List<Supplier> _suppliers = new();

    public SupplierForm(SupplierService service, ExcelService excelService)
    {
        _service = service;
        _excelService = excelService;
        InitializeComponent();
        this.Load += SupplierForm_Load;
    }

    private void InitializeComponent()
    {
        // Designer placeholder - UI created in Load event
    }

    private async void SupplierForm_Load(object sender, EventArgs e)
    {
        this.Text = "供應商管理";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        SetupUI();
        await LoadSuppliers();
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

        var btnAdd = new Button { Text = "新增", Width = 80 };
        btnAdd.Click += (s, e) => OpenSupplierDialog(null);

        var btnEdit = new Button { Text = "編輯", Width = 80 };
        btnEdit.Click += (s, e) => EditSelectedSupplier();

        var btnDelete = new Button { Text = "刪除", Width = 80 };
        btnDelete.Click += (s, e) => DeleteSelectedSupplier();

        var btnRefresh = new Button { Text = "刷新", Width = 80 };
        btnRefresh.Click += async (s, e) => await LoadSuppliers();

        var btnImport = new Button { Text = "匯入 Excel", Width = 100 };
        btnImport.Click += async (s, e) => await ImportExcelAsync();

        var btnExport = new Button { Text = "匯出 Excel", Width = 100 };
        btnExport.Click += async (s, e) => await ExportExcelAsync();

        btnPanel.Controls.Add(btnAdd);
        btnPanel.Controls.Add(btnEdit);
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
            Name = "dgvSuppliers"
        };

        // Selection behaviour: select full row and only one row at a time
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;

        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TaxId", HeaderText = "統一編號", Width = 120 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "名稱", Width = 200 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EnglishName", HeaderText = "英文名稱", Width = 200 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ContactName", HeaderText = "聯絡人", Width = 150 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Email", HeaderText = "Email", Width = 150 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phone", HeaderText = "電話", Width = 120 });

        mainPanel.Controls.Add(btnPanel, 0, 0);
        mainPanel.Controls.Add(dgv, 0, 1);

        this.Controls.Add(mainPanel);
    }

    private async Task LoadSuppliers()
    {
        try
        {
            _suppliers = await _service.GetAllSuppliersAsync();
            var dgv = this.Controls.Find("dgvSuppliers", true).FirstOrDefault() as DataGridView;
            if (dgv != null)
            {
                dgv.DataSource = _suppliers;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EditSelectedSupplier()
    {
        var dgv = this.Controls.Find("dgvSuppliers", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0)
        {
            var rowIndex = dgv.SelectedRows[0].Index;
            var supplier = _suppliers[rowIndex];
            OpenSupplierDialog(supplier);
        }
        else
        {
            MessageBox.Show("請選擇一個供應商", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void DeleteSelectedSupplier()
    {
        var dgv = this.Controls.Find("dgvSuppliers", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0)
        {
            var rowIndex = dgv.SelectedRows[0].Index;
            var supplier = _suppliers[rowIndex];
            if (MessageBox.Show($"確定刪除 {supplier.Name} 嗎？", "確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    await _service.DeleteSupplierAsync(supplier.Id);
                    await LoadSuppliers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"刪除失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("請選擇一個供應商", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void OpenSupplierDialog(Supplier? supplier)
    {
        var dialog = new SupplierEditDialog(_service, supplier);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadSuppliers();
        }
    }

    private async Task ImportExcelAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
            Title = "選擇要匯入的 Excel 檔案"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var result = await _excelService.ImportSuppliersAsync(dialog.FileName);
            var message = $"新增 {result.Added} 筆，更新 {result.Updated} 筆";
            if (result.Errors.Any())
            {
                message += $"\n發現錯誤：\n{string.Join("\n", result.Errors)}";
            }

            MessageBox.Show(message, "匯入完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadSuppliers();
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
            Title = "儲存 Excel 檔案"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            await _excelService.ExportSuppliersAsync(dialog.FileName);
            MessageBox.Show("匯出完成", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"匯出失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
