using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeFuncto.Assertions;
using Flurl.Http;
using FpIntroWebAPI.Controllers;
using Xunit;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Tests.API.WeatherForecast;

public class Gets
{
    [Fact(DisplayName = "No token gets you unauthorized")]
    public Task NoTokenUnauthorized() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.GetFailure<WeatherForecastController.ErrorResult>("weatherforecast");
            })
            .ShouldBeOk(er =>
            {
                Assert.Equal(401, er.code);
                Assert.Contains("Missing token in the headers", er.result.Message);
                return unit;
            });

    [Fact(DisplayName = "John has no permission, so he gets unauthorized via token")]
    public Task JohnTokenFails() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.GetFailure<WeatherForecastController.ErrorResult>(
                    "weatherforecast",
                    new Dictionary<string, string> { { "token", "C7F558BA-4E7B-4521-B963-2D402CCD26C6" } }
                );
            })
            .ShouldBeOk(er =>
            {
                Assert.Equal(401, er.code);
                Assert.Contains("User John does not have role Admin", er.result.Message);
                return unit;
            });

    [Fact(DisplayName = "John has no permission, so he gets unauthorized via password")]
    public Task JohnPasswordFails() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.GetFailure<WeatherForecastController.ErrorResult>(
                    "weatherforecast",
                    new Dictionary<string, string>
                    {
                        { "username", "john" },
                        { "password", "frank" }
                    }
                );
            })
            .ShouldBeOk(er =>
            {
                Assert.Equal(401, er.code);
                Assert.Contains("User John does not have role Admin", er.result.Message);
                return unit;
            });

    [Fact(DisplayName = "An unknown username and password gets unauthorized")]
    public Task UnknownUsernamePasswordUnauthorized() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.GetFailure<WeatherForecastController.ErrorResult>(
                    "weatherforecast",
                    new Dictionary<string, string>
                    {
                        { "username", "not" },
                        { "password", "there" }
                    }
                );
            })
            .ShouldBeOk(er =>
            {
                Assert.Equal(401, er.code);
                Assert.Contains("Username and password combination was incorrect", er.result.Message);
                return unit;
            });

    [Fact(DisplayName = "An unknown token gets unauthorized")]
    public Task UnknownTokenUnauthorized() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.GetFailure<WeatherForecastController.ErrorResult>(
                    "weatherforecast",
                    new Dictionary<string, string> { { "token", "EEB41A50-F3E1-4F78-B371-54BDF3EA93D1" } }
                );
            })
            .ShouldBeOk(er =>
            {
                Assert.Equal(401, er.code);
                Assert.Contains("Token was not recognized", er.result.Message);
                return unit;
            });

    [Fact(DisplayName = "Frank should get five results with a token")]
    public Task FrankTokenGetsFive() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.Get<IEnumerable<FpIntroWebAPI.WeatherForecast>>(
                    "weatherforecast",
                    new Dictionary<string, string> { { "token", "E43C80F5-62B7-424E-86E3-56BBA8F14793" } }
                );
            })
            .ShouldBeOk(forecasts =>
            {
                Assert.Equal(5, forecasts.Count());
                return unit;
            });

    [Fact(DisplayName = "Frank should get five results with password")]
    public Task FrankPasswordGetsFive() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.Get<IEnumerable<FpIntroWebAPI.WeatherForecast>>(
                    "weatherforecast",
                    new Dictionary<string, string>
                    {
                        { "username", "frank" },
                        { "password", "pete" }
                    }
                );
            })
            .ShouldBeOk(forecasts =>
            {
                Assert.Equal(5, forecasts.Count());
                return unit;
            });

    [Fact(DisplayName = "Pete should get ten results with a token")]
    public Task PeteTokenGetsMore() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.Get<IEnumerable<FpIntroWebAPI.WeatherForecast>>(
                    "weatherforecast",
                    new Dictionary<string, string> { { "token", "519CD0A5-65F6-47AA-9931-A87946920BF8" } }
                );
            })
            .ShouldBeOk(forecasts =>
            {
                Assert.Equal(10, forecasts.Count());
                return unit;
            });

    [Fact(DisplayName = "Pete should get ten results with password")]
    public Task PetePasswordGetsMore() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.Get<IEnumerable<FpIntroWebAPI.WeatherForecast>>(
                    "weatherforecast",
                    new Dictionary<string, string>
                    {
                        { "username", "pete" },
                        { "password", "pete" }
                    }
                );
            })
            .ShouldBeOk(forecasts =>
            {
                Assert.Equal(10, forecasts.Count());
                return unit;
            });
}
