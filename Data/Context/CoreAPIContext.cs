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

        // User-related entities
        public DbSet<CustomerModel> Customers { get; set; }
        public DbSet<LinkedUserModel> LinkedUsers { get; set; }
        public DbSet<UserGroupModel> UserGroups { get; set; }
        public DbSet<LoyaltyProgramModel> LoyaltyPrograms { get; set; }

        // Product-related entities
        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<ProductModel> Products { get; set; }
        public DbSet<ProductExpirationModel> ProductExpirations { get; set; }
        public DbSet<ProductTaxModel> ProductTaxes { get; set; }
        public DbSet<ProductPromotionModel> ProductPromotions { get; set; }
        public DbSet<PromotionModel> Promotions { get; set; }
        public DbSet<SupplierModel> Suppliers { get; set; }
        public DbSet<SupplierPriceModel> SupplierPrices { get; set; }

        // Sale-related entities
        public DbSet<SaleModel> Sales { get; set; }
        public DbSet<SaleItemModel> SaleItems { get; set; }

        // Purchase order-related entities
        public DbSet<PurchaseOrderModel> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItemModel> PurchaseOrderItems { get; set; }

        // Stock-related entities
        public DbSet<StockModel> Stocks { get; set; }
        public DbSet<StockMovementModel> StockMovements { get; set; }

        // Pricing and tax-related entities
        public DbSet<PriceHistoryModel> PriceHistories { get; set; }
        public DbSet<TaxModel> Taxes { get; set; }

        // Subscription-related entities
        public DbSet<SubscriptionPlanModel> SubscriptionPlans { get; set; }

        // Payment-related entities
        public DbSet<PaymentMethodModel> PaymentMethods { get; set; }
        public DbSet<UserPaymentCardModel> UserPaymentCards { get; set; }

        // Background job-related entities
        public DbSet<BackgroundJobsModel> BackgroundJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Call the base method to ensure any configurations defined in the base class are applied.
            base.OnModelCreating(modelBuilder);

            //=================================================================
            // Category relationships and indexes
            //=================================================================
            // Configure the relationship between Category and UserGroup.
            // Each category belongs to a specific user group.
            modelBuilder.Entity<CategoryModel>()
                .HasOne(c => c.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted

            // Configure the one-to-many relationship between Category and Product.
            // A category can have multiple products, and each product must belong to one catsegory.
            modelBuilder.Entity<CategoryModel>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete of products when a category is deleted.

            // Create an index to optimize queries filtering by GroupId and Category Name.
            modelBuilder.Entity<CategoryModel>()
                .HasIndex(c => new { c.GroupId, c.Name })
                .HasDatabaseName("IX_Categories_GroupId_Name");

            //=================================================================
            // Product relationships and indexes
            //=================================================================
            // Configure relationship between Product and UserGroup.
            // Each product belongs to a specific user group.
            modelBuilder.Entity<ProductModel>()
                .HasOne(p => p.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Configure the relationship between Product and Category.
            // Each product must belong to one category.
            modelBuilder.Entity<ProductModel>()
                .HasOne(p => p.Category)
                .WithMany() // Category does not require a navigation property for products here.
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Create a unique index to optimize searches by SKU within a user group.
            modelBuilder.Entity<ProductModel>()
                .HasIndex(p => new { p.GroupId, p.SKU })
                .HasDatabaseName("IX_Products_GroupId_SKU")
                .IsUnique();

            // Create a unique index to optimize barcode searches within a user group.
            modelBuilder.Entity<ProductModel>()
                .HasIndex(p => new { p.GroupId, p.BarCode })
                .HasDatabaseName("IX_Products_GroupId_BarCode")
                .IsUnique();

            // Create an index for product searches by name within a user group.
            modelBuilder.Entity<ProductModel>()
                .HasIndex(p => new { p.GroupId, p.Name })
                .HasDatabaseName("IX_Products_GroupId_Name");

            //=================================================================
            // Stock relationships and indexes
            //=================================================================
            // Configure the relationship between Stock and Product.
            // Each stock record is associated with one product and a product can have multiple stock records.
            modelBuilder.Entity<StockModel>()
                .HasOne(s => s.Product)
                .WithMany(p => p.Stock)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure relationship between Stock and UserGroup.
            // Each stock record belongs to a specific user group.
            modelBuilder.Entity<StockModel>()
                .HasOne(s => s.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted

            // Create an index to optimize queries filtering by ProductId
            modelBuilder.Entity<StockModel>()
                .HasIndex(s => s.ProductId)
                .HasDatabaseName("IX_Stock_ProductId");

            // Create an index to optimize queries filtering by GroupId and ProductId.
            modelBuilder.Entity<StockModel>()
                .HasIndex(s => new { s.GroupId, s.ProductId })
                .HasDatabaseName("IX_Stock_GroupId_ProductId");

            //=================================================================
            // Stock Movement relationships and indexes
            //=================================================================
            // Configure the relationship between StockMovement and Stock.
            // Each stock movement is linked to one stock record, and a stock can have multiple movements.
            // When a stock record is deleted, all its movements are also deleted.
            modelBuilder.Entity<StockMovementModel>()
                .HasOne(sm => sm.Stock)
                .WithMany(s => s.StockMovements)
                .HasForeignKey(sm => sm.StockId)
                .OnDelete(DeleteBehavior.Cascade); // Delete StockMovements when Stock is deleted

            // Create an index to optimize queries by StockId
            modelBuilder.Entity<StockMovementModel>()
                .HasIndex(sm => sm.StockId)
                .HasDatabaseName("IX_StockMovements_StockId");

            // Create an index to optimize queries by MovementDate
            modelBuilder.Entity<StockMovementModel>()
                .HasIndex(sm => sm.MovementDate)
                .HasDatabaseName("IX_StockMovements_Date");

            // Create an index to optimize queries filtering by StockId and MovementDate.
            modelBuilder.Entity<StockMovementModel>()
                .HasIndex(sm => new { sm.StockId, sm.MovementDate })
                .HasDatabaseName("IX_StockMovements_StockId_Date");

            //=================================================================
            // Product Expiration relationships and indexes
            //=================================================================
            // Configure the relationship between ProductExpiration and Product.
            // Each expiration record is linked to one product, and a product can have multiple expiration records.
            modelBuilder.Entity<ProductExpirationModel>()
                .HasOne(pe => pe.Product)
                .WithMany(p => p.ProductExpirations)
                .HasForeignKey(pe => pe.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure the relationship between ProductExpiration and Stock.
            // Each expiration record is linked to one stock record, and a stock can have multiple expiration records.
            modelBuilder.Entity<ProductExpirationModel>()
                .HasOne(pe => pe.Stock)
                .WithMany(s => s.ProductExpirations)
                .HasForeignKey(pe => pe.StockId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Create an index to optimize queries filtering by StockId and ExpirationDate.
            modelBuilder.Entity<ProductExpirationModel>()
                .HasIndex(pe => new { pe.StockId, pe.ExpirationDate })
                .HasDatabaseName("IX_ProductExpirations_StockId_Date");

            //=================================================================
            // Price History relationships and indexes
            //=================================================================
            // Configure the relationship between PriceHistory and Product.
            // Each price history record is associated with one product to track price changes over time.
            modelBuilder.Entity<PriceHistoryModel>()
                .HasOne(ph => ph.Product)
                .WithMany(p => p.PriceHistories)
                .HasForeignKey(ph => ph.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure the relationship between PriceHistory and UserGroup.
            // Each price history record belongs to a specific user group.
            modelBuilder.Entity<PriceHistoryModel>()
                .HasOne(ph => ph.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(ph => ph.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Configure the relationship to track which user made the price change.
            modelBuilder.Entity<PriceHistoryModel>()
                .HasOne(ph => ph.ChangedByUser)
                .WithMany() // No navigation property on the User side.
                .HasForeignKey(ph => ph.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Create an index to optimize queries filtering by GroupId, ProductId, and ChangeDate.
            modelBuilder.Entity<PriceHistoryModel>()
                .HasIndex(ph => new { ph.GroupId, ph.ProductId, ph.ChangeDate })
                .HasDatabaseName("IX_PriceHistory_GroupId_ProductId_Date");

            //=================================================================
            // Tax relationships and indexes
            //=================================================================
            // Configure the relationship between Tax and UserGroup.
            // Each tax belongs to a specific user group.
            modelBuilder.Entity<TaxModel>()
                .HasOne(t => t.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(t => t.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Create a unique index to optimize tax searches by Name within a user group.
            modelBuilder.Entity<TaxModel>()
                .HasIndex(t => new { t.GroupId, t.Name })
                .HasDatabaseName("IX_Taxes_GroupId_Name")
                .IsUnique();

            //=================================================================
            // Product Tax relationships and indexes
            //=================================================================
            // Configure the relationship between ProductTax and Product.
            // Each product-tax association links one product with one tax rate.
            modelBuilder.Entity<ProductTaxModel>()
                .HasOne(pt => pt.Product)
                .WithMany(p => p.ProductTaxes)
                .HasForeignKey(pt => pt.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Delete ProductTax when Product is deleted

            // Configure the relationship between ProductTax and Tax.
            // Each product-tax association links one tax with one product.
            modelBuilder.Entity<ProductTaxModel>()
                .HasOne(pt => pt.Tax)
                .WithMany(t => t.ProductTaxes)
                .HasForeignKey(pt => pt.TaxId)
                .OnDelete(DeleteBehavior.Cascade); // Delete ProductTax when Tax is deleted

            // Configure the relationship between ProductTax and UserGroup.
            // Each product-tax association belongs to a specific user group.
            modelBuilder.Entity<ProductTaxModel>()
                .HasOne(pt => pt.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(pt => pt.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Create an index to optimize queries filtering by ProductId
            modelBuilder.Entity<ProductTaxModel>()
                .HasIndex(pt => pt.ProductId)
                .HasDatabaseName("IX_ProductTaxes_ProductId");

            // Create an index to optimize queries filtering by TaxId
            modelBuilder.Entity<ProductTaxModel>()
                .HasIndex(pt => pt.TaxId)
                .HasDatabaseName("IX_ProductTaxes_TaxId");

            // Create a unique index to optimize queries filtering by GroupId, ProductId, and TaxId.
            modelBuilder.Entity<ProductTaxModel>()
                .HasIndex(pt => new { pt.GroupId, pt.ProductId, pt.TaxId })
                .HasDatabaseName("IX_ProductTaxes_GroupId_ProductId_TaxId")
                .IsUnique();

            //=================================================================
            // Product Promotion relationships and indexes
            //=================================================================
            // Configure the relationship between ProductPromotion and Product.
            // Each product-promotion association links one product with one promotion.
            modelBuilder.Entity<ProductPromotionModel>()
                .HasOne(pp => pp.Product)
                .WithMany(p => p.ProductPromotions)
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Delete ProductPromotion when Product is deleted

            // Configure the relationship between ProductPromotion and Promotion.
            // Each product-promotion association links one promotion with one product.
            modelBuilder.Entity<ProductPromotionModel>()
                .HasOne(pp => pp.Promotion)
                .WithMany(p => p.ProductPromotions)
                .HasForeignKey(pp => pp.PromotionId)
                .OnDelete(DeleteBehavior.Cascade); // Delete ProductPromotion when Promotion is deleted

            // Configure the relationship between ProductPromotion and UserGroup.
            // Each product-promotion association belongs to a specific user group.
            modelBuilder.Entity<ProductPromotionModel>()
                .HasOne(pp => pp.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(pp => pp.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete.

            // Create an index to optimize queries filtering by ProductId
            modelBuilder.Entity<ProductPromotionModel>()
                .HasIndex(pp => pp.ProductId)
                .HasDatabaseName("IX_ProductPromotions_ProductId");

            // Create an index to optimize queries filtering by PromotionId
            modelBuilder.Entity<ProductPromotionModel>()
                .HasIndex(pp => pp.PromotionId)
                .HasDatabaseName("IX_ProductPromotions_PromotionId");

            // Create a unique index to optimize queries filtering by GroupId, ProductId and PromotionId.
            modelBuilder.Entity<ProductPromotionModel>()
                .HasIndex(pp => new { pp.GroupId, pp.ProductId, pp.PromotionId })
                .HasDatabaseName("IX_ProductPromotions_GroupId_ProductId_PromotionId")
                .IsUnique();

            //=================================================================
            // Payment Method relationships and indexes
            //=================================================================
            // Configure the relationship between PaymentMethod and UserGroup.
            // Each payment method belongs to a specific user group.
            modelBuilder.Entity<PaymentMethodModel>()
                .HasOne(pm => pm.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(pm => pm.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Configure the relationship between PaymentMethod and Sale.
            // A payment method can be used in multiple sales.
            modelBuilder.Entity<PaymentMethodModel>()
                .HasMany(pm => pm.Sales)
                .WithOne(s => s.PaymentMethod)
                .HasForeignKey(s => s.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Create a unique index to optimize payment method searches by Code within a user group.
            modelBuilder.Entity<PaymentMethodModel>()
                .HasIndex(pm => new { pm.GroupId, pm.Code })
                .HasDatabaseName("IX_PaymentMethods_GroupId_Code")
                .IsUnique();

            //=================================================================
            // Sale relationships and indexes
            //=================================================================
            // Configure an optional relationship between Sale and Customer.
            // Each sale can be associated with one customer, but this is optional.
            modelBuilder.Entity<SaleModel>()
                .HasOne(s => s.Customer)
                .WithMany() // No navigation property on the Customer side.
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure the relationship between Sale and UserGroup.
            // Each sale belongs to a specific user group.
            modelBuilder.Entity<SaleModel>()
                .HasOne(s => s.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted

            // Configure the relationship between Sale and PaymentMethod.
            // Each sale must have one payment method.
            modelBuilder.Entity<SaleModel>()
                .HasOne(s => s.PaymentMethod)
                .WithMany() // No navigation property on the PaymentMethod side.
                .HasForeignKey(s => s.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Create an index to optimize queries by CustomerId
            modelBuilder.Entity<SaleModel>()
                .HasIndex(s => s.CustomerId)
                .HasDatabaseName("IX_Sales_CustomerId");

            // Create an index to optimize queries by PaymentMethodId
            modelBuilder.Entity<SaleModel>()
                .HasIndex(s => s.PaymentMethodId)
                .HasDatabaseName("IX_Sales_PaymentMethodId");

            // Create an index to optimize queries by SaleDate
            modelBuilder.Entity<SaleModel>()
                .HasIndex(s => s.SaleDate)
                .HasDatabaseName("IX_Sales_Date");

            // Create an index to optimize sale queries filtering by GroupId and SaleDate.
            modelBuilder.Entity<SaleModel>()
                .HasIndex(s => new { s.GroupId, s.SaleDate })
                .HasDatabaseName("IX_Sales_GroupId_Date");

            //=================================================================
            // Sale Item relationships and indexes
            //=================================================================
            // Configure the relationship between SaleItem and Sale.
            // Each sale item belongs to one sale, and a sale can have multiple items.
            // When a sale is deleted, all its items are also deleted.
            modelBuilder.Entity<SaleItemModel>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade); // Delete SaleItems when Sale is deleted

            // Configure the relationship between SaleItem and Product.
            // Each sale item references one product.
            modelBuilder.Entity<SaleItemModel>()
                .HasOne(si => si.Product)
                .WithMany(p => p.SaleItems)
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure the relationship between SaleItem and UserGroup.
            // Each sale item belongs to a specific user group.
            modelBuilder.Entity<SaleItemModel>()
                .HasOne(si => si.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(si => si.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Create an index to optimize queries by SaleId
            modelBuilder.Entity<SaleItemModel>()
                .HasIndex(si => si.SaleId)
                .HasDatabaseName("IX_SaleItems_SaleId");

            // Create an index to optimize queries by ProductId
            modelBuilder.Entity<SaleItemModel>()
                .HasIndex(si => si.ProductId)
                .HasDatabaseName("IX_SaleItems_ProductId");

            // Create an index to optimize queries filtering by GroupId, SaleId, and ProductId.
            modelBuilder.Entity<SaleItemModel>()
                .HasIndex(si => new { si.GroupId, si.SaleId, si.ProductId })
                .HasDatabaseName("IX_SaleItems_GroupId_SaleId_ProductId");

            //=================================================================
            // Purchase Order relationships and indexes
            //=================================================================
            // Configure the relationship between PurchaseOrder and Supplier.
            // Each purchase order is associated with one supplier.
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasOne(po => po.Supplier)
                .WithMany() // No navigation property on the Supplier side.
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure the relationship between PurchaseOrder and UserGroup.
            // Each purchase order belongs to a specific user group.
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasOne(po => po.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(po => po.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted

            // Create an index to optimize queries by SupplierId
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasIndex(po => po.SupplierId)
                .HasDatabaseName("IX_PurchaseOrders_SupplierId");

            // Create an index to optimize queries by OrderDate
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasIndex(po => po.OrderDate)
                .HasDatabaseName("IX_PurchaseOrders_Date");

            // Create a unique index to optimize purchase order searches by OrderNumber within a user group.
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasIndex(po => new { po.GroupId, po.OrderNumber })
                .HasDatabaseName("IX_PurchaseOrders_GroupId_OrderNumber")
                .IsUnique();

            // Create an index to optimize purchase order searches by OrderDate within a user group.
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasIndex(po => new { po.GroupId, po.OrderDate })
                .HasDatabaseName("IX_PurchaseOrders_GroupId_Date");

            //=================================================================
            // Purchase Order Item relationships and indexes
            //=================================================================
            // Configure the relationship between PurchaseOrderItem and PurchaseOrder.
            // Each purchase order item belongs to one purchase order.
            // When a purchase order is deleted, all its items are also deleted.
            modelBuilder.Entity<PurchaseOrderItemModel>()
                .HasOne(poi => poi.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(poi => poi.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade); // Delete PurchaseOrderItems when PurchaseOrder is deleted

            // Configure the relationship between PurchaseOrderItem and Product.
            // Each purchase order item references one product.
            modelBuilder.Entity<PurchaseOrderItemModel>()
                .HasOne(poi => poi.Product)
                .WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(poi => poi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure the relationship between PurchaseOrderItem and UserGroup.
            // Each purchase order item belongs to a specific user group.
            modelBuilder.Entity<PurchaseOrderItemModel>()
                .HasOne(poi => poi.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(poi => poi.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Create an index to optimize queries by PurchaseOrderId
            modelBuilder.Entity<PurchaseOrderItemModel>()
                .HasIndex(poi => poi.PurchaseOrderId)
                .HasDatabaseName("IX_PurchaseOrderItems_PurchaseOrderId");

            // Create an index to optimize queries by ProductId
            modelBuilder.Entity<PurchaseOrderItemModel>()
                .HasIndex(poi => poi.ProductId)
                .HasDatabaseName("IX_PurchaseOrderItems_ProductId");

            // Create an index to optimize queries filtering by GroupId, PurchaseOrderId, and ProductId.
            modelBuilder.Entity<PurchaseOrderItemModel>()
                .HasIndex(poi => new { poi.GroupId, poi.PurchaseOrderId, poi.ProductId })
                .HasDatabaseName("IX_PurchaseOrderItems_GroupId_PurchaseOrderId_ProductId");

            //=================================================================
            // Supplier relationships and indexes
            //=================================================================
            // Configure the relationship between Supplier and UserGroup.
            // Each supplier belongs to a specific user group.
            modelBuilder.Entity<SupplierModel>()
                .HasOne(s => s.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Create a unique index to optimize supplier searches by Document within a user group.
            modelBuilder.Entity<SupplierModel>()
                .HasIndex(s => new { s.GroupId, s.Document })
                .HasDatabaseName("IX_Suppliers_GroupId_Document")
                .IsUnique();

            // Create an index to optimize supplier searches by Name within a user group.
            modelBuilder.Entity<SupplierModel>()
                .HasIndex(s => new { s.GroupId, s.Name })
                .HasDatabaseName("IX_Suppliers_GroupId_Name");

            //=================================================================
            // Supplier Price relationships and indexes
            //=================================================================
            // Configure the relationship between SupplierPrice and Supplier.
            // Each supplier price is associated with one supplier.
            modelBuilder.Entity<SupplierPriceModel>()
                .HasOne(sp => sp.Supplier)
                .WithMany() // No navigation property on the Supplier side.
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure the relationship between SupplierPrice and Product.
            // Each supplier price is associated with one product.
            modelBuilder.Entity<SupplierPriceModel>()
                .HasOne(sp => sp.Product)
                .WithMany(p => p.SupplierPrices)
                .HasForeignKey(sp => sp.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Configure the relationship between SupplierPrice and UserGroup.
            // Each supplier price belongs to a specific user group.
            modelBuilder.Entity<SupplierPriceModel>()
                .HasOne(sp => sp.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(sp => sp.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Create an index to optimize queries filtering by GroupId, SupplierId, ProductId, and ValidFrom.
            modelBuilder.Entity<SupplierPriceModel>()
                .HasIndex(sp => new { sp.GroupId, sp.SupplierId, sp.ProductId, sp.ValidFrom })
                .HasDatabaseName("IX_SupplierPrices_GroupId_SupplierId_ProductId_ValidFrom");

            //===============================================================
            // Customer relationships and indexes
            //=================================================================
            // Configure the relationship between Customer and UserGroup.
            // Each customer belongs to a specific user group.
            modelBuilder.Entity<CustomerModel>()
                .HasOne(c => c.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Configure the optional relationship between Customer and LoyaltyProgram.
            // Each customer can be enrolled in one loyalty program.
            modelBuilder.Entity<CustomerModel>()
                .HasOne(c => c.LoyaltyProgram)
                .WithMany(lp => lp.Customers)
                .HasForeignKey(c => c.LoyaltyProgramId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete.

            // Create a unique index to optimize customer searches by Document within a user group.
            modelBuilder.Entity<CustomerModel>()
                .HasIndex(c => new { c.GroupId, c.Document })
                .HasDatabaseName("IX_Customers_GroupId_Document")
                .IsUnique();

            // Create a unique index to optimize customer searches by Email within a user group.
            modelBuilder.Entity<CustomerModel>()
                .HasIndex(c => new { c.GroupId, c.Email })
                .HasDatabaseName("IX_Customers_GroupId_Email")
                .IsUnique();

            // Create an index to optimize customer searches by Name within a user group.
            modelBuilder.Entity<CustomerModel>()
                .HasIndex(c => new { c.GroupId, c.Name })
                .HasDatabaseName("IX_Customers_GroupId_Name");

            //=================================================================
            // Loyalty Program relationships and indexes
            //=================================================================
            // Configure the relationship between LoyaltyProgram and UserGroup.
            // Each loyalty program belongs to a specific user group.
            modelBuilder.Entity<LoyaltyProgramModel>()
                .HasOne(lp => lp.UserGroup)
                .WithMany() // No navigation property on the UserGroup side.
                .HasForeignKey(lp => lp.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Delete when UserGroup is deleted.

            // Create a unique index to optimize loyalty program searches by Name within a user group.
            modelBuilder.Entity<LoyaltyProgramModel>()
                .HasIndex(lp => new { lp.GroupId, lp.Name })
                .HasDatabaseName("IX_LoyaltyPrograms_GroupId_Name")
                .IsUnique();

            //=================================================================
            // Payment Card relationships and indexes
            //=================================================================
            // Configure the relationship between PaymentCard and IdentityUser.
            // Each PaymentCard is associated with one IdentityUser (owner of the card).
            modelBuilder.Entity<UserPaymentCardModel>()
                .HasOne(upc => upc.User) // A card belongs to a single user
                .WithMany() // Assuming you don't have a collection of cards in IdentityUser
                .HasForeignKey(upc => upc.UserId) // Defines UserId as the foreign key
                .OnDelete(DeleteBehavior.Cascade); // When a user is deleted, the associated cards are also deleted

            // Index for UserId to improve query performance
            modelBuilder.Entity<UserPaymentCardModel>()
                .HasIndex(upc => upc.UserId); // Creates an index on UserId
        }
    }
}
