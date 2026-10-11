using Microsoft.EntityFrameworkCore;
using ShopApp.MVC.Data.Entities;

namespace ShopApp.MVC.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories { get; set; }
}


