using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Hosting;
using Unity;
using static Flurl.GeneratedExtensions;


namespace FpIntroWebAPI.Tests;

public class TestServer : IDisposable
{
    private static readonly SemaphoreSlim Sm = new(1);

    private static readonly Random Rn = new();

    private readonly IHost host;
    private readonly string url;

    public TestServer()
    {
        int port = GetPort();
        url = $"https://localhost:{port}/";
        var webApp = BuilderPrimer.CreateApp(TestLocator.Get<IUnityContainer>());
        webApp.Urls.Add(url);
        webApp.Start();
        host = webApp;
    }

    private static int RandomPort => Rn.Next(30_000) + 10_000;

    public void Dispose() =>
        host.Dispose();

    private static TcpConnectionInformation[] GetConnectionInfo() =>
        IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

    private static int GetPort()
    {
        Sm.Wait();
        int port = Go(RandomPort);
        Sm.Release();
        return port;

        static int Go(int portNumber) =>
            GetConnectionInfo().Any(ci => ci.LocalEndPoint.Port == portNumber)
                ? Go(RandomPort)
                : portNumber;
    }

    private IFlurlRequest BaseReq(string path, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null)
    {
        var request = url
            .AppendPathSegment(path)
            .WithHeaders(headers ?? new Dictionary<string, string>());

        return (queryParams ?? new Dictionary<string, string>())
            .ToList()
            .Aggregate(
                request,
                (acc, kvp) => acc.SetQueryParam(kvp.Key, kvp.Value)
            );
    }

    public Task<T> Get<T>(string path, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null) =>
        BaseReq(path, headers, queryParams).GetJsonAsync<T>();

    public async Task<(int code, T result)> GetFailure<T>(string path, Dictionary<string, string>? headers = null, Dictionary<string, string>? queryParams = null)
    {
        var response = await BaseReq(path, headers, queryParams)
            .AllowAnyHttpStatus()
            .GetAsync();
        var t = await response.GetJsonAsync<T>();
        var code = response.StatusCode;
        return (code, t);
    }
}
