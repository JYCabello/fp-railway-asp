using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeFuncto.Assertions;
using Flurl.Http;
using Xunit;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Tests.API.WeatherForecast;

public class Gets
{
    public static readonly string JohnToken = "C7F558BA-4E7B-4521-B963-2D402CCD26C6";
    public static readonly string FrankToken = "E43C80F5-62B7-424E-86E3-56BBA8F14793";
    public static readonly string UnknownToken = "EEB41A50-F3E1-4F78-B371-54BDF3EA93D1";

    [Fact(DisplayName = "John has no permission, so he gets unauthorized")]
    public Task JohnTokenFails() =>
        Try(async () =>
            {
                using var server = new TestServer();
                await server.Get<IEnumerable<FpIntroWebAPI.WeatherForecast>>(
                    "weatherforecast",
                    new Dictionary<string, string> { { "token", JohnToken } }
                );
                return unit;
            })
            .ShouldBeError(ex =>
            {
                Assert.IsType<FlurlHttpException>(ex);
                var fex = (ex as FlurlHttpException)!;
                Assert.Equal(401, fex.StatusCode);
                return unit;
            });

    [Fact(DisplayName = "An unknown token gets unauthorized")]
    public Task UnknownTokenUnauthorized() =>
        Try(async () =>
            {
                using var server = new TestServer();
                await server.Get<IEnumerable<FpIntroWebAPI.WeatherForecast>>(
                    "weatherforecast",
                    new Dictionary<string, string> { { "token", UnknownToken } }
                );
                return unit;
            })
            .ShouldBeError(ex =>
            {
                Assert.IsType<FlurlHttpException>(ex);
                var fex = (ex as FlurlHttpException)!;
                Assert.Equal(401, fex.StatusCode);
                return unit;
            });

    [Fact(DisplayName = "Frank should get five results with a token")]
    public Task FrankTokenGetsFive() =>
        Try(async () =>
            {
                using var server = new TestServer();
                return await server.Get<IEnumerable<FpIntroWebAPI.WeatherForecast>>(
                    "weatherforecast",
                    new Dictionary<string, string> { { "token", FrankToken } }
                );
            })
            .ShouldBeOk(forecasts =>
            {
                Assert.Equal(5, forecasts.Count());
                return unit;
            });
}
