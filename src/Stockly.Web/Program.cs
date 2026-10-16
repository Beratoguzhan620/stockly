using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Stockly.Application;
using Stockly.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Every POST form is checked for an antiforgery token.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddStocklyData(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddStocklyServices();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<Stockly.Web.Infrastructure.ImageStorage>();
builder.Services.AddScoped<Stockly.Web.Infrastructure.Lookups>();

var app = builder.Build();

await app.Services.PrepareDatabaseAsync(seedDemoData: app.Configuration.GetValue<bool>("Seed:DemoContent"));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

var turkish = new CultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkish),
    SupportedCultures = [turkish],
    SupportedUICultures = [turkish],
});

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

// Product photos uploaded at runtime are not part of the build-time static asset manifest.
app.UseStaticFiles();
app.UseRouting();

app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
