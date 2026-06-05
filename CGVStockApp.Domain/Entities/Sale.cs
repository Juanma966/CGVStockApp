using CGVStockApp.Domain.Common;
using CGVStockApp.Domain.Entities;
using CGVStockApp.Domain.Enums;

namespace CGVStockApp.Domain.Entities;

public class Sale : AuditableEntity
{
    public DateTime SaleDate { get; set; }
    public CustomerType CustomerType { get; set; }
    public PaymentMethodType PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}