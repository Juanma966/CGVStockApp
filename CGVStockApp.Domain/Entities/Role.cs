using CGVStockApp.Domain.Common;
using CGVStockApp.Domain.Enums;

namespace CGVStockApp.Domain.Entities;

public class Role : AuditableEntity
{
    public string Name  { get; set; } = string.Empty;
    public RoleType Type { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}