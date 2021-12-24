using DeFuncto;

namespace FpIntroWebAPI;

public class MyError
{
    public readonly Du5<KeyMissing, KeyInvalid, UserInactive, PermissionMissing, EntityNotFound> Value;
    public MyError(Du5<KeyMissing, KeyInvalid, UserInactive, PermissionMissing, EntityNotFound> value) =>
        Value = value;

    public static MyError KeyMissing => new(new KeyMissing());
    public static MyError KeyInvalid => new(new KeyInvalid());
    public static MyError UserInactive(string username) => new(new UserInactive(username));
    public static MyError PermissionMissing(string username, string role) => new(new PermissionMissing(username, role));
    public static MyError EntityNotFound(string message) => new(new EntityNotFound(message));
}

public class KeyMissing { }
public class KeyInvalid { }

public class UserInactive
{
    public UserInactive(string username) =>
        Username = username;
    public string Username { get; }
}
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

public class EntityNotFound
{
    public EntityNotFound(string message) =>
        Message = message;
    public string Message { get; }
}
