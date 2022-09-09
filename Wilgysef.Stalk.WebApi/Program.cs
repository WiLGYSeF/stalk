using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Polly;
using Serilog;
using Serilog.Extensions.Logging;
using Wilgysef.Stalk.Application;
using Wilgysef.Stalk.Application.HttpClientPolicies;
using Wilgysef.Stalk.Application.ServiceRegistrar;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.Core.Shared.Options;
using Wilgysef.Stalk.EntityFrameworkCore;
using Wilgysef.Stalk.WebApi.Extensions;
using Wilgysef.Stalk.WebApi.Middleware;
using Wilgysef.Stalk.WebApi.Options;

var loggerFactory = new SerilogLoggerFactory(new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
    .WriteTo.Debug(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
    .CreateLogger());
var logger = loggerFactory.CreateLogger("default");

var builder = WebApplication.CreateBuilder(args);

ConfigureConfiguration();
ConfigureServices();
ConfigureSwagger();

var app = builder.Build();
var appOptions = GetOptions<AppOptions>(app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

ConfigureExceptionHandler();
ConfigureLoggingHandler();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var appStartup = scope.ServiceProvider.GetRequiredService<Startup>();
    await appStartup.StartAsync(app.Services.GetAutofacRoot());
}

app.Run();

void ConfigureConfiguration()
{
    builder.Host.ConfigureAppConfiguration((context, configuration) =>
    {
        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);
    });
}

void ConfigureServices()
{
    builder.Services.AddControllers();

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    var connectionString = builder.Configuration.GetConnectionString("Default");
    var extractorsOptions = GetOptions<ExtractorsOptions>(builder.Configuration);

    var serviceRegistrar = new ServiceRegistrar();
    serviceRegistrar.RegisterServices(builder.Services);

    builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
    {
        serviceRegistrar.RegisterDbContext(
            containerBuilder,
            new DbContextOptionsBuilder<StalkDbContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                //.UseLoggerFactory(loggerFactory)
                //.EnableSensitiveDataLogging()
                .Options);

        serviceRegistrar.RegisterServices(
            containerBuilder,
            logger,
            extractorsOptions.Path,
            t => GetOptionsByType(t, builder.Configuration));
    });
}

void ConfigureSwagger()
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

void ConfigureExceptionHandler()
{
    var exceptionHandler = new ExceptionHandler
    {
        ExceptionsInResponse = appOptions.ExceptionsInResponse,
    };

    app.Configuration.RegisterRepeatChangeCallback(_ =>
    {
        var appOptions = GetOptions<AppOptions>(app.Configuration);
        exceptionHandler.ExceptionsInResponse = appOptions.ExceptionsInResponse;
    });

    app.UseExceptionHandler(exceptionHandler.GetExceptionHandlerOptions());
}

void ConfigureLoggingHandler()
{
    app.Use(new LoggingHandler(logger).HandleAsync);
}

T GetOptions<T>(IConfiguration configuration) where T : IOptionSection
{
    var optionsInstance = Activator.CreateInstance<T>();
    return configuration.GetSection(optionsInstance.Label).Get<T>() ?? optionsInstance;
}

IOptionSection GetOptionsByType(Type type, IConfiguration configuration)
{
    if (Activator.CreateInstance(type) is not IOptionSection optionsInstance)
    {
        throw new ArgumentException("Type must be assignable from IOptionSection.");
    }
    return (IOptionSection)(configuration.GetSection(optionsInstance.Label).Get(type) ?? optionsInstance);
}

public partial class Program { }
