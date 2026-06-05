using CGVStockApp.Domain.Common;

namespace CGVStockApp.Domain.Entities;

public class Category : AuditableEntity
{
    public string Name { get; set;} = string.Empty;
    public string? Description { get; set; }
    public ICollection<Subcategory> Subcategories { get; set;} = new List<Subcategory>();

    public ICollection<Product> Products { get; set;} = new List<Product>();
}