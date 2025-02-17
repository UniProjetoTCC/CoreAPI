using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Data.Context
{
    public class CoreAPIContext : IdentityDbContext
    {
        public CoreAPIContext(DbContextOptions<CoreAPIContext> options) : base(options)
        {
        }
        // public DbSet<Customer> Customers { get; set; }
        // public DbSet<Product> Products { get; set; }
        // public DbSet<PriceHistory> PriceHistories { get; set; }
        // public DbSet<Stock> Stocks { get; set; }
        // public DbSet<StockMovement> StockMovements { get; set; }
        // public DbSet<Sale> Sales { get; set; }
        // public DbSet<SaleItem> SaleItems { get; set; }
        // public DbSet<PaymentMethod> PaymentMethods { get; set; }
        // public DbSet<Supplier> Suppliers { get; set; }
        // public DbSet<SupplierPrice> SupplierPrices { get; set; }
        // public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        // public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        // public DbSet<ProductExpiration> ProductExpirations { get; set; }
        // public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }
        // public DbSet<Promotion> Promotions { get; set; }
        // public DbSet<Tax> Taxes { get; set; }
        // public DbSet<ProductTax> ProductTaxes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // A Customer can have a LoyaltyProgram, but if the LoyaltyProgram is deleted, do not delete the customer
            // modelBuilder.Entity<Customer>()
            //     .HasOne(c => c.LoyaltyProgram)
            //     .WithMany()
            //     .HasForeignKey(c => c.LoyaltyProgramId)
            //     .OnDelete(DeleteBehavior.SetNull);

            // A SaleItem requires a Sale and a Product; if a Sale is deleted, delete the SaleItems
            // modelBuilder.Entity<SaleItem>()
            //     .HasOne(si => si.Sale)
            //     .WithMany(s => s.SaleItems)
            //     .HasForeignKey(si => si.SaleId)
            //     .OnDelete(DeleteBehavior.Cascade);

            // A Sale requires a User, but if the User is deleted, keep the sales
            // modelBuilder.Entity<Sale>()
            //     .HasOne(s => s.User)
            //     .WithMany()
            //     .HasForeignKey(s => s.UserId)
            //     .OnDelete(DeleteBehavior.Restrict);

            // A Sale requires a Customer, but if the Customer is deleted, keep the sale without a linked customer
            // modelBuilder.Entity<Sale>()
            //     .HasOne(s => s.Customer)
            //     .WithMany()
            //     .HasForeignKey(s => s.CustomerId)
            //     .OnDelete(DeleteBehavior.SetNull);

            // A stock belongs to a product; if a product is deleted, delete the stock
            // modelBuilder.Entity<Stock>()
            //     .HasOne(s => s.Product)
            //     .WithMany(p => p.Stock)
            //     .HasForeignKey(s => s.ProductId)
            //     .OnDelete(DeleteBehavior.Cascade);

            // A price history belongs to a product; if the product is deleted, keep the history
            // modelBuilder.Entity<PriceHistory>()
            //     .HasOne(ph => ph.Product)
            //     .WithMany(p => p.PriceHistories)
            //     .HasForeignKey(ph => ph.ProductId)
            //     .OnDelete(DeleteBehavior.Restrict);

            // Ensure each product has a unique barcode
            // modelBuilder.Entity<Product>()
            //     .HasIndex(p => p.BarCode)
            //     .IsUnique();

            // Ensure each SKU is unique
            // modelBuilder.Entity<Product>()
            //     .HasIndex(p => p.SKU)
            //     .IsUnique();

            // An order has a supplier; if a supplier is deleted, keep the orders
            // modelBuilder.Entity<PurchaseOrder>()
            //     .HasOne(po => po.Supplier)
            //     .WithMany()
            //     .HasForeignKey(po => po.SupplierId)
            //     .OnDelete(DeleteBehavior.Restrict);

            // A product can have taxes; if the tax is deleted, keep the product
            // modelBuilder.Entity<ProductTax>()
            //     .HasOne(pt => pt.Tax)
            //     .WithMany()
            //     .HasForeignKey(pt => pt.TaxId)
            //     .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
