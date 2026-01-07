using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.IO;

namespace InventorySystem.Services;

public class SystemService
{
    private readonly InventoryContext _context;
    private readonly string _settingsFile;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SystemService(InventoryContext context)
    {
        _context = context;
        _settingsFile = Path.Combine(AppContext.BaseDirectory, "system-settings.json");
    }

    public async Task<SystemSettings> GetSettingsAsync()
    {
        if (File.Exists(_settingsFile))
        {
            var text = await File.ReadAllTextAsync(_settingsFile);
            try
            {
                var settings = JsonSerializer.Deserialize<SystemSettings>(text, _jsonOptions);
                if (settings != null) return settings;
            }
            catch
            {
                // ignore corrupt file, fallback to defaults
            }
        }

        var defaultSettings = new SystemSettings();
        await SaveSettingsAsync(defaultSettings);
        return defaultSettings;
    }

    public async Task SaveSettingsAsync(SystemSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsFile);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var text = JsonSerializer.Serialize(settings, _jsonOptions);
        await File.WriteAllTextAsync(_settingsFile, text);
    }

    public Task<string> BackupDatabaseAsync(string destinationPath)
    {
        var dbPath = _context.Database.GetDbConnection().DataSource;
        var targetFile = Path.Combine(destinationPath, $"inventory_backup_{DateTime.Now:yyyyMMddHHmmss}.db");
        File.Copy(dbPath, targetFile, true);
        return Task.FromResult(targetFile);
    }

    public async Task RestoreBackupAsync(string backupFile)
    {
        var dbPath = _context.Database.GetDbConnection().DataSource;
        await _context.Database.CloseConnectionAsync();
        File.Copy(backupFile, dbPath, true);
    }

    public Task VacuumDatabaseAsync()
    {
        return _context.Database.ExecuteSqlRawAsync("VACUUM");
    }
}
