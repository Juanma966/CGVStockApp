using CGVStockApp.Domain.Common;
using CGVStockApp.Domain.Enums;

namespace CGVStockApp.Domain.Entities;

public class AccountingMovement : AuditableEntity
{
    public DateTime MovementDate { get; set; }
    public AccountingMovementType MovementType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsIncome { get; set; }
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;



}