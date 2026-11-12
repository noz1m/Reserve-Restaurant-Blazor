using Blazor.Components;
using Infrastructure.Data;
using Infrastructure.DI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Initialize the database before serving requests. Never silently continue with a broken UI.
// Development enables this by default; production requires an explicit setting.
if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
    await using var db = await factory.CreateDbContextAsync();
    try
    {
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Restaurant database migrations completed.");
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex,
            "Database initialization failed. Check ConnectionStrings:DefaultConnection, PostgreSQL port, role privileges and migrations.");
        throw new InvalidOperationException(
            "Restaurant database is not ready. Verify PostgreSQL and the connection string. " +
            "For a new database, PostgreSQL user needs CREATE DATABASE privileges. " +
            "See README.md for setup and migration commands.", ex);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
