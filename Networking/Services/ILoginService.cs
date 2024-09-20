using Library.Types;
using Library.Util;

namespace Networking.Services;

public interface ILoginService
{
    Result ValidateToken(string token);
    Result IsLoggedIn(Session session);
    Result IsLoggedIn(string token);

    Result Login(Session session, string token);
    Result Logout(Session session);
}