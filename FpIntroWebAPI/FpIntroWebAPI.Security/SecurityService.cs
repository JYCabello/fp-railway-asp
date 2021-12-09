using DeFuncto;
using DeFuncto.Extensions;
using Microsoft.Extensions.Primitives;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Security;


public static class SecurityService
{
    private static readonly User John = new User("John", Role.Nobody);
    private static readonly User Frank = new User("Frank", Role.Admin);
    private static readonly User Pete = new User("Pete", Role.Admin);


    private static readonly Dictionary<Guid, User> byToken = new()
    {
        { Guid.Parse("C7F558BA-4E7B-4521-B963-2D402CCD26C6"), John },
        { Guid.Parse("E43C80F5-62B7-424E-86E3-56BBA8F14793"), Frank },
        { Guid.Parse("519CD0A5-65F6-47AA-9931-A87946920BF8"), Pete },
    };

    private static readonly Dictionary<string, User> byUserNamePassword = new Dictionary<string, User>
    {
        { "frankpete", Frank },
        { "petepete", Pete },
    };

    public static Option<Du<Guid, (string, string)>> GetIdentifier(IDictionary<string, StringValues> dict)
    {
        try
        {
            if (dict.ContainsKey("token"))
                return dict["token"].Apply(s => Guid.Parse(s))!.Apply(First<Guid, (string, string)>);

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
                            guid => byToken[guid]!,
                            (tuple) => byUserNamePassword[$"{tuple.Item1}{tuple.Item2}"]!
                        )
                );
        }
        catch
        {
            return None;
        }
    }

    public static Option<SeeForecastPermission> CanSeeForecast(User user) =>
        user.Role == Role.Admin ? new SeeForecastPermission() : None;

    public static Task<int> GetNumberOfResults(User user) =>
        Task.FromResult(user.Name == "Pete" ? 10 : 5);
}

public interface IPermissionToken { }

public record SeeForecastPermission : IPermissionToken
{
    internal SeeForecastPermission() { }
}

public enum Role
{
    Admin,
    Nobody
}

public record User
{
    public string Name { get; private init; }
    public Role Role { get; private init; }

    internal User(string name, Role role)
    {
        Name = name;
        Role = role;
    }
}
