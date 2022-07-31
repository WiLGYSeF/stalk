using Autofac;
using Autofac.Extensions.DependencyInjection;
using IdGen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        public static T? GetService<T>() where T : notnull
        {
            var provider = GetProvider();
            return provider.GetService<T>();
        }

        public static T GetRequiredService<T>() where T : notnull
        {
            var provider = GetProvider();
            return provider.GetRequiredService<T>();
        }

        private static IServiceProvider GetProvider()
        {
            var provider = new AutofacServiceProviderFactory();
            var services = new ServiceCollection();

            services.AddDbContext<StalkDbContext>(opt =>
            {
                opt.UseSqlite("DataSource=:memory:");
            });

            var builder = new ContainerBuilder();
            builder.Populate(services);

            builder.Register(c => new IdGenerator(IdGeneratorId, IdGeneratorOptions.Default))
                .As<IIdGenerator<long>>()
                .SingleInstance();

            var assembly = Assembly.GetExecutingAssembly();

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