using InventorySystem.Services;

namespace InventorySystem;

public partial class Form1 : Form
{
    private readonly ProductService _productService;
    private readonly CustomerService _customerService;
    private readonly SupplierService _supplierService;
    private readonly OrderService _orderService;
    private readonly InventoryService _inventoryService;
    private readonly ReportService _reportService;
    private readonly SystemService _systemService;
    private readonly ExcelService _excelService;

    private SplitContainer _splitContainer = null!;
    private ListBox _menuListBox = null!;
    private Panel _contentPanel = null!;

    public Form1(ProductService productService, CustomerService customerService,
                 SupplierService supplierService, OrderService orderService,
                 InventoryService inventoryService, ReportService reportService,
                 SystemService systemService, ExcelService excelService)
    {
        _productService = productService;
        _customerService = customerService;
        _supplierService = supplierService;
        _orderService = orderService;
        _inventoryService = inventoryService;
        _reportService = reportService;
        _systemService = systemService;
        _excelService = excelService;
        InitializeComponent();
        this.Load += Form1_Load;
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        this.Text = "進銷存系統 - 主控面板";
        this.Size = new Size(1200, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("微軟正黑體", 10);

        SetupUI();
    }

    private void SetupUI()
    {
        // Main SplitContainer
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 200,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 2
        };

        // ===== Left Panel: Navigation Menu =====
        var leftPanel = _splitContainer.Panel1;
        leftPanel.Padding = new Padding(0);
        leftPanel.BackColor = Color.FromArgb(240, 244, 248);

        // Title label
        var titleLabel = new Label
        {
            Text = "功能菜單",
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("微軟正黑體", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(33, 150, 243),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(10, 0, 10, 0)
        };
        leftPanel.Controls.Add(titleLabel);

        // Menu ListBox
        _menuListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Emoji", 11),
            ItemHeight = 40,
            IntegralHeight = false,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(240, 244, 248),
            ForeColor = Color.FromArgb(50, 50, 50),
            SelectionMode = SelectionMode.One
        };

        // Add menu items
        _menuListBox.Items.AddRange(new object[]
        {
            "🏠 首頁儀表板",
            "📦 產品管理",
            "👥 客戶管理",
            "🏭 供應商管理",
            "📥 進貨管理",
            "📤 銷貨管理",
            "📊 庫存查詢",
            "📈 報表分析",
            "🛠️ 系統管理"
        });

        _menuListBox.SelectedIndex = 0;
        _menuListBox.SelectedIndexChanged += MenuListBox_SelectedIndexChanged;
        
        // Style selected item
        _menuListBox.DrawMode = DrawMode.OwnerDrawFixed;
        _menuListBox.DrawItem += MenuListBox_DrawItem;

        leftPanel.Controls.Add(_menuListBox);

        // ===== Right Panel: Content Area =====
        var rightPanel = _splitContainer.Panel2;
        rightPanel.Padding = new Padding(10);
        rightPanel.BackColor = Color.White;

        // Content container
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            AutoScroll = true
        };
        rightPanel.Controls.Add(_contentPanel);

        this.Controls.Add(_splitContainer);

