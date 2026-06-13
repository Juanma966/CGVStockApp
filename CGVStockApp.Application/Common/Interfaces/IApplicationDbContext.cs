using CGVStockApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CGVStockApp.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Subcategory> Subcategories { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleDetail> SaleDetails { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<AccountingMovement> AccountingMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
}