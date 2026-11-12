using Infrastructure.AutoMapper;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DI;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection via user-secrets or environment variables.");

        // DbContextFactory prevents concurrent use of the same DbContext by separate Blazor events.
        services.AddDbContextFactory<DataContext>(options => options.UseNpgsql(connection));
        services.AddAutoMapper(typeof(InfrastructureProfile));
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ITableService, TableService>();
        services.AddScoped<IReservationService, ReservationService>();
        return services;
    }
}
