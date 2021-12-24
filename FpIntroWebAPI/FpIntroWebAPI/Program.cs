using System.Reflection;
using FpIntroWebAPI.Security;
using Unity;
using Unity.Microsoft.DependencyInjection;

var app = BuilderPrimer.CreateApp();

app.Run();

public static class BuilderPrimer
{
    public static WebApplication CreateApp(IUnityContainer? container = null)
    {
        var app = CreateBuilder(container).Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    public static WebApplicationBuilder CreateBuilder(IUnityContainer? container = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseUnityServiceProvider(container ?? GetUnityContainer());
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        // Needed for the functional tests. app.MapControllers() uses the calling assembly, not finding the controllers
        var assembly = Assembly.GetExecutingAssembly();
        builder.Services.AddMvcCore().AddApplicationPart(assembly).AddRazorViewEngine();
        return builder;
    }

    public static IUnityContainer GetUnityContainer() =>
        new UnityContainer()
            .RegisterSingleton<ISecurityService, SecurityService>();
}
