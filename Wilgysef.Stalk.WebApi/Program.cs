using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Extensions.Logging;
using Wilgysef.Stalk.Application;
using Wilgysef.Stalk.Application.ServiceRegistrar;
using Wilgysef.Stalk.Core.Shared.Options;
using Wilgysef.Stalk.EntityFrameworkCore;
using Wilgysef.Stalk.WebApi.Middleware;

var responseExceptions = true;

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseExceptionHandler(new ExceptionHandler
{
    ExceptionsInResponse = responseExceptions,
}.GetExceptionHandlerOptions());

app.Use(new LoggingHandler(logger).HandleAsync);

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var appStartup = scope.ServiceProvider.GetRequiredService<Startup>();
    await appStartup.StartAsync(app.Services.GetAutofacRoot());
}

app.Run();

void ConfigureServices()
{
    builder.Services.AddAutofac();

    builder.Services.AddControllers();

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
    {
        var connectionString = context.Configuration.GetConnectionString("Default");

        var extractorsOptions = GetOptions<ExtractorsOptions>(context.Configuration);

        var serviceRegistrar = new ServiceRegistrar(
            new DbContextOptionsBuilder<StalkDbContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                .UseLoggerFactory(loggerFactory)
                .EnableSensitiveDataLogging()
                .Options,
            logger,
            extractorsOptions.Path,
            t => GetOptionsByType(t, context.Configuration));
        serviceRegistrar.RegisterApplication(containerBuilder, builder.Services);
    });
}

void ConfigureSwagger()
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

void ConfigureConfiguration()
{
    builder.Host.ConfigureAppConfiguration((context, configuration) =>
    {
        configuration.Sources.Clear();

        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    });
}

T? GetOptions<T>(IConfiguration configuration) where T : IOptionSection
{
    var optionsInstance = Activator.CreateInstance<T>();
    return configuration.GetSection(optionsInstance.Label).Get<T>();
}

IOptionSection? GetOptionsByType(Type type, IConfiguration configuration)
{
    if (Activator.CreateInstance(type) is not IOptionSection optionsInstance)
    {
        throw new ArgumentException("Type must be assignable from IOptionSection.");
    }
    return (IOptionSection?)configuration.GetSection(optionsInstance.Label).Get(type);
}

public partial class Program { }
