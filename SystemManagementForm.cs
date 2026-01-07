using InventorySystem.Models;
using InventorySystem.Services;
using System.Security.Cryptography;
using System.Text;

namespace InventorySystem;

public class SystemManagementForm : Form
{
    private readonly SystemService _systemService;
    private SystemSettings _settings = new();

    private readonly TabControl _tabControl = new();
    private readonly Label _backupStatusLabel = new() { AutoSize = true };
    private readonly Label _lastBackupLabel = new() { AutoSize = true };
    private readonly Label _restoreHintLabel = new() { AutoSize = true, ForeColor = Color.Gray, MaximumSize = new Size(400, 0) };
    private readonly Button _restoreButton = new() { Text = "還原備份", Width = 140, Height = 40 };
    private readonly TextBox _companyTextBox = new() { Width = 150 };
    private readonly TextBox _reportTextBox = new() { Width = 150 };
    private readonly TextBox _addressTextBox = new() { Width = 250 };
    private readonly TextBox _phoneTextBox = new() { Width = 150 };
    private readonly TextBox _taxIdTextBox = new() { Width = 150 };
    private readonly TextBox _ownerTextBox = new() { Width = 150 };
    private readonly TextBox _bankInfoTextBox = new() { Width = 250 };
    private readonly NumericUpDown _alertThresholdNumeric = new NumericUpDown { Minimum = 1, Maximum = 9999 };
    private readonly TextBox _usernameTextBox = new();
    private readonly TextBox _passwordTextBox = new() { UseSystemPasswordChar = true };
    private readonly TextBox _confirmPasswordTextBox = new() { UseSystemPasswordChar = true };
    private readonly Label _userStatusLabel = new() { AutoSize = true };
    private readonly Label _vacuumStatusLabel = new() { AutoSize = true };
    private readonly Label _lastVacuumLabel = new() { AutoSize = true };

    public SystemManagementForm(SystemService systemService)
    {
        _systemService = systemService;
        Text = "系統管理";
        Dock = DockStyle.Fill;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.White;
        InitializeComponents();
        Load += SystemManagementForm_Load;
    }

    private async void SystemManagementForm_Load(object? sender, EventArgs e)
    {
        _settings = await _systemService.GetSettingsAsync();
        _companyTextBox.Text = _settings.CompanyName;
        _reportTextBox.Text = _settings.ReportTitle;
        _addressTextBox.Text = _settings.CompanyAddress;
        _phoneTextBox.Text = _settings.CompanyPhone;
        _taxIdTextBox.Text = _settings.CompanyTaxId;
        _ownerTextBox.Text = _settings.CompanyOwner;
        _bankInfoTextBox.Text = _settings.BankInfo;
        _alertThresholdNumeric.Value = Math.Max(1, Math.Min(_alertThresholdNumeric.Maximum, _settings.InventoryAlertThreshold));
        _lastBackupLabel.Text = string.IsNullOrEmpty(_settings.LastBackupFile)
            ? "尚未建立備份"
            : $"最後備份：{_settings.LastBackup:yyyy/MM/dd HH:mm:ss}\n檔案：{_settings.LastBackupFile}";
        _lastVacuumLabel.Text = _settings.LastVacuum == default
            ? "尚未清理資料庫"
            : $"最後資料庫清理：{_settings.LastVacuum:yyyy/MM/dd HH:mm:ss}";
        _usernameTextBox.Text = _settings.AdminUsername;
    }

    private void InitializeComponents()
    {
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.TabPages.Add(CreateBackupTab());
        _tabControl.TabPages.Add(CreateParametersTab());
        _tabControl.TabPages.Add(CreateUserTab());
        _tabControl.TabPages.Add(CreateMaintenanceTab());
        Controls.Add(_tabControl);
    }

    private TabPage CreateBackupTab()
    {
        var tab = new TabPage("💾 資料備份與還原");
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };
        var backupButton = new Button { Text = "立即備份", Width = 120, Height = 40 };
        backupButton.Click += BackupButton_Click;

        _restoreButton.Click += RestoreBackupButton_Click;
        var folderButton = new Button { Text = "選擇儲存資料夾", Width = 160, Height = 40 };
        folderButton.Click += SelectFolderButton_Click;

