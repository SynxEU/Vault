using Microsoft.EntityFrameworkCore;
using Vault.Components;
using Vault.Domain.Persistence;
using Vault.Service.Interfaces;
using Vault.Service.Security;
using Vault.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("VaultDb") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=VaultDb;Trusted_Connection=true;";
builder.Services.AddDbContext<VaultDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Add Security Services
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();

// Add Business Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();

// Add Session support for user state management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseSession();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VaultDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
