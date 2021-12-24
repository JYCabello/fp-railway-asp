using Unity;
using Unity.Microsoft.DependencyInjection;

var app = BuilderPrimer.CreateBuilder().Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public static class BuilderPrimer
{
    public static WebApplicationBuilder CreateBuilder(IUnityContainer? container = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseUnityServiceProvider(container ?? GetUnityContainer());
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddMvcCore().AddRazorViewEngine();
        return builder;
    }

    public static IUnityContainer GetUnityContainer() =>
        new UnityContainer();
}