        layout.Controls.Add(backupButton);
        layout.Controls.Add(_restoreButton);
        layout.Controls.Add(folderButton);
        layout.Controls.Add(_backupStatusLabel);
        layout.Controls.Add(_lastBackupLabel);
        _restoreHintLabel.Text = "資料還原：請選擇備份檔案後自動覆寫 inventory.db，完成後請重新啟動應用程式。";
        layout.Controls.Add(_restoreHintLabel);
        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage CreateParametersTab()
    {
        var tab = new TabPage("⚙️ 系統參數");
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, Padding = new Padding(20), AutoSize = true, ColumnCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.RowCount = 9;
        for (int i = 0; i < 9; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = "公司名稱", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(_companyTextBox, 1, 0);
        layout.Controls.Add(new Label { Text = "報表抬頭", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
        layout.Controls.Add(_reportTextBox, 1, 1);
        layout.Controls.Add(new Label { Text = "公司地址", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
        layout.Controls.Add(_addressTextBox, 1, 2);
        layout.Controls.Add(new Label { Text = "公司電話", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 3);
        layout.Controls.Add(_phoneTextBox, 1, 3);
        layout.Controls.Add(new Label { Text = "統一編號", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 4);
        layout.Controls.Add(_taxIdTextBox, 1, 4);
        layout.Controls.Add(new Label { Text = "負責人", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 5);
        layout.Controls.Add(_ownerTextBox, 1, 5);
        layout.Controls.Add(new Label { Text = "匯款帳戶", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 6);
        layout.Controls.Add(_bankInfoTextBox, 1, 6);
        layout.Controls.Add(new Label { Text = "庫存警示值", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 7);
        layout.Controls.Add(_alertThresholdNumeric, 1, 7);

        var saveButton = new Button { Text = "儲存設定", Width = 120, Height = 40, Dock = DockStyle.Left, Margin = new Padding(0, 10, 0, 0) };
        saveButton.Click += SaveParametersButton_Click;
        layout.Controls.Add(saveButton, 1, 8);

        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage CreateUserTab()
    {
        var tab = new TabPage("👤 使用者管理");
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, Padding = new Padding(20), AutoSize = true, ColumnCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowCount = 4;
        for (int i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = "管理員帳號", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(_usernameTextBox, 1, 0);
        layout.Controls.Add(new Label { Text = "新密碼", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
        layout.Controls.Add(_passwordTextBox, 1, 1);
        layout.Controls.Add(new Label { Text = "確認密碼", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
        layout.Controls.Add(_confirmPasswordTextBox, 1, 2);

        var saveButton = new Button { Text = "更新密碼", Width = 120, Height = 40, Dock = DockStyle.Left, Margin = new Padding(0, 10, 0, 0) };
        saveButton.Click += UpdateUserButton_Click;
        layout.Controls.Add(saveButton, 1, 3);
        layout.Controls.Add(_userStatusLabel, 1, 4);
        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage CreateMaintenanceTab()
    {
        var tab = new TabPage("🧹 系統維護");
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var vacuumButton = new Button { Text = "壓縮資料庫（VACUUM）", Width = 220, Height = 40 };
        vacuumButton.Click += VacuumButton_Click;
        layout.Controls.Add(vacuumButton);
        layout.Controls.Add(_vacuumStatusLabel);
        layout.Controls.Add(_lastVacuumLabel);
        tab.Controls.Add(layout);
        return tab;
    }

    private async void BackupButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() != DialogResult.OK) return;

        _backupStatusLabel.Text = "備份中...";
        var targetFile = await _systemService.BackupDatabaseAsync(dialog.SelectedPath);
        _settings.LastBackup = DateTime.Now;
        _settings.LastBackupFile = targetFile;
        await _systemService.SaveSettingsAsync(_settings);
        _backupStatusLabel.Text = "備份完成";
        _lastBackupLabel.Text = $"最後備份：{_settings.LastBackup:yyyy/MM/dd HH:mm:ss}\n檔案：{_settings.LastBackupFile}";
    }

    private async void RestoreBackupButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "SQLite 備份 (*.db)|*.db" };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        _backupStatusLabel.Text = "還原中...";
        _restoreButton.Enabled = false;
        try
        {
            await _systemService.RestoreBackupAsync(dialog.FileName);
            _settings.LastBackup = DateTime.Now;
            _settings.LastBackupFile = dialog.FileName;
            await _systemService.SaveSettingsAsync(_settings);
            _backupStatusLabel.Text = "還原完成，請重新啟動應用程式";
            _lastBackupLabel.Text = $"最後備份：{_settings.LastBackup:yyyy/MM/dd HH:mm:ss}\n檔案：{_settings.LastBackupFile}";
        }
        catch (Exception ex)
        {
            _backupStatusLabel.Text = "還原失敗";
            MessageBox.Show($"還原時發生錯誤：{ex.Message}", "還原失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _restoreButton.Enabled = true;
        }
    }

    private void SelectFolderButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.ShowDialog();
    }

    private async void SaveParametersButton_Click(object? sender, EventArgs e)
    {
        _settings.CompanyName = _companyTextBox.Text.Trim();
        _settings.ReportTitle = _reportTextBox.Text.Trim();
        _settings.CompanyAddress = _addressTextBox.Text.Trim();
        _settings.CompanyPhone = _phoneTextBox.Text.Trim();
        _settings.CompanyTaxId = _taxIdTextBox.Text.Trim();
        _settings.CompanyOwner = _ownerTextBox.Text.Trim();
        _settings.BankInfo = _bankInfoTextBox.Text.Trim();
        _settings.InventoryAlertThreshold = (int)_alertThresholdNumeric.Value;
        await _systemService.SaveSettingsAsync(_settings);
        MessageBox.Show("設定已儲存", "系統參數", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async void UpdateUserButton_Click(object? sender, EventArgs e)
    {
        if (_passwordTextBox.Text != _confirmPasswordTextBox.Text)
        {
            _userStatusLabel.Text = "密碼與確認密碼不一致";
            return;
        }

        if (string.IsNullOrWhiteSpace(_passwordTextBox.Text))
        {
            _userStatusLabel.Text = "密碼不可為空";
            return;
        }

        _settings.AdminUsername = _usernameTextBox.Text.Trim();
        _settings.AdminPasswordHash = HashPassword(_passwordTextBox.Text);
        await _systemService.SaveSettingsAsync(_settings);
        _userStatusLabel.Text = "管理員帳號已更新";
        _passwordTextBox.Clear();
        _confirmPasswordTextBox.Clear();
    }

    private async void VacuumButton_Click(object? sender, EventArgs e)
    {
        _vacuumStatusLabel.Text = "清理中...";
        await _systemService.VacuumDatabaseAsync();
        _settings.LastVacuum = DateTime.Now;
        await _systemService.SaveSettingsAsync(_settings);
        _vacuumStatusLabel.Text = "清理完成";
        _lastVacuumLabel.Text = $"最後資料庫清理：{_settings.LastVacuum:yyyy/MM/dd HH:mm:ss}";
    }

    private static string HashPassword(string raw)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hashed = sha.ComputeHash(bytes);
        return Convert.ToHexString(hashed);
    }
}
