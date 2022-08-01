using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using IdGen;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Reflection;
using Wilgysef.Stalk.Application;
using Wilgysef.Stalk.Core;
using Wilgysef.Stalk.Core.Shared;
using Wilgysef.Stalk.EntityFrameworkCore;

namespace Wilgysef.Stalk.TestBase
{
    public class BaseTest
    {
        private static Type[] DependsOn = new[]
        {
            typeof(ApplicationModule),
            typeof(CoreModule),
        };

        private const int IdGeneratorId = 1;

        private DbConnection? _connection;

        public T? GetService<T>() where T : notnull
        {
            var provider = GetProvider();
            return provider.GetService<T>();
        }

        public T GetRequiredService<T>() where T : notnull
        {
            var provider = GetProvider();
            return provider.GetRequiredService<T>();
        }

        private IServiceProvider GetProvider()
        {
            var provider = new AutofacServiceProviderFactory();
            var services = new ServiceCollection();

            if (_connection == null)
            {
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                using var context = new StalkDbContext(new DbContextOptionsBuilder<StalkDbContext>()
                    .UseSqlite(_connection)
                    .Options);
                context.Database.EnsureCreated();
            }

            services.AddDbContext<StalkDbContext>(opt =>
            {
                opt.UseSqlite(_connection);
            });

            var builder = new ContainerBuilder();
            builder.Populate(services);

            var assembly = Assembly.GetExecutingAssembly();

            builder.RegisterAutoMapper(true, ServiceRegistration.GetAssemblies(assembly).ToArray());

            builder.Register(c => new IdGenerator(IdGeneratorId, IdGeneratorOptions.Default))
                .As<IIdGenerator<long>>()
                .SingleInstance();

            foreach (var (implementation, service) in ServiceRegistration.GetTransientServiceImplementations(assembly))
            {
                builder.RegisterType(implementation)
                    .As(service)
                    .PropertiesAutowired()
                    .InstancePerDependency();
            }

            foreach (var (implementation, service) in ServiceRegistration.GetScopedServiceImplementations(assembly))
            {
                builder.RegisterType(implementation)
                    .As(service)
                    .PropertiesAutowired()
                    .InstancePerLifetimeScope();
            }

            foreach (var (implementation, service) in ServiceRegistration.GetSingletonServiceImplementations(assembly))
            {
                builder.RegisterType(implementation)
                    .As(service)
                    .PropertiesAutowired()
                    .SingleInstance();
            }

            return provider.CreateServiceProvider(builder);
        }
    }
}