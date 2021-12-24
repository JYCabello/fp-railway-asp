using DeFuncto;
using DeFuncto.Extensions;
using FpIntroWebAPI.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Controllers;


[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    public class ErrorResult
    {
        public ErrorResult(string message) =>
            Message = message;

        public string Message { get; }
    }

    protected IActionResult Handle(MyError error) =>
        error.Value.Match<ActionResult>(
            _ => Unauthorized(new ErrorResult("Missing token in the headers")),
            _ => Unauthorized(new ErrorResult("Token was not recognized")),
            _ => Unauthorized(new ErrorResult("Username and password combination was incorrect")),
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
    public Task<IActionResult> Get() =>
    (
        from user
            in securityService.GetUser(Request.Headers).Apply(Lift)
        from forecastToken
            in securityService
                .CanSeeForecast(user)
                .Result(() => MyError.PermissionMissing(user.Name, "SeeForecast"))
                .Async()
        from numberOfResults
            in securityService
                .GetNumberOfResults(user)
                .Apply(Lift)
        select GetForecast(forecastToken, numberOfResults)
    ).Match(Ok, Handle);

    public AsyncResult<User, MyError> Lift(Result<User, CredentialsFailed> result) =>
        result.MapError(Translate).Async();

    /*
     * The goal of this function is to show what linq does to do the binding.
     */
    [HttpGet("getuglylinq", Name = "GetWeatherForecastUgly")]
    public Task<IActionResult> GetButDoingLinqsJob() =>
        securityService.GetUser(Request.Headers).MapError(Translate).Async()
            .Bind(user =>
                securityService
                    .CanSeeForecast(user)
                    .Result(() => MyError.PermissionMissing(user.Name, "SeeForecast"))
                    .Map(token => (user, token))
            )
            .Bind(tuple => securityService.GetNumberOfResults(tuple.user).Apply(Lift).Map(num => (tuple.token, num)))
            .Map(tuple => GetForecast(tuple.token, tuple.num))
            .Match(Ok, Handle);

    /*
     * The goal of this function is to do the binding in an "ugly" way, so it becomes obvious that those expressions are a bind.
     */
    [HttpGet("getugly", Name = "GetWeatherForecastUglyLinq")]
    public async Task<IActionResult> GetButUgly()
    {
        // ReSharper disable SuggestVarOrType_Elsewhere
        Result<User, MyError> maybeUser = securityService.GetUser(Request.Headers).MapError(Translate);

        Result<SeeForecastPermissionToken, MyError> maybeToken =
            maybeUser
                .Bind(user =>
                    securityService
                        .CanSeeForecast(user)
                        .Result(() => MyError.PermissionMissing(user.Name, "SeeForecast"))
                );

        Result<int, MyError> maybeNumberOfResult =
            await maybeUser
                .Async()
                .Bind(user => securityService.GetNumberOfResults(user).Map(Ok<int, MyError>))
                .ToTask();

        Result<(SeeForecastPermissionToken, int), MyError> dataToGetOrError =
            maybeNumberOfResult.Bind(num => maybeToken.Map(token => (token, num)));

        Result<IEnumerable<WeatherForecast>, MyError> maybeForecast =
            dataToGetOrError.Map(tuple => GetForecast(tuple.Item1, tuple.Item2));
        // ReSharper restore SuggestVarOrType_Elsewhere

        return maybeForecast.Match(Ok, Handle);
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

    public static AsyncResult<T, MyError> Lift<T>(Task<T> task) =>
        task.Map(Ok<T, MyError>);

    public static MyError Translate(CredentialsFailed creds) =>
        creds switch
        {
            CredentialsFailed.None => MyError.KeyMissing,
            CredentialsFailed.Token => MyError.KeyInvalid,
            CredentialsFailed.UsernamePassword => MyError.UsernamePasswordInvalid,
            _ => throw new ArgumentOutOfRangeException(nameof(creds), creds, null)
        };
}
