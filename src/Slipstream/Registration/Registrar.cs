using Microsoft.Extensions.DependencyInjection;
using Slipstream.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Slipstream.Registration
{
    /// <summary>
    /// Minimal container-agnostic registrar abstraction. Implement this against your container
    /// (or use the delegate-based overloads) to register request handlers and pipeline behaviors.
    /// </summary>
    internal interface IRegistrar
    {
        /// <summary>Register a single service -> implementation mapping.</summary>
        void Register(Type service, Type implementation);

        /// <summary>Register an implementation as part of a service collection (multi-registration).</summary>
        void RegisterCollection(Type service, Type implementation);
    }

    internal sealed class DelegateRegistrar : IRegistrar
    {
        private readonly Action<Type, Type> _register;
        private readonly Action<Type, Type> _registerCollection;

        public DelegateRegistrar(Action<Type, Type> register, Action<Type, Type> registerCollection)
        {
            _register = register ?? throw new ArgumentNullException(nameof(register));
            _registerCollection = registerCollection ?? throw new ArgumentNullException(nameof(registerCollection));
        }

        public void Register(Type service, Type implementation) => _register(service, implementation);

        public void RegisterCollection(Type service, Type implementation) => _registerCollection(service, implementation);
    }

    public static class RegistrarExtensions
    {
        /// <summary>
        /// Adds Slipstream request dispatcher services and registers request handlers from the specified assemblies to the
        /// dependency injection container.
        /// </summary>
        /// <remarks>This method registers the Slipstream request dispatcher and all discovered request
        /// handlers as transient services. Call this method during application startup to enable Slipstream-based event
        /// handling.</remarks>
        /// <param name="services">The service collection to which Slipstream services and request handlers will be added.</param>
        /// <param name="assemblies">An array of assemblies to scan for request handler registrations. Each assembly is searched for types to
        /// register as request handlers.</param>
        /// <returns>The same service collection instance, enabling method chaining.</returns>
        public static IServiceCollection AddSlipstream(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            var registrar = new DelegateRegistrar(
                (service, impl) => services.AddTransient(service, impl),
                (service, impl) => services.AddTransient(service, impl)
            );
            registrar.RegisterHandlers((IEnumerable<Assembly>)assemblies);
            services.AddTransient<IDispatcher, SlipstreamDispatcher>();
            return services;
        }

        /// <summary>
        /// Scan the supplied assemblies and register all implementations of <see cref="IRequestHandler{TRequest, TResponse}"/>
        /// using the provided registrar.
        /// Handlers are registered as single services.
        /// </summary>
        private static void RegisterHandlers(this IRegistrar registrar, IEnumerable<Assembly> assemblies)
        {
            if (registrar is null) throw new ArgumentNullException(nameof(registrar));
            if (assemblies is null) throw new ArgumentNullException(nameof(assemblies));

            var types = assemblies
                .Where(a => a != null)
                .SelectMany(a => SafeGetTypes(a))
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToArray();

            foreach (var impl in types)
            {
                var interfaces = impl.GetInterfaces().Where(i => i.IsGenericType).ToArray();

                foreach (var iface in interfaces)
                {
                    var def = iface.GetGenericTypeDefinition();

                    if (def == typeof(IRequestHandler<,>) || def == typeof(IRequestHandler<>))
                    {
                        registrar.Register(iface, impl);
                    }                    
                }
            }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
        }
    }
}
