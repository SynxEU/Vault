using Microsoft.EntityFrameworkCore;
using Vault.Components;
using Vault.Domain.Persistence;
using Vault.Service.Interfaces;
using Vault.Service.Security;
using Vault.Service.Services;
using Radzen;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Vault.Domain.Entities;
using Vault.Service.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

builder.Services.AddHttpClient();

// Radzen services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

builder.Services.AddScoped(sp =>
{
    var navigation =
        sp.GetRequiredService<NavigationManager>();

    return new HttpClient
    {
        BaseAddress = new Uri(navigation.BaseUri)
    };
});

var connectionString =
    builder.Configuration.GetConnectionString("VaultDb")
    ?? "Server=(localdb)\\mssqllocaldb;Database=VaultDb;Trusted_Connection=true;";

builder.Services.AddDbContextPool<VaultDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<VaultDbContext>()
    .AddSignInManager();


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            IdentityConstants.ApplicationScheme;

        options.DefaultChallengeScheme =
            IdentityConstants.ApplicationScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<IHashingService, HashingService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();
app.UseAntiforgery();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append(
        "X-Content-Type-Options",
        "nosniff");

    await next();
});

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var logger =
        scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();
    try
    {
        var db =
            scope.ServiceProvider
            .GetRequiredService<VaultDbContext>();

        await db.Database.MigrateAsync();

        logger.LogInformation(
            "Database migrated successfully.");
    }
    catch(Exception ex)
    {
        logger.LogError(
            ex,
            "Database migration failed.");
    }
}

app.Run();