namespace TMDTStore.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

public static class DbInitializer
{
    public static async Task InitializeAsync(UserManager<User> userManager, RoleManager<Role> roleManager, StoreDbContext context)
    {
        await EnsureOrderItemsPrimaryKeyAsync(context);

        // Create roles if they don't exist
        string[] roles = new[] { "Admin", "Customer", "Staff" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new Role { Name = role });
            }
        }

        // Create an admin user if it doesn't exist
        var adminEmail = "admin@gmail.com";
        var adminPass = "Abc!23";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Admin",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, adminPass);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed brands
        if (!await context.Brands.AnyAsync())
        {
            var brands = new[]
            {
                new Brand { Id = "BRA_001", Name = "ASUS", Slug = "asus", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Id = "BRA_002", Name = "Lenovo", Slug = "lenovo", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Id = "BRA_003", Name = "Logitech", Slug = "logitech", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Id = "BRA_004", Name = "Dell", Slug = "dell", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Id = "BRA_005", Name = "Samsung", Slug = "samsung", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Id = "BRA_006", Name = "Apple", Slug = "apple", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Id = "BRA_007", Name = "Kingston", Slug = "kingston", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Id = "BRA_008", Name = "Intel", Slug = "intel", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Id = "BRA_009", Name = "AMD", Slug = "amd", IsActive = true, CreatedAt = DateTime.UtcNow },
            };
            context.Brands.AddRange(brands);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Đổi PK order_items từ (order_id, product_id) → (order_id, product_id, variant_id)
    /// để một đơn có thể chứa nhiều biến thể cùng sản phẩm.
    /// </summary>
    private static async Task EnsureOrderItemsPrimaryKeyAsync(StoreDbContext context)
    {
        try
        {
            // Chuẩn hóa NULL → '' (PK không chấp nhận NULL)
            await context.Database.ExecuteSqlRawAsync("""
                UPDATE order_items SET variant_id = '' WHERE variant_id IS NULL;
                """);

            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE order_items
                    ALTER COLUMN variant_id SET DEFAULT '',
                    ALTER COLUMN variant_id SET NOT NULL;
                """);

            // Đổi PK nếu đang là (order_id, product_id) cũ
            await context.Database.ExecuteSqlRawAsync("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.table_constraints
                        WHERE table_name = 'order_items'
                          AND constraint_type = 'PRIMARY KEY'
                          AND constraint_name = 'pk_order_items'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.key_column_usage
                        WHERE table_name = 'order_items'
                          AND constraint_name = 'pk_order_items'
                          AND column_name = 'variant_id'
                    ) THEN
                        ALTER TABLE order_items DROP CONSTRAINT pk_order_items;
                        ALTER TABLE order_items
                            ADD CONSTRAINT pk_order_items
                            PRIMARY KEY (order_id, product_id, variant_id);
                    END IF;
                END $$;
                """);
        }
        catch
        {
            // Không chặn startup nếu DB chưa có bảng / quyền hạn chế
        }
    }
}