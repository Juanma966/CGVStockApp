using CGVStockApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CGVStockApp.Infrastructure.Persistance.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductCode).HasMaxLength(10).IsRequired();

        builder.HasIndex(x => x.ProductCode).IsUnique();
        
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.PublicPrice).HasPrecision(18,2);

        builder.Property(x => x.WholesalePrice).HasPrecision(18,2);

        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Subcategory).WithMany().HasForeignKey(x => x.Subcategory).OnDelete(DeleteBehavior.Restrict);
        

        
        
    }
}