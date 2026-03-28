namespace Mercator;

/// <summary>
/// Base class for declaring type mappings. Subclass this and call
/// <see cref="Register{TSource,TDestination}"/> from the constructor.
/// MappingRegistries are discovered automatically by <see cref="MercatorServiceCollectionExtensions.AddMercator(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Reflection.Assembly[])"/>
/// via assembly scanning; they must have an accessible parameterless constructor.
/// </summary>
public abstract class MappingRegistry
{
    // Accessed by MercatorMapper during construction; not part of the public API.
    internal List<MappingConfiguration> Configurations { get; } = new();

    /// <summary>
    /// Declares a mapping from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
    /// Returns a fluent expression for adding per-member overrides, transforms, and reverse maps.
    /// Convention-based copying of same-named, assignable-typed properties is applied automatically
    /// before any explicit mappings run.
    /// </summary>
    protected IMappingExpression<TSource, TDestination> Register<TSource, TDestination>()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(TSource),
            DestinationType = typeof(TDestination)
        };
        Configurations.Add(config);
        return new MappingExpression<TSource, TDestination>(config);
    }
}
