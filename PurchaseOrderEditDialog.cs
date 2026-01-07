using InventorySystem.Models;
using InventorySystem.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InventorySystem;

public partial class PurchaseOrderEditDialog : Form
{
    private readonly OrderService _service;
    private readonly PurchaseOrder? _order;
    private List<Supplier> _suppliers = new();
    private List<Product> _products = new();
    private List<PurchaseOrderItem> _items = new();

    public PurchaseOrderEditDialog(OrderService service, PurchaseOrder? order)
    {
        _service = service;
        _order = order;
        InitializeComponent();
        this.Load += PurchaseOrderEditDialog_Load;
    }

    private void InitializeComponent()
    {
        // Designer placeholder - UI created in Load event
    }

    private async void PurchaseOrderEditDialog_Load(object sender, EventArgs e)
    {
        this.Text = _order == null ? "新增進貨單" : "編輯進貨單";
        this.Size = new Size(900, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        await LoadSuppliersAndProducts();
        SetupUI();

        if (_order != null)
        {
            PopulateForm();
        }
    }

    private async Task LoadSuppliersAndProducts()
    {
        try
        {
            _suppliers = await _service.GetAllSuppliersAsync();
            _products = await _service.GetAllProductsAsync();
            if (_order != null)
            {
                _items = new List<PurchaseOrderItem>(_order.Items);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗: {ex.Message}", "錯誤");
        }
    }

    private void SetupUI()
    {
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10)
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Row sizing: labels rows auto-size, items row fills remaining space, buttons row auto-sizes
        mainPanel.RowStyles.Clear();
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // row 0
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // row 1
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // row 2
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // items row
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // buttons row

        // Order Number
        var lblOrder = new Label { Text = "訂單號:", AutoSize = true };
        var txtOrder = new TextBox { Dock = DockStyle.Fill, Name = "txtOrderNumber" };
        mainPanel.Controls.Add(lblOrder, 0, 0);
        mainPanel.Controls.Add(txtOrder, 1, 0);

        // Supplier
        var lblSupplier = new Label { Text = "供應商:", AutoSize = true };
        var cboSupplier = new ComboBox { Dock = DockStyle.Fill, Name = "cboSupplier", DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var sup in _suppliers)
        {
            cboSupplier.Items.Add($"{sup.Id}|{sup.Name}");
        }
        mainPanel.Controls.Add(lblSupplier, 0, 1);
        mainPanel.Controls.Add(cboSupplier, 1, 1);

        // Order Date
        var lblDate = new Label { Text = "日期:", AutoSize = true };
        var dtpDate = new DateTimePicker { Dock = DockStyle.Fill, Name = "dtpOrderDate", Format = DateTimePickerFormat.Short };
        mainPanel.Controls.Add(lblDate, 0, 2);
        mainPanel.Controls.Add(dtpDate, 1, 2);

        // Items GridView
        var lblItems = new Label { Text = "項目:", AutoSize = true };
        mainPanel.Controls.Add(lblItems, 0, 3);

        var dgvItems = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            Name = "dgvItems",
            Height = 200,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true
        };

        var prodCol = new DataGridViewComboBoxColumn
        {
            HeaderText = "產品",
            Name = "ProductId",
            DataPropertyName = "ProductId",
            Width = 250,
            ValueType = typeof(int)
        };

        // Populate combo with product list (Id as value, Name as display)
        prodCol.DataSource = _products;
        prodCol.ValueMember = "Id";
        prodCol.DisplayMember = "Name";
        dgvItems.Columns.Add(prodCol);
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "數量", Name = "Quantity", DataPropertyName = "Quantity", Width = 120 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "單價", Name = "UnitPrice", DataPropertyName = "UnitPrice", Width = 150 });

        // Commit combo changes immediately so CellValueChanged fires
        dgvItems.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (dgvItems.IsCurrentCellDirty && dgvItems.CurrentCell?.OwningColumn?.Name == "ProductId")
                dgvItems.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        // When product selection changes, fill default UnitPrice
        dgvItems.CellValueChanged += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            var col = dgvItems.Columns[e.ColumnIndex];
            if (col.Name == "ProductId")
            {
                var cell = dgvItems.Rows[e.RowIndex].Cells["ProductId"];
                var val = cell.Value;
                if (val != null && int.TryParse(val.ToString(), out var pid))
                {
                    var prod = _products.FirstOrDefault(p => p.Id == pid);
                    if (prod != null)
                    {
                        dgvItems.Rows[e.RowIndex].Cells["UnitPrice"].Value = prod.UnitPrice;
                    }
                }
            }
        };

        // Validate Quantity and UnitPrice on edit
        dgvItems.CellValidating += (s, e) =>
        {
            var col = dgvItems.Columns[e.ColumnIndex];
            var formatted = e.FormattedValue?.ToString() ?? string.Empty;
            if (col.Name == "Quantity")
            {
                if (!int.TryParse(formatted, out var q) || q < 0)
                {
                    MessageBox.Show("數量必須為非負整數", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
            else if (col.Name == "UnitPrice")
            {
                if (!decimal.TryParse(formatted, out var up) || up < 0)
                {
                    MessageBox.Show("單價必須為非負數字", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
        };

        // Place dgvItems to span both columns so it uses the full dialog width
        mainPanel.Controls.Add(dgvItems, 0, 3);
        mainPanel.SetColumnSpan(dgvItems, 2);

        // If editing existing order, bind existing items to the grid so product selection shows correctly
        if (_items != null && _items.Count > 0)
        {
            dgvItems.DataSource = new BindingList<PurchaseOrderItem>(_items);
        }

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var btnOK = new Button { Text = "確定", Width = 80, DialogResult = DialogResult.OK };
        btnOK.Click += async (s, e) => await SaveOrder();

        var btnCancel = new Button { Text = "取消", Width = 80, DialogResult = DialogResult.Cancel };

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOK);

        // Move buttons to their own bottom row
        mainPanel.Controls.Add(btnPanel, 0, 4);
        mainPanel.SetColumnSpan(btnPanel, 2);

        this.Controls.Add(mainPanel);
    }

    private void PopulateForm()
    {
        if (_order == null) return;

        (this.Controls.Find("txtOrderNumber", true).FirstOrDefault() as TextBox)!.Text = _order.OrderNumber;
        var cboSupplier = this.Controls.Find("cboSupplier", true).FirstOrDefault() as ComboBox;
        if (cboSupplier != null)
        {
            cboSupplier.SelectedItem = $"{_order.SupplierId}|{_order.Supplier?.Name}";
        }

        (this.Controls.Find("dtpOrderDate", true).FirstOrDefault() as DateTimePicker)!.Value = _order.OrderDate;
    }

    private async Task SaveOrder()
    {
        try
        {
            var orderNum = (this.Controls.Find("txtOrderNumber", true).FirstOrDefault() as TextBox)?.Text;
            var cboSupplier = this.Controls.Find("cboSupplier", true).FirstOrDefault() as ComboBox;
            var dtpDate = this.Controls.Find("dtpOrderDate", true).FirstOrDefault() as DateTimePicker;

            if (string.IsNullOrWhiteSpace(orderNum))
            {
                MessageBox.Show("訂單號為必填", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboSupplier?.SelectedItem == null)
            {
                MessageBox.Show("請選擇供應商", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var supplierIdStr = cboSupplier.SelectedItem.ToString()?.Split('|')[0];
            if (!int.TryParse(supplierIdStr, out var supplierId))
            {
                MessageBox.Show("供應商選擇錯誤", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_order == null)
            {
                var newOrder = new PurchaseOrder
                {
                    OrderNumber = orderNum,
                    SupplierId = supplierId,
                    OrderDate = dtpDate?.Value ?? DateTime.Now,
                    Total = 0
                };
                // Collect items from grid
                var dgv = this.Controls.Find("dgvItems", true).FirstOrDefault() as DataGridView;
                var items = new List<PurchaseOrderItem>();
                if (dgv != null)
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.IsNewRow) continue;
                        var prodVal = row.Cells["ProductId"].Value;
                        if (prodVal == null) continue;
                        if (!int.TryParse(prodVal.ToString(), out var pid)) continue;
                        var qtyObj = row.Cells["Quantity"].Value;
                        var priceObj = row.Cells["UnitPrice"].Value;
                        if (!int.TryParse(Convert.ToString(qtyObj ?? "0"), out var qty)) qty = 0;
                        if (!decimal.TryParse(Convert.ToString(priceObj ?? "0"), out var up)) up = 0m;
                        items.Add(new PurchaseOrderItem { ProductId = pid, Quantity = qty, UnitPrice = up });
                    }
                }
                newOrder.Items = items;
                newOrder.Total = items.Sum(i => i.Quantity * i.UnitPrice);
                await _service.AddPurchaseOrderAsync(newOrder);
            }
            else
            {
                // Update existing order and its items
                var existing = await _service.GetPurchaseOrderByIdAsync(_order.Id);
                if (existing != null)
                {
                    existing.OrderNumber = orderNum;
                    existing.SupplierId = supplierId;
                    existing.OrderDate = dtpDate?.Value ?? DateTime.Now;

                    // Rebuild items from grid
                    var dgv = this.Controls.Find("dgvItems", true).FirstOrDefault() as DataGridView;
                    var newItems = new List<PurchaseOrderItem>();
                    if (dgv != null)
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            if (row.IsNewRow) continue;
                            var prodVal = row.Cells["ProductId"].Value;
                            if (prodVal == null) continue;
                            if (!int.TryParse(prodVal.ToString(), out var pid)) continue;
                            var qtyObj = row.Cells["Quantity"].Value;
                            var priceObj = row.Cells["UnitPrice"].Value;
                            if (!int.TryParse(Convert.ToString(qtyObj ?? "0"), out var qty)) qty = 0;
                            if (!decimal.TryParse(Convert.ToString(priceObj ?? "0"), out var up)) up = 0m;
                            newItems.Add(new PurchaseOrderItem { ProductId = pid, Quantity = qty, UnitPrice = up });
                        }
                    }

                    // Replace items
                    existing.Items.Clear();
                    foreach (var it in newItems)
                        existing.Items.Add(it);

                    existing.Total = existing.Items.Sum(i => i.Quantity * i.UnitPrice);
                    await _service.UpdatePurchaseOrderAsync(existing);
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失敗: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
