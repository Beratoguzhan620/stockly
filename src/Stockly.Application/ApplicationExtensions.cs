using Microsoft.Extensions.DependencyInjection;
using Stockly.Application.Categories;
using Stockly.Application.Dashboard;

namespace Stockly.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddStocklyServices(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
