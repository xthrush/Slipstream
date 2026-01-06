using System;
using Slipstream.Abstractions;

namespace Slipstream
{
    // These extension methods assume the consumer has Microsoft.Extensions.DependencyInjection available.
    // They remain here as convenience; if the DI package is not referenced compilation will fail in consumer project.
    public static class ServiceCollectionExtensions
    {
        public static object AddSlipstream(this object services)
        {
            // This method is a placeholder to avoid hard dependency in the core library.
            // Prefer to call AddSlipstream in an application project with Microsoft.Extensions.DependencyInjection.
            return services;
        }
    }
}