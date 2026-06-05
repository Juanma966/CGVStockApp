using CGVStockApp.Domain.Common;
using CGVStockApp.Domain.Exceptions;

namespace CGVStockApp.Domain.Entities;

public class Product : AuditableEntity
{
    public string ProductCode { get; set;} = string.Empty;
    public string Name { get; set;} = string.Empty;
    public string? Description { get; set;}
    public decimal PublicPrice { get; set; }
    public decimal WholesalePrice { get; set;}
    public int InitialStock { get; set; }
    public int AvailableStock { get; set;}
    public int AlertStock { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set;}
    public int? SubcategoryId { get; set; }
    public Subcategory? Subcategory { get; set;}
    public void DecreaseStock(int quantity)
    {
        if (quantity > AvailableStock)
        {
            throw new InsufficientStockException($"Stock insuficiente para producto {Name}. Stock disponible: {AvailableStock}");
        }
        AvailableStock -= quantity;
    }
    public void IncreaseStock ( int quantity)
    {
        AvailableStock += quantity;
    }

}