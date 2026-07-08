using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace TMDTStore.Models;

public partial class StoreDbContext : IdentityDbContext<User>
{
    public StoreDbContext()
    {
    }

    public StoreDbContext(DbContextOptions<StoreDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductBadge> ProductBadges { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<ProductEmbedding> ProductEmbeddings { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public new virtual DbSet<Role> Roles { get; set; }

    public new virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .HasPostgresEnum("discount_type", new[] { "fixed", "percentage" })
            .HasPostgresEnum("order_status", new[] { "pending", "paid", "shipping", "completed", "cancelled_auto", "cancelled_by_user" })
            .HasPostgresExtension("vector");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_categories");

            entity.ToTable("categories");

            entity.HasIndex(e => e.ParentId, "ix_categories_parent_id");

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("('CAT_'::text || lpad((nextval('cat_id_seq'::regclass))::text, 3, '0'::text))")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ParentId)
                .HasMaxLength(20)
                .HasColumnName("parent_id");
            entity.Property(e => e.Slug).HasColumnName("slug");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_categories_categories_parent_id");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("pk_inventories");

            entity.ToTable("inventories");

            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.ReservedQuantity)
                .HasDefaultValue(0)
                .HasColumnName("reserved_quantity");
            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0)
                .HasColumnName("stock_quantity");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Product).WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(d => d.ProductId)
                .HasConstraintName("fk_inventories_products_product_id");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_orders");

            entity.ToTable("orders");

            entity.HasIndex(e => e.UserId, "ix_orders_user_id");

            entity.HasIndex(e => e.VoucherId, "ix_orders_voucher_id");

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("('ORD_'::text || lpad((nextval('order_id_seq'::regclass))::text, 3, '0'::text))")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(18, 2)
                .HasColumnName("discount_amount");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(18, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.ShippingFee)
                .HasPrecision(18, 2)
                .HasColumnName("shipping_fee");
            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .HasColumnName("user_id");
            entity.Property(e => e.VoucherId)
                .HasMaxLength(20)
                .HasColumnName("voucher_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending")
                .HasColumnName("status");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_orders_users_user_id");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Orders)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_orders_vouchers_voucher_id");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.ProductId }).HasName("pk_order_items");

            entity.ToTable("order_items");

            entity.HasIndex(e => e.ProductId, "ix_order_items_product_id");

            entity.Property(e => e.OrderId)
                .HasMaxLength(20)
                .HasColumnName("order_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2)
                .HasColumnName("unit_price");
            entity.Property(e => e.VariantId)
                .HasMaxLength(20)
                .HasColumnName("variant_id");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
            entity.Property(e => e.VariantName)
                .HasMaxLength(200)
                .HasColumnName("variant_name");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_order_items_orders_order_id");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_order_items_products_product_id");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_order_status_history");

            entity.ToTable("order_status_history");

            entity.HasIndex(e => e.ChangedByUserId, "ix_order_status_history_changed_by_user_id");

            entity.HasIndex(e => e.OrderId, "ix_order_status_history_order_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChangedAtUtc)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at_utc");
            entity.Property(e => e.ChangedByUserId)
                .HasMaxLength(450)
                .HasColumnName("changed_by_user_id");
            entity.Property(e => e.OrderId)
                .HasMaxLength(20)
                .HasColumnName("order_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_order_status_history_users_changed_by_user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_order_status_history_orders_order_id");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_permissions");

            entity.ToTable("permissions");

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("('PERM_'::text || lpad((nextval('perm_id_seq'::regclass))::text, 2, '0'::text))")
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_products");

            entity.ToTable("products");

            entity.HasIndex(e => e.CategoryId, "ix_products_category_id");

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("('PROD_'::text || lpad((nextval('prod_id_seq'::regclass))::text, 3, '0'::text))")
                .HasColumnName("id");
            entity.Property(e => e.BadgeLabels).HasColumnName("badge_labels");
            entity.Property(e => e.BrandId)
                .HasMaxLength(20)
                .HasColumnName("brand_id");
            entity.Property(e => e.BrandName)
                .HasMaxLength(120)
                .HasColumnName("brand_name");
            entity.Property(e => e.CategoryId)
                .HasMaxLength(20)
                .HasColumnName("category_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.ImageUrls)
                .HasColumnType("jsonb")
                .HasColumnName("image_urls");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ListPrice)
                .HasPrecision(18, 2)
                .HasColumnName("list_price");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.RatingAvg)
                .HasPrecision(3, 2)
                .HasColumnName("rating_avg");
            entity.Property(e => e.RatingCount).HasColumnName("rating_count");
            entity.Property(e => e.ReturnDays).HasColumnName("return_days");
            entity.Property(e => e.ReviewSummary).HasColumnName("review_summary");
            entity.Property(e => e.ReviewSummaryUpdatedAt).HasColumnName("review_summary_updated_at");
            entity.Property(e => e.SalePrice)
                .HasPrecision(18, 2)
                .HasColumnName("sale_price");
            entity.Property(e => e.ShortDescription)
                .HasMaxLength(500)
                .HasColumnName("short_description");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.TechnicalSpecs)
                .HasColumnType("jsonb")
                .HasColumnName("technical_specs");
            entity.Property(e => e.WarrantyMonths).HasColumnName("warranty_months");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_products_categories_category_id");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_products_brands_brand_id");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_brands");

            entity.ToTable("brands");

            entity.HasIndex(e => e.Slug, "ix_brands_slug").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("('BRA_'::text || lpad((nextval('bra_id_seq'::regclass))::text, 3, '0'::text))")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .HasColumnName("name");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<ProductBadge>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.Label }).HasName("pk_product_badges");

            entity.ToTable("product_badges");

            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Label)
                .HasMaxLength(50)
                .HasColumnName("label");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductBadges)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_product_badges_products_product_id");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_product_variants");

            entity.ToTable("product_variants");

            entity.HasIndex(e => e.ProductId, "ix_product_variants_product_id");

            entity.HasIndex(e => e.Sku, "ix_product_variants_sku").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("('VAR_'::text || lpad((nextval('var_id_seq'::regclass))::text, 3, '0'::text))")
                .HasColumnName("id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("sku");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.ListPrice)
                .HasPrecision(18, 2)
                .HasColumnName("list_price");
            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0)
                .HasColumnName("stock_quantity");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.ImageUrls).HasColumnName("image_urls");
            entity.Property(e => e.Attributes)
                .HasColumnType("jsonb")
                .HasColumnName("attributes");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_product_variants_products_product_id");
        });

        modelBuilder.Entity<ProductEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_product_embeddings");

            entity.ToTable("product_embeddings");

            entity.HasIndex(e => e.ProductId, "ix_product_embeddings_product_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContentChunk).HasColumnName("content_chunk");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductEmbeddings)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_product_embeddings_products_product_id");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_reviews");

            entity.ToTable("reviews");

            entity.HasIndex(e => e.ParentId, "ix_reviews_parent_id");

            entity.HasIndex(e => e.ProductId, "ix_reviews_product_id");

            entity.HasIndex(e => e.UserId, "ix_reviews_user_id");

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("('REV_'::text || lpad((nextval('rev_id_seq'::regclass))::text, 3, '0'::text))")
                .HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.IsStaffReply).HasColumnName("is_staff_reply");
            entity.Property(e => e.ParentId)
                .HasMaxLength(20)
                .HasColumnName("parent_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId)
                .HasMaxLength(450)
                .HasColumnName("user_id");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_reviews_reviews_parent_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_reviews_products_product_id");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_reviews_users_user_id");
        });

        // User & Role configuration handled by IdentityDbContext

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_vouchers");

            entity.ToTable("vouchers");

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .HasDefaultValueSql("('VOU_'::text || lpad((nextval('vou_id_seq'::regclass))::text, 3, '0'::text))")
                .HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'fixed'::character varying")
                .HasColumnName("discount_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountValue)
                .HasPrecision(18, 2)
                .HasColumnName("discount_value");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.MaxDiscountAmount)
                .HasPrecision(18, 2)
                .HasColumnName("max_discount_amount");
            entity.Property(e => e.MinOrderValue)
                .HasPrecision(18, 2)
                .HasColumnName("min_order_value");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.UsedCount).HasColumnName("used_count");
        });
        modelBuilder.HasSequence<int>("bra_id_seq");
        modelBuilder.HasSequence<int>("cat_id_seq");
        modelBuilder.HasSequence<int>("order_id_seq");
        modelBuilder.HasSequence<int>("perm_id_seq");
        modelBuilder.HasSequence<int>("prod_id_seq");
        modelBuilder.HasSequence<int>("product_embeddings_id_seq");
        modelBuilder.HasSequence<int>("rev_id_seq");
        modelBuilder.HasSequence<int>("role_id_seq");
        modelBuilder.HasSequence<int>("user_id_seq");
        modelBuilder.HasSequence<int>("var_id_seq");
        modelBuilder.HasSequence<int>("vou_id_seq");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
