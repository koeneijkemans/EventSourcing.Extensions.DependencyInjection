using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kei.EventSourcing.ServiceCollectionExtension
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Adds the default setup for event sourcing to the service collection.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The extended <see cref="IServiceCollection" /></returns>
        public static IServiceCollection AddEventSourcing(this IServiceCollection services)
        {
            services.AddTransient<StateConnector>();
            services.AddTransient<CommandHandler>();
            services.AddTransient<EventPublisher, EventPublisher>();

            // Scan assemblies for classes that implement ICommandHandler
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                var commandHandlerTypes = assembly
                    .GetTypes()
                    .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableFrom(typeof(ICommandHandler<>))))
                    .ToList();

                foreach (var commandHandlerType in commandHandlerTypes)
                {
                    List<Type> allGenericTypes = commandHandlerType.GetInterfaces().SelectMany(i => i.GetGenericArguments()).ToList();

                    foreach (var genericType in allGenericTypes)
                    {
                        var serviceType = typeof(ICommandHandler<>).MakeGenericType(genericType);

                        services.AddTransient(serviceType, commandHandlerType);
                    }
                }
            }

            return services;
        }
    }
}
