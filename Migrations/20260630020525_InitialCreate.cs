using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TMDTStore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discount_type", "fixed,percentage")
                .Annotation("Npgsql:Enum:order_status", "pending,paid,shipping,completed,cancelled_auto,cancelled_by_user")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateSequence<int>(
                name: "cat_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "order_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "perm_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "prod_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "product_embeddings_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "rev_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "role_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "user_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "vou_id_seq");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "('CAT_'::text || lpad((nextval('cat_id_seq'::regclass))::text, 3, '0'::text))"),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "('PERM_'::text || lpad((nextval('perm_id_seq'::regclass))::text, 2, '0'::text))"),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "('ROLE_'::text || lpad((nextval('role_id_seq'::regclass))::text, 2, '0'::text))"),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "('USER_'::text || lpad((nextval('user_id_seq'::regclass))::text, 3, '0'::text))"),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lockout_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    email_confirmation_token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email_confirmation_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vouchers",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "('VOU_'::text || lpad((nextval('vou_id_seq'::regclass))::text, 3, '0'::text))"),
                    code = table.Column<string>(type: "text", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    min_order_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    used_count = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vouchers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "('PROD_'::text || lpad((nextval('prod_id_seq'::regclass))::text, 3, '0'::text))"),
                    category_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    list_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sale_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    short_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    technical_specs = table.Column<string>(type: "jsonb", nullable: true),
                    review_summary = table.Column<string>(type: "text", nullable: true),
                    review_summary_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    image_urls = table.Column<string>(type: "jsonb", nullable: true),
                    badge_labels = table.Column<List<string>>(type: "text[]", nullable: true),
                    brand_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    rating_avg = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    rating_count = table.Column<int>(type: "integer", nullable: false),
                    warranty_months = table.Column<int>(type: "integer", nullable: true),
                    return_days = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    permission_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    role_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "('ORD_'::text || lpad((nextval('order_id_seq'::regclass))::text, 3, '0'::text))"),
                    user_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_address = table.Column<string>(type: "text", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voucher_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_orders_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inventories",
                columns: table => new
                {
                    product_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    stock_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reserved_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventories", x => x.product_id);
                    table.ForeignKey(
                        name: "fk_inventories_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_badges",
                columns: table => new
                {
                    product_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_badges", x => new { x.product_id, x.label });
                    table.ForeignKey(
                        name: "fk_product_badges_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_embeddings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    content_chunk = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_embeddings", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_embeddings_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "('REV_'::text || lpad((nextval('rev_id_seq'::regclass))::text, 3, '0'::text))"),
                    product_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    user_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    parent_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    rating = table.Column<short>(type: "smallint", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: false),
                    is_staff_reply = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviews_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reviews_reviews_parent_id",
                        column: x => x.parent_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    product_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => new { x.order_id, x.product_id });
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_status_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_by_user_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_status_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_status_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_status_history_changed_by_user_id",
                table: "order_status_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_status_history_order_id",
                table: "order_status_history",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_voucher_id",
                table: "orders",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_embeddings_product_id",
                table: "product_embeddings",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_parent_id",
                table: "reviews",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_product_id",
                table: "reviews",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_user_id",
                table: "reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventories");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "order_status_history");

            migrationBuilder.DropTable(
                name: "product_badges");

            migrationBuilder.DropTable(
                name: "product_embeddings");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "vouchers");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropSequence(
                name: "cat_id_seq");

            migrationBuilder.DropSequence(
                name: "order_id_seq");

            migrationBuilder.DropSequence(
                name: "perm_id_seq");

            migrationBuilder.DropSequence(
                name: "prod_id_seq");

            migrationBuilder.DropSequence(
                name: "product_embeddings_id_seq");

            migrationBuilder.DropSequence(
                name: "rev_id_seq");

            migrationBuilder.DropSequence(
                name: "role_id_seq");

            migrationBuilder.DropSequence(
                name: "user_id_seq");

            migrationBuilder.DropSequence(
                name: "vou_id_seq");
        }
    }
}
