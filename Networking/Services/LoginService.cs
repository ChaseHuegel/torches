using Library.Types;
using Library.Util;

namespace Networking.Services;

public class LoginService : ILoginService
{
    //  TODO implement login expiration
    private readonly Dictionary<Session, string> _logins = [];
    private readonly object _loginsLock = new();

    public Result ValidateToken(string token)
    {
        //  TODO Validate the token with an auth server
        return new Result(!string.IsNullOrWhiteSpace(token));
    }

    public Result IsLoggedIn(Session session)
    {
        lock (_loginsLock)
        {
            bool loginExists = _logins.ContainsKey(session);
            return new Result(loginExists);
        }
    }

    public Result IsLoggedIn(string token)
    {
        lock (_loginsLock)
        {
            //  TODO Get username from token claim and check against that
            bool loginExists = _logins.Values.Any(username => username == token);
            return new Result(loginExists);
        }
    }

    public Result Login(Session session, string token)
    {
        lock (_loginsLock)
        {
            if (IsLoggedIn(token))
            {
                return new Result(false, "Account is already logged in.");
            }

            if (IsLoggedIn(session))
            {
                return new Result(false, "Session is already logged into another account.");
            }

            if (!ValidateToken(token))
            {
                return new Result(false, "Invalid token.");
            }

            //  TODO Get username from token claim and map it to the session
            _logins.Add(session, token);
            return new Result(true);
        }
    }

    public Result Logout(Session session)
    {
        lock (_loginsLock)
        {
            if (!IsLoggedIn(session))
            {
                return new Result(false, "Session is not logged into an account.");
            }

            _logins.Remove(session);
            return new Result(true);
        }
    }
}
