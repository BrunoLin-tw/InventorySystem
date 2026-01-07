using InventorySystem.Models;
using InventorySystem.Services;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InventorySystem;

public partial class InventoryQueryForm : Form
{
    private readonly InventoryService _inventoryService;
    private readonly BindingSource _bindingSource = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchTextBox = new();
    private readonly Button _refreshButton = new();
    private List<InventorySummaryDto> _inventoryList = new();

    public InventoryQueryForm(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        InitializeComponent();
        Load += InventoryQueryForm_Load;
    }

    private async void InventoryQueryForm_Load(object? sender, EventArgs e)
    {
        await LoadInventoryAsync();
    }

    private async Task LoadInventoryAsync()
    {
        _inventoryList = await _inventoryService.GetInventorySummaryAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var keyword = _searchTextBox.Text?.Trim();
        var filtered = string.IsNullOrEmpty(keyword)
            ? _inventoryList
            : _inventoryList.Where(i => i.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                      || i.SKU.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        _bindingSource.DataSource = filtered;
        _grid.DataSource = _bindingSource;
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // Search bar
        _searchTextBox.PlaceholderText = "搜尋 SKU 或產品名稱";
        _searchTextBox.Dock = DockStyle.Top;
        _searchTextBox.Margin = new Padding(12);
        _searchTextBox.TextChanged += (s, e) => ApplyFilter();

        // Refresh button
        _refreshButton.Text = "重新整理";
        _refreshButton.Height = 34;
        _refreshButton.Dock = DockStyle.Top;
        _refreshButton.FlatStyle = FlatStyle.System;
        _refreshButton.Click += async (s, e) => await LoadInventoryAsync();

        // Grid
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _grid.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn { DataPropertyName = "SKU", HeaderText = "SKU" },
            new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "產品名稱" },
            new DataGridViewTextBoxColumn { DataPropertyName = "CurrentStock", HeaderText = "目前庫存" },
            new DataGridViewTextBoxColumn { DataPropertyName = "TotalPurchased", HeaderText = "總進貨" },
            new DataGridViewTextBoxColumn { DataPropertyName = "TotalSold", HeaderText = "總銷貨" }
        });

        Controls.Add(_grid);
        Controls.Add(_refreshButton);
        Controls.Add(_searchTextBox);

        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;
        this.ResumeLayout(false);
    }
}
