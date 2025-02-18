using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Data.Models;

namespace Data.Context
{
    public class CoreAPIContext : IdentityDbContext
    {
        public CoreAPIContext(DbContextOptions<CoreAPIContext> options) : base(options)
        {
        }

        // DbSet properties for all entities
        public DbSet<CustomerModel> Customers { get; set; }
        public DbSet<ProductModel> Products { get; set; }
        public DbSet<PriceHistoryModel> PriceHistories { get; set; }
        public DbSet<StockModel> Stocks { get; set; }
        public DbSet<StockMovementModel> StockMovements { get; set; }
        public DbSet<SaleModel> Sales { get; set; }
        public DbSet<SaleItemModel> SaleItems { get; set; }
        public DbSet<PaymentMethodModel> PaymentMethods { get; set; }
        public DbSet<SupplierModel> Suppliers { get; set; }
        public DbSet<SupplierPriceModel> SupplierPrices { get; set; }
        public DbSet<PurchaseOrderModel> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItemModel> PurchaseOrderItems { get; set; }
        public DbSet<ProductExpirationModel> ProductExpirations { get; set; }
        public DbSet<LoyaltyProgramModel> LoyaltyPrograms { get; set; }
        public DbSet<PromotionModel> Promotions { get; set; }
        public DbSet<TaxModel> Taxes { get; set; }
        public DbSet<ProductTaxModel> ProductTaxes { get; set; }
        public DbSet<UserHierarchyModel> UserHierarchies { get; set; }
        public DbSet<SubscriptionPlanModel> SubscriptionPlans { get; set; }
        public DbSet<UserSubscriptionModel> UserSubscriptions { get; set; }
        public DbSet<UserGroupModel> UserGroups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // *** User and Authentication Related Configurations ***

