using DeFuncto;

namespace FpIntroWebAPI;

public class MyError
{
    public readonly Du4<KeyMissing, KeyInvalid, UsernamePasswordInvalid, PermissionMissing> Value;
    public MyError(Du4<KeyMissing, KeyInvalid, UsernamePasswordInvalid, PermissionMissing> value) =>
        Value = value;

    public static MyError KeyMissing => new(new KeyMissing());
    public static MyError KeyInvalid => new(new KeyInvalid());
    public static MyError UsernamePasswordInvalid => new(new UsernamePasswordInvalid());
    public static MyError PermissionMissing(string username, string role) => new(new PermissionMissing(username, role));
}

public class KeyMissing { }
public class KeyInvalid { }
public class UsernamePasswordInvalid { }

public class PermissionMissing
{
    public PermissionMissing(string username, string role)
    {
        Username = username;
        Role = role;
    }
    public string Username { get; }
    public string Role { get; }
}
