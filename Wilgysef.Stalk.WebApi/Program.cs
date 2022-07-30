using Autofac;
using Autofac.Extensions.DependencyInjection;
using IdGen;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Reflection;
using Wilgysef.Stalk.EntityFrameworkCore;

const int IdGeneratorId = 1;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

ConfigureDbContext();
ConfigureSwagger();
ConfigureDependencyInjection();

var app = builder.Build();

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

void ConfigureDbContext()
{
    builder.Services.AddDbContext<StalkDbContext>(opt =>
    {
        opt.UseSqlite("DataSource=abc.db");
    });
}

void ConfigureSwagger()
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

void ConfigureDependencyInjection()
{
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    builder.Host.ConfigureContainer<ContainerBuilder>(builder =>
    {
        var asm = Assembly.GetExecutingAssembly();
        builder.RegisterAssemblyTypes(asm);
    });

    builder.Services.AddSingleton<IIdGenerator<long>>(new IdGenerator(IdGeneratorId, IdGeneratorOptions.Default));
}
