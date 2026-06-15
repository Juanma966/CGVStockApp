namespace CGVStockApp.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken( int userId, string username, string role);
}