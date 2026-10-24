using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Motorsports.Common;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ExcludeFromDiScanAttribute : Attribute;

public static partial class Extensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers every non-abstract implementation of <typeparamref name="T"/> (as itself and as T).</summary>
        public IServiceCollection AddAll<T>(ServiceLifetime lifetime, params Assembly[] assemblies)
        {
            var service = typeof(T);
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (
                        type.IsAbstract
                        || type.IsInterface
                        || type.IsGenericTypeDefinition
                        || !service.IsAssignableFrom(type)
                        || type.IsDefined(typeof(ExcludeFromDiScanAttribute), false)
                    )
                    {
                        continue;
                    }

                    services.TryAdd(new ServiceDescriptor(type, type, lifetime));
                    services.TryAddEnumerable(
                        new ServiceDescriptor(service, sp => sp.GetRequiredService(type), lifetime)
                    );
                }
            }

            return services;
        }
    }
}
