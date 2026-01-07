using InventorySystem.Models;
using InventorySystem.Services;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace InventorySystem;

public class ReportForm : Form
{
    private readonly ReportService _reportService;
    private readonly DataGridView _inventoryGrid;
    private readonly DataGridView _productRankGrid;
    private readonly DataGridView _monthlyGrid;
    private readonly DataGridView _customerGrid;
    private readonly ComboBox _yearFilterCombo;
    private readonly Button _inventoryRefreshButton;
    private readonly Button _productRankRefreshButton;
    private readonly Button _monthlyRefreshButton;
    private readonly Button _customerRefreshButton;
    private bool _isPopulatingYearFilter;

    public ReportForm(ReportService reportService)
    {
        _reportService = reportService;

        _inventoryGrid = CreateGrid();
        _productRankGrid = CreateGrid();
        _monthlyGrid = CreateGrid();
        _customerGrid = CreateGrid();

        _inventoryRefreshButton = new Button { Text = "更新", AutoSize = true, Padding = new Padding(12, 6, 12, 6) };
        _productRankRefreshButton = new Button { Text = "重新整理", AutoSize = true, Padding = new Padding(12, 6, 12, 6) };
        _monthlyRefreshButton = new Button { Text = "重新整理", AutoSize = true, Padding = new Padding(12, 6, 12, 6) };
        _customerRefreshButton = new Button { Text = "重新整理", AutoSize = true, Padding = new Padding(12, 6, 12, 6) };
        _yearFilterCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140
        };

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "報表分析面板";
        Font = new Font("微軟正黑體", 10);
        Size = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterParent;

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("微軟正黑體", 11)
        };

        tabControl.TabPages.Add(CreateInventoryTab());
        tabControl.TabPages.Add(CreateProductSalesTab());
        tabControl.TabPages.Add(CreateMonthlySalesTab());
        tabControl.TabPages.Add(CreateCustomerContributionTab());

        Controls.Add(tabControl);

        Load += async (_, _) => await LoadAllReportsAsync();
    }

    private TabPage CreateInventoryTab()
    {
        var tab = new TabPage("庫存狀況與價值報表");
        var topPanel = CreateTopPanel();
        topPanel.Controls.Add(new Label { Text = "顯示目前庫存數量與評估價值", AutoSize = true, Margin = new Padding(0, 8, 15, 0) });
        topPanel.Controls.Add(_inventoryRefreshButton);
        _inventoryRefreshButton.Click += async (_, _) => await LoadInventoryValueAsync();

        SetupTabLayout(tab, topPanel, _inventoryGrid);
        return tab;
    }

    private TabPage CreateProductSalesTab()
    {
        var tab = new TabPage("產品銷售排行報表");
        var topPanel = CreateTopPanel();
        topPanel.Controls.Add(new Label { Text = "依銷售數量排序，預設 Top 20", AutoSize = true, Margin = new Padding(0, 8, 15, 0) });
        topPanel.Controls.Add(_productRankRefreshButton);
        _productRankRefreshButton.Click += async (_, _) => await LoadProductSalesRankAsync();

        SetupTabLayout(tab, topPanel, _productRankGrid);
        return tab;
    }

    private TabPage CreateMonthlySalesTab()
    {
        var tab = new TabPage("月度銷售趨勢");
        var topPanel = CreateTopPanel();
        topPanel.Controls.Add(new Label { Text = "依年月顯示訂單數與營收", AutoSize = true, Margin = new Padding(0, 8, 15, 0) });
        topPanel.Controls.Add(new Label { Text = "年度：", AutoSize = true, Margin = new Padding(0, 8, 0, 0) });
        topPanel.Controls.Add(_yearFilterCombo);
        topPanel.Controls.Add(_monthlyRefreshButton);

        _yearFilterCombo.SelectedIndexChanged += async (_, _) => await RefreshMonthlyWithFilterAsync();
        _monthlyRefreshButton.Click += async (_, _) => await LoadMonthlySalesAsync();

        SetupTabLayout(tab, topPanel, _monthlyGrid);
        return tab;
    }

    private TabPage CreateCustomerContributionTab()
    {
        var tab = new TabPage("客戶貢獻度分析");
        var topPanel = CreateTopPanel();
        topPanel.Controls.Add(new Label { Text = "依營收排序的前 20 名客戶", AutoSize = true, Margin = new Padding(0, 8, 15, 0) });
        topPanel.Controls.Add(_customerRefreshButton);
        _customerRefreshButton.Click += async (_, _) => await LoadCustomerContributionAsync();

        SetupTabLayout(tab, topPanel, _customerGrid);
        return tab;
    }

    private FlowLayoutPanel CreateTopPanel()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(12, 6, 12, 6),
            WrapContents = false,
            AutoSize = true
        };
    }

    private void SetupTabLayout(TabPage tab, Control topPanel, Control grid)
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ColumnCount = 1,
            RowCount = 2
        };

        // Reserve auto height for the header panel and rest for the grid
        container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        grid.Dock = DockStyle.Fill;
        container.Controls.Add(topPanel, 0, 0);
        container.Controls.Add(grid, 0, 1);

        tab.Controls.Add(container);
    }

    private static DataGridView CreateGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoGenerateColumns = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Color.White
        };
    }

    private async Task LoadAllReportsAsync()
    {
        var tasks = new List<Task>
        {
            LoadInventoryValueAsync(),
            LoadProductSalesRankAsync(),
            InitializeMonthlyTabAsync(),
            LoadCustomerContributionAsync()
        };

        await Task.WhenAll(tasks);
    }

    private async Task LoadInventoryValueAsync()
    {
        var data = await _reportService.GetInventoryValuationAsync();
        _inventoryGrid.DataSource = data;
    }

    private async Task LoadProductSalesRankAsync()
    {
        var data = await _reportService.GetProductSalesRankingAsync();
        _productRankGrid.DataSource = data;
    }

    private async Task InitializeMonthlyTabAsync()
    {
        var data = await _reportService.GetMonthlySalesTrendAsync();
        _monthlyGrid.DataSource = data;
        PopulateYearFilter(data);
        await LoadMonthlySalesAsync();
    }

    private async Task LoadMonthlySalesAsync()
    {
        int? yearFilter = null;
        if (_yearFilterCombo.SelectedItem is int year)
        {
            yearFilter = year;
        }

        var data = await _reportService.GetMonthlySalesTrendAsync(yearFilter);
        _monthlyGrid.DataSource = data;
    }

    private async Task RefreshMonthlyWithFilterAsync()
    {
        if (_isPopulatingYearFilter)
        {
            return;
        }

        await LoadMonthlySalesAsync();
    }

    private void PopulateYearFilter(IEnumerable<MonthlySalesDto> data)
    {
        _isPopulatingYearFilter = true;
        _yearFilterCombo.Items.Clear();
        _yearFilterCombo.Items.Add("全部");
        foreach (var year in data.Select(x => x.Year).Distinct().OrderByDescending(x => x))
        {
            _yearFilterCombo.Items.Add(year);
        }

        if (_yearFilterCombo.Items.Count > 0)
        {
            _yearFilterCombo.SelectedIndex = 0;
        }

        _isPopulatingYearFilter = false;
    }

    private async Task LoadCustomerContributionAsync()
    {
        var data = await _reportService.GetCustomerContributionAsync();
        _customerGrid.DataSource = data;
    }
}
