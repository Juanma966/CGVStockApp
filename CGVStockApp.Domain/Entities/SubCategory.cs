using CGVStockApp.Domain.Common;

namespace CGVStockApp.Domain.Entities;

public class Subcategory : AuditableEntity
{
    public string Name { get; set;} = string.Empty;
    public string? Description { get; set;}
    public int CategoryId { get; set;}
    public Category Category { get; set;} = null!;
    public ICollection<Product> Products { get; set;} = new List<Product>();
}