using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeFuncto.Assertions;
using Flurl.Http;
using Xunit;
using static DeFuncto.Prelude;

namespace FpIntroWebAPI.Tests.API.WeatherForecast;

public class Gets
{
    public static readonly string JohnToken = "C7F558BA-4E7B-4521-B963-2D402CCD26C6";

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
}
