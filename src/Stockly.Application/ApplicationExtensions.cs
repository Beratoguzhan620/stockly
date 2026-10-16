using Microsoft.Extensions.DependencyInjection;
using Stockly.Application.Categories;
using Stockly.Application.Dashboard;
using Stockly.Application.Products;
using Stockly.Application.Stock;
using Stockly.Application.Suppliers;

namespace Stockly.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddStocklyServices(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ProductService>();
        services.AddScoped<StockService>();
        services.AddScoped<SupplierService>();
        return services;
    }
}
