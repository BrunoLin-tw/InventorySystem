using InventorySystem.Models;
using InventorySystem.Services;
using System;
using System.Windows.Forms;

namespace InventorySystem;

public partial class ProductEditDialog : Form
{
    private readonly ProductService _service;
    private readonly Product? _product;
    private List<Supplier> _suppliers = new();

    public ProductEditDialog(ProductService service, Product? product)
    {
        _service = service;
        _product = product;
        InitializeComponent();
        this.Load += ProductEditDialog_Load;
    }

    private void InitializeComponent()
    {
        // Designer placeholder - UI created in Load event
    }

    private async void ProductEditDialog_Load(object sender, EventArgs e)
    {
        this.Text = _product == null ? "新增產品" : "編輯產品";
        this.Size = new Size(400, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        await LoadSuppliers();
        SetupUI();

        if (_product != null)
        {
            PopulateForm();
        }
    }

    private async Task LoadSuppliers()
    {
        try
        {
            _suppliers = await _service.GetAllSuppliersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入供應商失敗: {ex.Message}", "錯誤");
        }
    }

    private void SetupUI()
    {
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(10),
            AutoSize = true
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // SKU
        var lblSKU = new Label { Text = "SKU:", AutoSize = true };
        var txtSKU = new TextBox { Dock = DockStyle.Fill, Name = "txtSKU" };
        mainPanel.Controls.Add(lblSKU, 0, 0);
        mainPanel.Controls.Add(txtSKU, 1, 0);

        // Name
        var lblName = new Label { Text = "名稱:", AutoSize = true };
        var txtName = new TextBox { Dock = DockStyle.Fill, Name = "txtName" };
        mainPanel.Controls.Add(lblName, 0, 1);
        mainPanel.Controls.Add(txtName, 1, 1);

        // Description
        var lblDesc = new Label { Text = "描述:", AutoSize = true };
        var txtDesc = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60, Name = "txtDesc" };
        mainPanel.Controls.Add(lblDesc, 0, 2);
        mainPanel.Controls.Add(txtDesc, 1, 2);

        // UnitPrice
        var lblPrice = new Label { Text = "單價:", AutoSize = true };
        var txtPrice = new TextBox { Dock = DockStyle.Fill, Name = "txtPrice" };
        mainPanel.Controls.Add(lblPrice, 0, 3);
        mainPanel.Controls.Add(txtPrice, 1, 3);

        // QuantityOnHand
        var lblQty = new Label { Text = "庫存:", AutoSize = true };
        var txtQty = new TextBox { Dock = DockStyle.Fill, Name = "txtQty" };
        mainPanel.Controls.Add(lblQty, 0, 4);
        mainPanel.Controls.Add(txtQty, 1, 4);

        // Supplier
        var lblSupplier = new Label { Text = "供應商:", AutoSize = true };
        var cboSupplier = new ComboBox
        {
            Dock = DockStyle.Fill,
            Name = "cboSupplier",
            DropDownStyle = ComboBoxStyle.DropDown
        };
        cboSupplier.Items.Add("(無)");
        foreach (var sup in _suppliers)
        {
            cboSupplier.Items.Add(sup.Name);
        }
        mainPanel.Controls.Add(lblSupplier, 0, 5);
        mainPanel.Controls.Add(cboSupplier, 1, 5);

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var btnOK = new Button { Text = "確定", Width = 80, DialogResult = DialogResult.OK };
        btnOK.Click += async (s, e) => await SaveProduct();

        var btnCancel = new Button { Text = "取消", Width = 80, DialogResult = DialogResult.Cancel };

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOK);

        mainPanel.Controls.Add(btnPanel, 0, 6);
        mainPanel.SetColumnSpan(btnPanel, 2);

        this.Controls.Add(mainPanel);
    }

    private void PopulateForm()
    {
        if (_product == null) return;

        (this.Controls.Find("txtSKU", true).FirstOrDefault() as TextBox)!.Text = _product.SKU;
        (this.Controls.Find("txtName", true).FirstOrDefault() as TextBox)!.Text = _product.Name;
        (this.Controls.Find("txtDesc", true).FirstOrDefault() as TextBox)!.Text = _product.Description ?? "";
        (this.Controls.Find("txtPrice", true).FirstOrDefault() as TextBox)!.Text = _product.UnitPrice.ToString("F2");
        (this.Controls.Find("txtQty", true).FirstOrDefault() as TextBox)!.Text = _product.QuantityOnHand.ToString();

        var cboSupplier = this.Controls.Find("cboSupplier", true).FirstOrDefault() as ComboBox;
        if (cboSupplier != null)
        {
            if (_product.SupplierId.HasValue && _product.Supplier != null)
            {
                cboSupplier.SelectedItem = _product.Supplier.Name;
            }
            else
            {
                cboSupplier.SelectedIndex = 0;
            }
        }
    }

    private async Task SaveProduct()
    {
        try
        {
            var sku = (this.Controls.Find("txtSKU", true).FirstOrDefault() as TextBox)?.Text;
            var name = (this.Controls.Find("txtName", true).FirstOrDefault() as TextBox)?.Text;
            var desc = (this.Controls.Find("txtDesc", true).FirstOrDefault() as TextBox)?.Text;
            var priceStr = (this.Controls.Find("txtPrice", true).FirstOrDefault() as TextBox)?.Text;
            var qtyStr = (this.Controls.Find("txtQty", true).FirstOrDefault() as TextBox)?.Text;
            var cboSupplier = this.Controls.Find("cboSupplier", true).FirstOrDefault() as ComboBox;

            if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("SKU 和名稱為必填", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(priceStr, out var price))
            {
                MessageBox.Show("單價必須是數字", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(qtyStr, out var qty))
            {
                MessageBox.Show("庫存必須是整數", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? supplierId = null;
            if (cboSupplier?.SelectedItem != null && cboSupplier.SelectedIndex > 0)
            {
                var selectedName = cboSupplier.SelectedItem.ToString();
                var supplier = _suppliers.FirstOrDefault(s => s.Name == selectedName);
                supplierId = supplier?.Id;
            }

            if (_product == null)
            {
                var newProduct = new Product
                {
                    SKU = sku,
                    Name = name,
                    Description = desc,
                    UnitPrice = price,
                    QuantityOnHand = qty,
                    SupplierId = supplierId
                };
                await _service.AddProductAsync(newProduct);
            }
            else
            {
                _product.SKU = sku;
                _product.Name = name;
                _product.Description = desc;
                _product.UnitPrice = price;
                _product.QuantityOnHand = qty;
                _product.SupplierId = supplierId;
                await _service.UpdateProductAsync(_product);
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
