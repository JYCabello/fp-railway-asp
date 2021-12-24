using DeFuncto;
using DeFuncto.Extensions;
using Microsoft.Extensions.Primitives;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Security;

public enum CredentialsFailed
{
    None,
    Token,
    UsernamePassword
}

public interface ISecurityService
{
    Result<User, CredentialsFailed> GetUser(IDictionary<string, StringValues> dict);
    Option<SeeForecastPermissionToken> CanSeeForecast(User user);
    Task<int> GetNumberOfResults(User user);
}

public class SecurityService : ISecurityService
{
    private static readonly User John = new("John", Role.Nobody);
    private static readonly User Frank = new("Frank", Role.Admin);
    private static readonly User Pete = new("Pete", Role.Admin);


    private static readonly Dictionary<Guid, User> ByToken = new()
    {
        { Guid.Parse("C7F558BA-4E7B-4521-B963-2D402CCD26C6"), John },
        { Guid.Parse("E43C80F5-62B7-424E-86E3-56BBA8F14793"), Frank },
        { Guid.Parse("519CD0A5-65F6-47AA-9931-A87946920BF8"), Pete }
    };

    private static readonly Dictionary<string, User> ByUserNamePassword = new()
    {
        { "frankpete", Frank },
        { "petepete", Pete },
        { "johnfrank", John }
    };

    public static Result<Du<Guid, (string un, string pw)>, CredentialsFailed> GetIdentifier(IDictionary<string, StringValues> dict)
    {
        try
        {
            if (dict.ContainsKey("token"))
                return dict["token"].Apply(s => Guid.Parse(s)).Apply(First<Guid, (string, string)>);

            string userName = dict["username"]!;
            string password = dict["password"]!;
            return (userName, password).Apply(Second<Guid, (string, string)>);
        }
        catch
        {
            return CredentialsFailed.None;
        }
    }

    public Result<User, CredentialsFailed> GetUser(IDictionary<string, StringValues> dict) =>
        GetIdentifier(dict)
            .Bind(
                du =>
                    du.Match<Result<User, CredentialsFailed>>(
                        guid =>
                            ByToken.ContainsKey(guid)
                                ? ByToken[guid]
                                : CredentialsFailed.Token,
                        tuple =>
                            ByUserNamePassword.ContainsKey($"{tuple.un}{tuple.pw}")
                                ? ByUserNamePassword[$"{tuple.un}{tuple.pw}"]
                                : CredentialsFailed.UsernamePassword
                    )
            );

    public Option<SeeForecastPermissionToken> CanSeeForecast(User user) =>
        user.Role == Role.Admin ? new SeeForecastPermissionToken() : None;

    public Task<int> GetNumberOfResults(User user) =>
        Task.FromResult(user.Name == "Pete" ? 10 : 5);
}

public interface IPermissionToken { }

public record SeeForecastPermissionToken : IPermissionToken
{
    internal SeeForecastPermissionToken() { }
}

public enum Role
{
    Admin,
    Nobody
}

public record User
{
    internal User(string name, Role role)
    {
        Name = name;
        Role = role;
    }

    public string Name { get; private init; }
    public Role Role { get; private init; }
}
