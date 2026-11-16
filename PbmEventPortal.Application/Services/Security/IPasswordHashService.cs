namespace PbmEventPortal.Application.Services.Security;

public interface IPasswordHashService
{
    string HashPassword(string password);

    bool VerifyPassword(string passwordHash, string providedPassword);
}