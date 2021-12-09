using System;
using System.Collections.Generic;
using System.Linq;
using DeFuncto;
using DeFuncto.Extensions;
using FpIntroWebAPI.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public Task<ActionResult<IEnumerable<WeatherForecast>>> Get() =>
    (
        from user in SecurityService.GetUser(Request.Headers).Apply(Lift)
        from forecastToken in SecurityService.CanSeeForecast(user).Apply(opt => Lift(opt, user))
        from numberOfResults in SecurityService.GetNumberOfResults(user).Apply(Lift)
        select GetForecast(forecastToken, numberOfResults)
    ).Apply(EdgeOfTheWorld);

    private static IEnumerable<WeatherForecast> GetForecast(SeeForecastPermission _, int numberOfResults) =>
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

    private ActionResult<T> MapError<T>(Errors error) =>
        error switch
        {
            Errors.Unauthorized err => OnUnauthorized<T>(err),
            _ => throw new ArgumentException($"Unhandled error type {error.GetType().Name}", nameof(error))
        };

    private ActionResult<T> OnUnauthorized<T>(Errors.Unauthorized error)
    {
        _logger.LogError($"Someone tried to access the system\n{error.Message}");
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
