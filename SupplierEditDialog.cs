using InventorySystem.Models;
using InventorySystem.Services;
using System;
using System.Windows.Forms;

namespace InventorySystem;

public partial class SupplierEditDialog : Form
{
    private readonly SupplierService _service;
    private readonly Supplier? _supplier;

    public SupplierEditDialog(SupplierService service, Supplier? supplier)
    {
        _service = service;
        _supplier = supplier;
        InitializeComponent();
        this.Load += SupplierEditDialog_Load;
    }

    private void InitializeComponent()
    {
        // Designer placeholder - UI created in Load event
    }

    private void SupplierEditDialog_Load(object sender, EventArgs e)
    {
        this.Text = _supplier == null ? "新增供應商" : "編輯供應商";
        this.Size = new Size(400, 350);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        SetupUI();

        if (_supplier != null)
        {
            PopulateForm();
        }
    }

        private void SetupUI()
        {
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(10),
                AutoSize = true
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblTaxId = new Label { Text = "統一編號:", AutoSize = true };
            var txtTaxId = new TextBox { Dock = DockStyle.Fill, Name = "txtTaxId" };
            mainPanel.Controls.Add(lblTaxId, 0, 0);
            mainPanel.Controls.Add(txtTaxId, 1, 0);

            var lblName = new Label { Text = "名稱:", AutoSize = true };
            var txtName = new TextBox { Dock = DockStyle.Fill, Name = "txtName" };
            mainPanel.Controls.Add(lblName, 0, 1);
            mainPanel.Controls.Add(txtName, 1, 1);

            var lblEnglish = new Label { Text = "英文名稱:", AutoSize = true };
            var txtEnglish = new TextBox { Dock = DockStyle.Fill, Name = "txtEnglish" };
            mainPanel.Controls.Add(lblEnglish, 0, 2);
            mainPanel.Controls.Add(txtEnglish, 1, 2);

            var lblContact = new Label { Text = "聯絡人:", AutoSize = true };
            var txtContact = new TextBox { Dock = DockStyle.Fill, Name = "txtContact" };
            mainPanel.Controls.Add(lblContact, 0, 3);
            mainPanel.Controls.Add(txtContact, 1, 3);

            var lblEmail = new Label { Text = "Email:", AutoSize = true };
            var txtEmail = new TextBox { Dock = DockStyle.Fill, Name = "txtEmail" };
            mainPanel.Controls.Add(lblEmail, 0, 4);
            mainPanel.Controls.Add(txtEmail, 1, 4);

            var lblPhone = new Label { Text = "電話:", AutoSize = true };
            var txtPhone = new TextBox { Dock = DockStyle.Fill, Name = "txtPhone" };
            mainPanel.Controls.Add(lblPhone, 0, 5);
            mainPanel.Controls.Add(txtPhone, 1, 5);

            var lblAddress = new Label { Text = "地址:", AutoSize = true };
            var txtAddress = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60, Name = "txtAddress" };
            mainPanel.Controls.Add(lblAddress, 0, 6);
            mainPanel.Controls.Add(txtAddress, 1, 6);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var btnOK = new Button { Text = "確定", Width = 80, DialogResult = DialogResult.OK };
        btnOK.Click += async (s, e) => await SaveSupplier();

        var btnCancel = new Button { Text = "取消", Width = 80, DialogResult = DialogResult.Cancel };

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOK);

        mainPanel.Controls.Add(btnPanel, 0, 5);
        mainPanel.SetColumnSpan(btnPanel, 2);

        this.Controls.Add(mainPanel);
    }

        private void PopulateForm()
        {
            if (_supplier == null) return;

            (this.Controls.Find("txtTaxId", true).FirstOrDefault() as TextBox)!.Text = _supplier.TaxId;
            (this.Controls.Find("txtName", true).FirstOrDefault() as TextBox)!.Text = _supplier.Name;
            (this.Controls.Find("txtEnglish", true).FirstOrDefault() as TextBox)!.Text = _supplier.EnglishName ?? "";
            (this.Controls.Find("txtContact", true).FirstOrDefault() as TextBox)!.Text = _supplier.ContactName ?? "";
            (this.Controls.Find("txtEmail", true).FirstOrDefault() as TextBox)!.Text = _supplier.Email ?? "";
            (this.Controls.Find("txtPhone", true).FirstOrDefault() as TextBox)!.Text = _supplier.Phone ?? "";
            (this.Controls.Find("txtAddress", true).FirstOrDefault() as TextBox)!.Text = _supplier.Address ?? "";
        }

    private async Task SaveSupplier()
    {
        try
        {
            var taxId = (this.Controls.Find("txtTaxId", true).FirstOrDefault() as TextBox)?.Text;
            var name = (this.Controls.Find("txtName", true).FirstOrDefault() as TextBox)?.Text;
            var english = (this.Controls.Find("txtEnglish", true).FirstOrDefault() as TextBox)?.Text;
            var contact = (this.Controls.Find("txtContact", true).FirstOrDefault() as TextBox)?.Text;
            var email = (this.Controls.Find("txtEmail", true).FirstOrDefault() as TextBox)?.Text;
            var phone = (this.Controls.Find("txtPhone", true).FirstOrDefault() as TextBox)?.Text;
            var address = (this.Controls.Find("txtAddress", true).FirstOrDefault() as TextBox)?.Text;

            if (string.IsNullOrWhiteSpace(taxId))
            {
                MessageBox.Show("名稱為必填", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_supplier == null)
            {
                var newSupplier = new Supplier
                {
                    TaxId = taxId!,
                    Name = name,
                    EnglishName = english,
                    ContactName = contact,
                    Email = email,
                    Phone = phone,
                    Address = address
                };
                await _service.AddSupplierAsync(newSupplier);
            }
            else
            {
                _supplier.TaxId = taxId!;
                _supplier.Name = name;
                _supplier.EnglishName = english;
                _supplier.ContactName = contact;
                _supplier.Email = email;
                _supplier.Phone = phone;
                _supplier.Address = address;
                await _service.UpdateSupplierAsync(_supplier);
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
