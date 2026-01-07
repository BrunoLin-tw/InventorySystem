using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace InventorySystem
{
    public class InventoryContextFactory : IDesignTimeDbContextFactory<InventoryContext>
    {
        public InventoryContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var conn = config.GetConnectionString("DefaultConnection") ?? "Data Source=inventory.db";

            var optionsBuilder = new DbContextOptionsBuilder<InventoryContext>();
            optionsBuilder.UseSqlite(conn);

            return new InventoryContext(optionsBuilder.Options);
        }
    }
}
