using CGVStockApp.Domain.Common;
using CGVStockApp.Domain.Enums;

namespace CGVStockApp.Domain.Entities;

public class StockMovement : AuditableEntity
{
    public DateTime MovementDate { get; set; }
    public StockMovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

}