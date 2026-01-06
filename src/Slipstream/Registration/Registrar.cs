using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Slipstream.Abstractions;

namespace Slipstream.Registration
{
    /// <summary>
    /// Minimal container-agnostic registrar abstraction. Implement this against your container
    /// (or use the delegate-based overloads) to register request handlers and pipeline behaviors.
    /// </summary>
    public interface IRegistrar
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
        /// Scan the supplied assemblies and register all implementations of <see cref="IRequestHandler{TRequest, TResponse}"/>
        /// and <see cref="IPipelineBehavior{TRequest, TResponse}"/> using the provided registrar.
        /// Handlers are registered as single services; behaviors are registered as collection entries.
        /// </summary>
        public static void RegisterHandlersAndBehaviors(this IRegistrar registrar, params Assembly[] assemblies)
        {
            RegisterHandlersAndBehaviors(registrar, (IEnumerable<Assembly>)assemblies);
        }

        public static void RegisterHandlersAndBehaviors(this IRegistrar registrar, IEnumerable<Assembly> assemblies)
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

                    if (def == typeof(IRequestHandler<,>))
                    {
                        registrar.Register(iface, impl);
                    }
                    else if (def == typeof(IPipelineBehavior<,>))
                    {
                        registrar.RegisterCollection(iface, impl);
                    }
                }
            }
        }

        /// <summary>
        /// Helper that accepts simple delegates for registration so you can use any container API.
        /// Example: for MS DI pass in (service, impl) => services.AddTransient(service, impl)
        /// and for collections call services.AddTransient(service, impl) for each behavior.
        /// </summary>
        public static void RegisterHandlersAndBehaviors(this Action<Type, Type> register,
            Action<Type, Type> registerCollection,
            params Assembly[] assemblies)
        {
            if (register is null) throw new ArgumentNullException(nameof(register));
            if (registerCollection is null) throw new ArgumentNullException(nameof(registerCollection));

            var registrar = new DelegateRegistrar(register, registerCollection);
            // call the other extension to perform scanning and registration
            registrar.RegisterHandlersAndBehaviors((IEnumerable<Assembly>)assemblies);
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