            // Relationship: UserHierarchy -> ParentUser
            // Description: Represents the hierarchical relationship between users where ParentUser is the supervisor
            // When ParentUser is deleted, all their hierarchical relationships are also deleted
            modelBuilder.Entity<UserHierarchyModel>()
                .HasOne(uh => uh.ParentUser)
                .WithMany()
                .HasForeignKey(uh => uh.ParentUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: UserHierarchy -> LinkedUser
            // Description: Represents the user being supervised in the hierarchy
            // When LinkedUser is deleted, their hierarchical relationships are also deleted
            modelBuilder.Entity<UserHierarchyModel>()
                .HasOne(uh => uh.LinkedUser)
                .WithMany()
                .HasForeignKey(uh => uh.LinkedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: UserGroup -> User
            // Description: Associates a user with their group for data isolation
            // When User is deleted, their group association is preserved for audit purposes
            modelBuilder.Entity<UserGroupModel>()
                .HasOne(ug => ug.User)
                .WithOne()
                .HasForeignKey<UserGroupModel>(ug => ug.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: UserSubscription -> User
            // Description: Tracks user's subscription plan history
            // When User is deleted, their subscription history is preserved
            modelBuilder.Entity<UserSubscriptionModel>()
                .HasOne(us => us.User)
                .WithMany()
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: UserSubscription -> SubscriptionPlan
            // Description: Links a user's subscription to the plan details
            // When SubscriptionPlan is deleted, preserve user subscription records
            modelBuilder.Entity<UserSubscriptionModel>()
                .HasOne(us => us.SubscriptionPlan)
                .WithMany(sp => sp.UserSubscriptions)
                .HasForeignKey(us => us.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // *** Sales Related Configurations ***

            // Relationship: Sale -> User
            // Description: Records which user processed the sale
            // When User is deleted, sales records are preserved
            modelBuilder.Entity<SaleModel>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Sale -> Customer
            // Description: Optional link to the customer who made the purchase
            // When Customer is deleted, their reference in sales becomes null
            modelBuilder.Entity<SaleModel>()
                .HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relationship: Sale -> PaymentMethod
            // Description: Records how the sale was paid
            // When PaymentMethod is deleted, sales records are preserved
            modelBuilder.Entity<SaleModel>()
                .HasOne(s => s.PaymentMethod)
                .WithMany()
                .HasForeignKey(s => s.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: SaleItem -> Sale
            // Description: Items included in a sale
            // When Sale is deleted, all its items are also deleted
            modelBuilder.Entity<SaleItemModel>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: SaleItem -> Product
            // Description: Links sale item to the product sold
            // When Product is deleted, sale records are preserved
            modelBuilder.Entity<SaleItemModel>()
                .HasOne(si => si.Product)
                .WithMany()
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // *** Stock Related Configurations ***

            // Relationship: Stock -> Product
            // Description: Current stock level for a product
            // When Product is deleted, its stock records are also deleted
            modelBuilder.Entity<StockModel>()
                .HasOne(s => s.Product)
                .WithMany(p => p.Stock)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: StockMovement -> Stock
            // Description: Records changes in stock levels
            // When Stock is deleted, all movement records are deleted
            modelBuilder.Entity<StockMovementModel>()
                .HasOne(sm => sm.Stock)
                .WithMany(s => s.StockMovements)
                .HasForeignKey(sm => sm.StockId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: StockMovement -> User
            // Description: Tracks who made the stock movement
            // When User is deleted, movement records are preserved
            modelBuilder.Entity<StockMovementModel>()
                .HasOne(sm => sm.User)
                .WithMany()
                .HasForeignKey(sm => sm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: ProductExpiration -> Product
            // Description: Tracks expiration dates for product batches
            // When Product is deleted, expiration records are deleted
            modelBuilder.Entity<ProductExpirationModel>()
                .HasOne(pe => pe.Product)
                .WithMany()
                .HasForeignKey(pe => pe.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: ProductExpiration -> Stock
            // Description: Links expiration dates to specific stock entries
            // When Stock is deleted, expiration records are deleted
            modelBuilder.Entity<ProductExpirationModel>()
                .HasOne(pe => pe.Stock)
                .WithMany()
                .HasForeignKey(pe => pe.StockId)
                .OnDelete(DeleteBehavior.Cascade);

            // *** Price History Related Configurations ***

            // Relationship: PriceHistory -> ChangedByUser (IdentityUser)
            // Description: Links price changes to the user who made them
            // When User is deleted, price history is preserved (Restrict)
            modelBuilder.Entity<PriceHistoryModel>()
                .HasOne(ph => ph.ChangedByUser)
                .WithMany()
                .HasForeignKey(ph => ph.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: PriceHistory -> Product
            // Description: Links price changes to the specific product
            // When Product is deleted, all its price history is also deleted (Cascade)
            modelBuilder.Entity<PriceHistoryModel>()
                .HasOne(ph => ph.Product)
                .WithMany(p => p.PriceHistories)
                .HasForeignKey(ph => ph.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // *** Product Related Configurations ***

            // Relationship: SupplierPrice -> Supplier
            // Description: Tracks supplier's prices for products
            // When Supplier is deleted, their price records are deleted
            modelBuilder.Entity<SupplierPriceModel>()
                .HasOne(sp => sp.Supplier)
                .WithMany(s => s.SupplierPrices)
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: SupplierPrice -> Product
            // Description: Links supplier prices to products
            // When Product is deleted, supplier price records are deleted
            modelBuilder.Entity<SupplierPriceModel>()
                .HasOne(sp => sp.Product)
                .WithMany()
                .HasForeignKey(sp => sp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: ProductTax -> Tax
            // Description: Associates products with applicable taxes
            // When Tax is deleted, product tax associations are deleted
            modelBuilder.Entity<ProductTaxModel>()
                .HasOne(pt => pt.Tax)
                .WithMany()
                .HasForeignKey(pt => pt.TaxId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: ProductTax -> Product
            // Description: Links products to their tax configurations
            // When Product is deleted, tax associations are deleted
            modelBuilder.Entity<ProductTaxModel>()
                .HasOne(pt => pt.Product)
                .WithMany(p => p.ProductTaxes)
                .HasForeignKey(pt => pt.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // *** Purchase Related Configurations ***

            // Relationship: PurchaseOrder -> Supplier
            // Description: Links purchase orders to suppliers
            // When Supplier is deleted, purchase orders are preserved
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasOne(po => po.Supplier)
                .WithMany()
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: PurchaseOrderItem -> PurchaseOrder
            // Description: Items included in a purchase order
            // When PurchaseOrder is deleted, all its items are deleted
            modelBuilder.Entity<PurchaseOrderItemModel>()
                .HasOne(poi => poi.Order)
                .WithMany(po => po.Items)
                .HasForeignKey(poi => poi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: PurchaseOrderItem -> Product
            // Description: Links purchase order items to products
            // When Product is deleted, purchase order records are preserved
            modelBuilder.Entity<PurchaseOrderItemModel>()
                .HasOne(poi => poi.Product)
                .WithMany()
                .HasForeignKey(poi => poi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // *** Customer Related Configurations ***

            // Relationship: Customer -> LoyaltyProgram
            // Description: Optional association with loyalty program
            // When LoyaltyProgram is deleted, customer's program becomes null
            modelBuilder.Entity<CustomerModel>()
                .HasOne(c => c.LoyaltyProgram)
                .WithMany()
                .HasForeignKey(c => c.LoyaltyProgramId)
                .OnDelete(DeleteBehavior.SetNull);

            // *** Supplier Related Configurations ***

            // Relationship: SupplierPrice -> Supplier
            // Description: Links supplier prices to the supplier
            // When Supplier is deleted, all their prices are deleted
            modelBuilder.Entity<SupplierPriceModel>()
                .HasOne(sp => sp.Supplier)
                .WithMany(s => s.SupplierPrices)
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: SupplierPrice -> Product
            // Description: Links supplier prices to products
            // When Product is deleted, supplier prices are preserved
            modelBuilder.Entity<SupplierPriceModel>()
                .HasOne(sp => sp.Product)
                .WithMany()
                .HasForeignKey(sp => sp.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // *** Tax Related Configurations ***

            // Relationship: ProductTax -> Product
            // Description: Links taxes to products
            // When Product is deleted, tax associations are deleted
            modelBuilder.Entity<ProductTaxModel>()
                .HasOne(pt => pt.Product)
                .WithMany()
                .HasForeignKey(pt => pt.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: ProductTax -> Tax
            // Description: Links products to taxes
            // When Tax is deleted, product associations are preserved
            modelBuilder.Entity<ProductTaxModel>()
                .HasOne(pt => pt.Tax)
                .WithMany(t => t.ProductTaxes)
                .HasForeignKey(pt => pt.TaxId)
                .OnDelete(DeleteBehavior.Restrict);

            // *** Customer Related Configurations ***

            // Relationship: Customer -> LoyaltyProgram
            // Description: Links customers to loyalty programs
            // When LoyaltyProgram is deleted, customer associations are preserved
            modelBuilder.Entity<CustomerModel>()
                .HasOne(c => c.LoyaltyProgram)
                .WithMany(lp => lp.Customers)
                .HasForeignKey(c => c.LoyaltyProgramId)
                .OnDelete(DeleteBehavior.Restrict);

            // *** Unique Constraints Within Groups ***

            // Ensures unique user per group
            modelBuilder.Entity<UserGroupModel>()
                .HasIndex(ug => ug.UserId)
                .IsUnique();

            // Ensures unique SKU within each group
            modelBuilder.Entity<ProductModel>()
                .HasIndex(p => new { p.UserGroupId, p.SKU })
                .IsUnique();

            // Ensures unique barcode within each group
            modelBuilder.Entity<ProductModel>()
                .HasIndex(p => new { p.UserGroupId, p.BarCode })
                .IsUnique();

            // Ensures unique supplier document within each group
            modelBuilder.Entity<SupplierModel>()
                .HasIndex(s => new { s.UserGroupId, s.Document })
                .IsUnique();

            // Ensures unique customer document within each group
            modelBuilder.Entity<CustomerModel>()
                .HasIndex(c => new { c.UserGroupId, c.Document })
                .IsUnique();

            // Ensures unique payment method code within each group
            modelBuilder.Entity<PaymentMethodModel>()
                .HasIndex(pm => new { pm.UserGroupId, pm.Code })
                .IsUnique();

            // Ensures unique product-tax combination within each group
            modelBuilder.Entity<ProductTaxModel>()
                .HasIndex(pt => new { pt.UserGroupId, pt.ProductId, pt.TaxId })
                .IsUnique();

            // Ensures unique supplier price for a product at a specific date within each group
            modelBuilder.Entity<SupplierPriceModel>()
                .HasIndex(sp => new { sp.UserGroupId, sp.SupplierId, sp.ProductId, sp.ValidFrom })
                .IsUnique();

            // Ensures unique user subscription for a plan at a specific date within each group
            modelBuilder.Entity<UserSubscriptionModel>()
                .HasIndex(us => new { us.UserGroupId, us.UserId, us.SubscriptionPlanId, us.StartDate })
                .IsUnique();

            // Ensures unique stock movement record within each group
            modelBuilder.Entity<StockMovementModel>()
                .HasIndex(sm => new { sm.UserGroupId, sm.StockId, sm.MovementType, sm.MovementDate })
                .IsUnique();
        }
    }
}
