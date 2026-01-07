using Microsoft.EntityFrameworkCore;
using InventorySystem.Models;

namespace InventorySystem
{
    public class InventoryContext : DbContext
    {
        public InventoryContext(DbContextOptions<InventoryContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;
        public DbSet<SalesOrder> SalesOrders { get; set; } = null!;
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; } = null!;
        public DbSet<InventoryLog> InventoryLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Supplier>().ToTable("Suppliers");
            modelBuilder.Entity<PurchaseOrder>().ToTable("PurchaseOrders");
            modelBuilder.Entity<PurchaseOrderItem>().ToTable("PurchaseOrderItems");
            modelBuilder.Entity<SalesOrder>().ToTable("SalesOrders");
            modelBuilder.Entity<SalesOrderItem>().ToTable("SalesOrderItems");
            modelBuilder.Entity<InventoryLog>().ToTable("InventoryLogs");

            modelBuilder.Entity<Product>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PurchaseOrderItem>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SalesOrderItem>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(i => i.PurchaseOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.PurchaseOrderId);

            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(i => i.SalesOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.SalesOrderId);

            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(i => i.Product)
                .WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(i => i.ProductId);

            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(i => i.Product)
                .WithMany(p => p.SalesOrderItems)
                .HasForeignKey(i => i.ProductId);

            modelBuilder.Entity<InventoryLog>()
                .HasOne(l => l.Product)
                .WithMany(p => p.InventoryLogs)
                .HasForeignKey(l => l.ProductId);
        }
    }
}
