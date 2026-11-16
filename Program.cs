using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("ShopSphere")
    ?? throw new InvalidOperationException("The 'ShopSphere' connection string is not configured.");

builder.Services.AddDbContext<ShopSphereDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ShopSphereDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ShoppingCartService>();
builder.Services.AddSingleton<GuestCartCookie>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "ShopSphere.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    if (app.Environment.IsDevelopment())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ShopSphereDbContext>();
        await DevelopmentDataSeeder.SeedAsync(dbContext);
    }

    await IdentityDataSeeder.InitializeAsync(
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        app.Configuration,
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityInitialization"));
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
