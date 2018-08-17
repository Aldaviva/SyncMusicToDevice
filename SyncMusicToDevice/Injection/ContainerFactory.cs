using System;
using System.Reflection;
using Autofac;

namespace SyncMusicToDevice.Injection
{
    internal static class ContainerFactory
    {
        internal static IContainer CreateContainer()
        {
            var containerBuilder = new ContainerBuilder();
            Assembly assembly = Assembly.GetExecutingAssembly();
            
            containerBuilder.RegisterAssemblyTypes(assembly)
                .Where(t => t.GetCustomAttribute<ComponentAttribute>() != null)
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope()
                .OnActivated(eventArgs =>
                {
                    eventArgs.Instance.GetType()
                        .GetMethod("PostConstruct", new Type[0])?
                        .Invoke(eventArgs.Instance, new object[0]);
                });

            containerBuilder.RegisterAssemblyModules(assembly);

            return containerBuilder.Build();
        }
    }
}