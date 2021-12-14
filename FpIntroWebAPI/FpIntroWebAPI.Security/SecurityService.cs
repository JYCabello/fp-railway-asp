using DeFuncto;
using DeFuncto.Extensions;
using Microsoft.Extensions.Primitives;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Security;

public static class SecurityService
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
        { "petepete", Pete }
    };

    public static Option<Du<Guid, (string, string)>> GetIdentifier(IDictionary<string, StringValues> dict)
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
            return None;
        }
    }

    public static Option<User> GetUser(IDictionary<string, StringValues> dict)
    {
        try
        {
            return GetIdentifier(dict)
                .Map(
                    du =>
                        du.Match(
                            guid => ByToken[guid],
                            tuple => ByUserNamePassword[$"{tuple.Item1}{tuple.Item2}"]
                        )
                );
        }
        catch
        {
            return None;
        }
    }

    public static Option<SeeForecastPermissionToken> CanSeeForecast(User user) =>
        user.Role == Role.Admin ? new SeeForecastPermissionToken() : None;

    public static Task<int> GetNumberOfResults(User user) =>
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
