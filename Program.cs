using FreePBXAIAssistant.Data;
using FreePBXAIAssistant.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddSingleton<SipService>();
builder.Services.AddSingleton<AzureSpeechService>();
builder.Services.AddSingleton<AzureOpenAIService>();
builder.Services.AddScoped<CallProcessor>();
builder.Services.AddScoped<DatabaseService>();

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue<int>("Security:SessionTimeoutMinutes", 60));
        options.SlidingExpiration = true;
    });

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
// Note: AddFile requires Serilog.Extensions.Logging.File package
// For now, we'll comment it out
// builder.Logging.AddFile("logs/app-{Date}.log");

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    // Seed default admin user if not exists
    var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    await databaseService.EnsureAdminUserExists();

    // Seed default AI settings
    await databaseService.EnsureDefaultSettingsExist();
}

// Start SIP service
var sipService = app.Services.GetRequiredService<SipService>();
_ = Task.Run(async () => await sipService.StartAsync());

// Configure the HTTP request pipeline
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
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();