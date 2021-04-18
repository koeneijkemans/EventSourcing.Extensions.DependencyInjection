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
        /// <param name="scanForHandlers">Boolean value indicating to scan the current application domain for command handler instances.</param>
        /// <returns>The extended <see cref="IServiceCollection" /></returns>
        public static IServiceCollection AddEventSourcing(this IServiceCollection services, bool scanForHandlers = false)
        {
            services.AddTransient<StateConnector>();
            services.AddTransient<CommandHandler>();
            services.AddSingleton<EventPublisher, EventPublisher>();

            if (scanForHandlers)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                ScanForCommandHandlers(services, assemblies);
            }

            return services;
        }

        /// <summary>
        /// Scans the given assemblies for implementations of <see cref="ICommandHandler{T}" /> and registers them as a transient type.
        /// </summary>
        /// <param name="services">The service collection to extend</param>
        /// <param name="assemblies">The assemblies to scan</param>
        /// <returns>The extended <see cref="IServiceCollection"/></returns>
        public static IServiceCollection ScanForCommandHandlers(this IServiceCollection services, params Assembly[] assemblies)
        {
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
