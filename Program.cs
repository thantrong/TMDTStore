using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using Microsoft.AspNetCore.Identity;
using TMDTStore.Services.Email;
using TMDTStore.Services.Cloudinary;
using TMDTStore.Services.Cart;
using TMDTStore.Services.Banking;
using TMDTStore.Services;
using TMDTStore.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<StoreDbContext>()
    .AddDefaultTokenProviders();

var emailSetting = builder.Configuration.GetSection("EmailSettings").Get<EmailSetting>()
    ?? throw new InvalidOperationException("EmailSettings not configured");
builder.Services.AddSingleton<IEmailService>(new EmailService(emailSetting));

builder.Services.Configure<CloudinarySetting>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder.Services.Configure<VietQrSetting>(builder.Configuration.GetSection("VietQr"));
builder.Services.AddScoped<IVietQrService, VietQrService>();

builder.Services.AddHostedService<OrderCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "product_pagination",
    pattern: "{category}/Page{page:int}",
    defaults: new { controller = "Product", action = "Index", page = 1 });

app.MapControllerRoute(
    name: "product_category",
    pattern: "{category}",
    defaults: new { controller = "Product", action = "Index", page = 1 },
    constraints: new { category = @"^(?!Account$|Cart$|Checkout$|Order$|Auth$|Home$|Product$|Admin$|css$|js$|lib$|images$|favicon\.ico$).+$" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    await DbInitializer.InitializeAsync(userManager, roleManager, context);
}

app.Run();
