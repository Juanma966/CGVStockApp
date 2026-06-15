using CGVStockApp.Application.Common.Interfaces;
using CGVStockApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CGVStockApp.Infrastructure.Persistance.Context;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AccountingMovement> AccountingMovements => Set<AccountingMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly( typeof(ApplicationDbContext).Assembly);
    }
}