        // Show default content
        ShowDashboard();
    }

    private void MenuListBox_DrawItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        // Draw background
        if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(33, 150, 243)), e.Bounds);
            e.Graphics.DrawString(_menuListBox.Items[e.Index].ToString(),
                e.Font, new SolidBrush(Color.White), e.Bounds, StringFormat.GenericDefault);
        }
        else
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(240, 244, 248)), e.Bounds);
            e.Graphics.DrawString(_menuListBox.Items[e.Index].ToString(),
                e.Font, new SolidBrush(Color.FromArgb(50, 50, 50)), e.Bounds, StringFormat.GenericDefault);
        }

        e.DrawFocusRectangle();
    }

    private void MenuListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        _contentPanel.Controls.Clear();

        switch (_menuListBox.SelectedIndex)
        {
            case 0:
                ShowDashboard();
                break;
            case 1:
                ShowProductsInPanel();
                break;
            case 2:
                ShowCustomersInPanel();
                break;
            case 3:
                ShowSuppliersInPanel();
                break;
            case 4:
                ShowPurchaseOrdersInPanel();
                break;
            case 5:
                ShowSalesOrdersInPanel();
                break;
            case 6:
                ShowInventoryQueries();
                break;
            case 7:
                ShowReports();
                break;
            case 8:
                ShowSystemManagement();
                break;
        }
    }

    private void ShowDashboard()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };

        // Header container to keep the title anchored at the very top
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.White,
            Padding = new Padding(0, 10, 0, 0)
        };

        var titleLabel = new Label
        {
            Text = "📊 首頁儀表板",
            Dock = DockStyle.Fill,
            Font = new Font("微軟正黑體", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 150, 243),
            Padding = new Padding(20, 10, 20, 10),
            AutoSize = false
        };

        headerPanel.Controls.Add(titleLabel);
        panel.Controls.Add(headerPanel);

        // Info panel
        var infoPanel = new Panel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(20, 10, 20, 20),
            BackColor = Color.White,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly
        };

        var infoLabel = new Label
        {
            Text = "歡迎使用本地進銷存系統\n\n系統功能：\n✓ 產品、客戶、供應商維護\n✓ 進貨、銷貨單據管理\n✓ 庫存查詢與報表分析\n\n請選擇左側菜單進行操作。",
            Dock = DockStyle.Fill,
            Font = new Font("微軟正黑體", 11),
            ForeColor = Color.FromArgb(80, 80, 80),
            AutoSize = true
        };

        infoPanel.Controls.Add(infoLabel);
        panel.Controls.Add(infoPanel);

        _contentPanel.Controls.Add(panel);
    }

    private void ShowProductsInPanel()
    {
        var productForm = new ProductForm(_productService);
        EmbedFormInPanel(productForm, "📦 產品管理");
    }

    private void ShowCustomersInPanel()
    {
        var customerForm = new CustomerForm(_customerService, _excelService);
        EmbedFormInPanel(customerForm, "👥 客戶管理");
    }

    private void ShowSuppliersInPanel()
    {
        var supplierForm = new SupplierForm(_supplierService, _excelService);
        EmbedFormInPanel(supplierForm, "🏭 供應商管理");
    }

    private void ShowPurchaseOrdersInPanel()
    {
        var purchaseForm = new PurchaseOrderForm(_orderService, _excelService);
        EmbedFormInPanel(purchaseForm, "📥 進貨管理");
    }

    private void ShowSalesOrdersInPanel()
    {
        var salesForm = new SalesOrderForm(_orderService, _excelService, _systemService);
        EmbedFormInPanel(salesForm, "📤 銷貨管理");
    }

    private void ShowInventoryQueries()
    {
        var inventoryForm = new InventoryQueryForm(_inventoryService);
        EmbedFormInPanel(inventoryForm, "📊 庫存查詢");
    }

    private void ShowReports()
    {
        var reportForm = new ReportForm(_reportService);
        EmbedFormInPanel(reportForm, "📈 報表分析");
    }

    private void ShowSystemManagement()
    {
        var systemForm = new SystemManagementForm(_systemService);
        EmbedFormInPanel(systemForm, "🛠️ 系統管理");
    }

    private void EmbedFormInPanel(Form form, string title)
    {
        // Convert Form to borderless, embedded control
        form.FormBorderStyle = FormBorderStyle.None;
        form.TopLevel = false;
        form.Dock = DockStyle.Fill;
        form.BackColor = Color.White;

        // Create a container so the title/header doesn't overlap the embedded form
        var container = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            AutoScroll = true
        };

        // Header/title at top of container
        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 50,
            Font = new Font("微軟正黑體", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 150, 243),
            Padding = new Padding(20, 10, 20, 10),
            AutoSize = false
        };

        container.Controls.Add(form);
        container.Controls.Add(titleLabel);
        _contentPanel.Controls.Add(container);
        form.Show();
    }
}

