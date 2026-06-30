using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using Microsoft.AspNetCore.Identity;
using TMDTStore.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();
builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<StoreDbContext>()
    .AddDefaultTokenProviders();
var emailSetting = builder.Configuration.GetSection("EmailSettings").Get<EmailSetting>()
    ?? throw new InvalidOperationException("EmailSettings not configured");
builder.Services.AddSingleton<IEmailService>(new EmailService(emailSetting));
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
