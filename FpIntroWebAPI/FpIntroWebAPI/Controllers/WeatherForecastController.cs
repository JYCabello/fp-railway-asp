using DeFuncto;
using DeFuncto.Extensions;
using FpIntroWebAPI.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Controllers;


public class MyData {}

public record User2(bool IsActive, string Username)
{
    public bool HasRole(string roleName) => true;
}


[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    public static Result<string, MyError> GetKey(IDictionary<string, StringValues> dict) =>
        dict.ContainsKey("api-key") ? dict["api-key"].ToString() : MyError.KeyMissing;

    public static  Result<User2, MyError> GetActiveUser(string key)
    {
        User2? user = GetUser(key);
        if (user == null)
            return MyError.KeyInvalid;
        return user.IsActive ? user : MyError.UserInactive(user.Username);
    }

    public static  Result<Unit, MyError> HasRole(User2 user, string role) =>
        user.HasRole(role) ? unit : MyError.PermissionMissing(user.Username, role);

    public static  Result<MyData, MyError> GetMyData(int id)
    {
        MyData? data = Fetch(id);
        return data != null ? MyError.EntityNotFound($"Could not find data with id {id}") : data;
    }

    [HttpGet("getdata", Name = "GetData")]
    public IActionResult GetData(int id) =>
    (
        from key in GetKey(Request.Headers)
        from user in GetActiveUser(key)
        from isAuthorized in HasRole(user, "data-getter")
        from data in GetMyData(id)
        select data
    ).Match(Ok, Handle);

    private static  MyData? Fetch(int id)
    {
        throw new NotImplementedException();
    }

    private static  User2? GetUser(string key)
    {
        throw new NotImplementedException();
    }

    public class ErrorResult
    {
        public ErrorResult(string message) =>
            Message = message;

        public string Message { get; }
    }

    protected IActionResult Handle(MyError error) =>
        error.Value.Match<IActionResult>(
            _ => Unauthorized(new ErrorResult("Missing key in the headers")),
            _ => Unauthorized(new ErrorResult("Key was not recognized")),
            inactive => Unauthorized(new ErrorResult($"User {inactive.Username} is inactive")),
            mising => Unauthorized(new ErrorResult($"User {mising.Username} does not have role {mising.Role}")),
            notFound => NotFound(new ErrorResult(notFound.Message))
        );


    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> logger;
    private readonly ISecurityService securityService;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, ISecurityService securityService)
    {
        this.logger = logger;
        this.securityService = securityService;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public Task<ActionResult<IEnumerable<WeatherForecast>>> Get() =>
    (
        from user in securityService.GetUser(Request.Headers).Apply(Lift)
        from forecastToken in securityService.CanSeeForecast(user).Apply(opt => Lift(opt, user))
        from numberOfResults in securityService.GetNumberOfResults(user).Apply(Lift)
        select GetForecast(forecastToken, numberOfResults)
    ).Apply(EdgeOfTheWorld);

    /*
     * The goal of this function is to show what linq does to do the binding.
     */
    [HttpGet("getuglylinq", Name = "GetWeatherForecastUgly")]
    public Task<ActionResult<IEnumerable<WeatherForecast>>> GetButDoingLinqsJob() =>
        securityService.GetUser(Request.Headers).Apply(Lift)
            .Bind(user => securityService.CanSeeForecast(user).Apply(opt => Lift(opt, user)).Map(token => (user, token)))
            .Bind(tuple => securityService.GetNumberOfResults(tuple.user).Apply(Lift).Map(num => (tuple.token, num)))
            .Map(tuple => GetForecast(tuple.token, tuple.num))
            .Apply(EdgeOfTheWorld);

    /*
     * The goal of this function is to do the binding in an "ugly" way, so it becomes obvious that those expressions are a bind.
     */
    [HttpGet("getugly", Name = "GetWeatherForecastUglyLinq")]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetButUgly()
    {
        // ReSharper disable SuggestVarOrType_Elsewhere
        Option<User> maybeUser = securityService.GetUser(Request.Headers);

        Option<SeeForecastPermissionToken> maybeToken = maybeUser.Bind(user => securityService.CanSeeForecast(user));

        Result<int, Errors> maybeNumberOfResult = await maybeUser.Match(
            user => securityService.GetNumberOfResults(user).Map(Ok<int, Errors>),
            () => new Errors.Unauthorized("User was not found").Apply(Error<int, Errors>).Apply(Task.FromResult)
        );

        Result<SeeForecastPermissionToken, Errors> tokenOrError =
            maybeToken.Match(
                Ok<SeeForecastPermissionToken, Errors>,
                () => Error<Errors>(new Errors.Unauthorized("Unautorized to forecast"))
            );

        // Not actually being used, it's just showing that matching an option to Ok in Some and Error in None is a case
        // common enough that it warrants a helper function (Result).
        // ReSharper disable once UnusedVariable
        Result<SeeForecastPermissionToken, Errors> tokenOrErrorAlternative =
            maybeToken.Result<Errors>(() => new Errors.Unauthorized("Unautorized to forecast"));

        Result<(SeeForecastPermissionToken, int), Errors> dataToGetOrError =
            maybeNumberOfResult.Bind(num => tokenOrError.Map(token => (token, num)));

        Result<IEnumerable<WeatherForecast>, Errors> maybeForecast =
            dataToGetOrError.Map(tuple => GetForecast(tuple.Item1, tuple.Item2));
        // ReSharper restore SuggestVarOrType_Elsewhere

        return maybeForecast.Apply(EdgeOfTheWorldSync);
    }

    // ReSharper disable once UnusedParameter.Local
    private static IEnumerable<WeatherForecast> GetForecast(SeeForecastPermissionToken _, int numberOfResults) =>
        Enumerable
            .Range(1, numberOfResults)
            .Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

    private async Task<ActionResult<T>> EdgeOfTheWorld<T>(AsyncResult<T, Errors> result) =>
        await result.Match(t => Ok(t), MapError<T>);

    private ActionResult<T> EdgeOfTheWorldSync<T>(Result<T, Errors> result) =>
        result.Match(t => Ok(t), MapError<T>);

    private ActionResult<T> MapError<T>(Errors error) =>
        error switch
        {
            Errors.Unauthorized err => OnUnauthorized<T>(err),
            _ => throw new ArgumentException($"Unhandled error type {error.GetType().Name}", nameof(error))
        };

    private ActionResult<T> OnUnauthorized<T>(Errors.Unauthorized error)
    {
        logger.LogError("Someone tried to access the system\n{Message}", error.Message);
        return Unauthorized();
    }

    public static AsyncResult<T, Errors> Lift<T>(Option<T> option, User user) where T : IPermissionToken =>
        option.Result<Errors>(() => new Errors.Unauthorized($"Error trying to get a permissiontoken of type {typeof(T).Name} for user {user.Name}"));

    public static AsyncResult<User, Errors> Lift(Option<User> option) =>
        option.Result<Errors>(() => new Errors.Unauthorized("Could not find the user with the given credentials"));

    public static AsyncResult<T, Errors> Lift<T>(Task<T> task) =>
        task.Map(Ok<T, Errors>);
}

public record Errors
{
    public record Unauthorized(string Message) : Errors;
}
