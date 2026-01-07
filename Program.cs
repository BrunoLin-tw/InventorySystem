using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using InventorySystem.Services;
using System.IO;

namespace InventorySystem;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // Setup DI
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        var conn = config.GetConnectionString("DefaultConnection") ?? "Data Source=inventory.db";

        services.AddDbContext<InventoryContext>(options => options.UseSqlite(conn));
        services.AddScoped<ProductService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<SupplierService>();
        services.AddScoped<OrderService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<ReportService>();
        services.AddScoped<SystemService>();
        services.AddScoped<ExcelService>();
        services.AddScoped<Form1>();

        var serviceProvider = services.BuildServiceProvider();

        // One-off maintenance action: update totals and exit
        if (args != null && args.Length > 0 && args.Contains("--update-totals"))
        {
            var orderService = serviceProvider.GetRequiredService<OrderService>();
            var updated = orderService.UpdateAllTotalsAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Updated totals for {updated} orders.");
            return;
        }

        var mainForm = serviceProvider.GetRequiredService<Form1>();
        Application.Run(mainForm);
    }    
}
