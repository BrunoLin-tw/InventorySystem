using InventorySystem.Models;
using InventorySystem.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InventorySystem;

public partial class ProductForm : Form
{
    private readonly ProductService _service;
    private List<Product> _products = new();

    public ProductForm(ProductService service)
    {
        _service = service;
        InitializeComponent();
        this.Load += ProductForm_Load;
    }

    private void InitializeComponent()
    {
        // Designer placeholder - UI created in Load event
    }

    private async void ProductForm_Load(object sender, EventArgs e)
    {
        this.Text = "產品管理";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        SetupUI();
        await LoadProducts();
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

        // Top button panel
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true
        };

        var btnAdd = new Button { Text = "新增", Width = 80 };
        btnAdd.Click += (s, e) => OpenProductDialog(null);

        var btnEdit = new Button { Text = "編輯", Width = 80 };
        btnEdit.Click += (s, e) => EditSelectedProduct();

        var btnDelete = new Button { Text = "刪除", Width = 80 };
        btnDelete.Click += (s, e) => DeleteSelectedProduct();

        var btnRefresh = new Button { Text = "刷新", Width = 80 };
        btnRefresh.Click += async (s, e) => await LoadProducts();

        btnPanel.Controls.Add(btnAdd);
        btnPanel.Controls.Add(btnEdit);
        btnPanel.Controls.Add(btnDelete);
        btnPanel.Controls.Add(btnRefresh);

        // DataGridView
        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            Name = "dgvProducts"
        };

        // Selection behaviour: select full row and only one row at a time
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;

        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Width = 50 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SKU", HeaderText = "SKU", Width = 100 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "產品名稱", Width = 200 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice", HeaderText = "單價", Width = 100 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "QuantityOnHand", HeaderText = "庫存", Width = 80 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "供應商", Width = 150 });

        mainPanel.Controls.Add(btnPanel, 0, 0);
        mainPanel.Controls.Add(dgv, 0, 1);

        this.Controls.Add(mainPanel);
    }

    private async Task LoadProducts()
    {
        try
        {
            _products = await _service.GetAllProductsAsync();
            var dgv = this.Controls.Find("dgvProducts", true).FirstOrDefault() as DataGridView;
            if (dgv != null)
            {
                dgv.DataSource = _products.Select(p => new
                {
                    p.Id,
                    p.SKU,
                    p.Name,
                    p.UnitPrice,
                    p.QuantityOnHand,
                    SupplierName = p.Supplier?.Name ?? "無"
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EditSelectedProduct()
    {
        var dgv = this.Controls.Find("dgvProducts", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0)
        {
            var rowIndex = dgv.SelectedRows[0].Index;
            var product = _products[rowIndex];
            OpenProductDialog(product);
        }
        else
        {
            MessageBox.Show("請選擇一個產品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void DeleteSelectedProduct()
    {
        var dgv = this.Controls.Find("dgvProducts", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0)
        {
            var rowIndex = dgv.SelectedRows[0].Index;
            var product = _products[rowIndex];
            if (MessageBox.Show($"確定刪除 {product.Name} 嗎？", "確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    await _service.DeleteProductAsync(product.Id);
                    await LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"刪除失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("請選擇一個產品", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void OpenProductDialog(Product? product)
    {
        var dialog = new ProductEditDialog(_service, product);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadProducts();
        }
    }
}
