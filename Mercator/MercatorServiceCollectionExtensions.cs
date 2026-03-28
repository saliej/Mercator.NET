namespace Mercator;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Mercator with the DI container.</summary>
public static class MercatorServiceCollectionExtensions
{
    /// <summary>
    /// Scans <paramref name="assemblies"/> for concrete <see cref="MappingRegistry"/> subclasses,
    /// instantiates each one, collects all <see cref="MappingConfiguration"/> entries, and
    /// registers a singleton <see cref="IMapper"/> backed by the compiled mappings.
    /// </summary>
    public static IServiceCollection AddMercator(
        this IServiceCollection services,
        params Assembly[] assemblies)
        => AddMercator(services, configure: null, assemblies);

    /// <summary>
    /// Scans <paramref name="assemblies"/> for concrete <see cref="MappingRegistry"/> subclasses and
    /// registers a singleton <see cref="IMapper"/>. Accepts an optional <paramref name="configure"/>
    /// delegate to set <see cref="MercatorOptions"/> such as <see cref="ValidationMode"/>.
    /// </summary>
    public static IServiceCollection AddMercator(
        this IServiceCollection services,
        Action<MercatorOptions>? configure,
        params Assembly[] assemblies)
    {
        var options = new MercatorOptions();
        configure?.Invoke(options);

        var configurations = assemblies
            .Distinct()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(MappingRegistry)))
            .Select(t => (MappingRegistry)Activator.CreateInstance(t, nonPublic: true)!)
            .SelectMany(p => p.Configurations)
            .ToList();

        services.AddSingleton<IMapper>(new MercatorMapper(configurations, options));
        return services;
    }
}
