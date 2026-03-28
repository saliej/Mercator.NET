namespace Mercator;

/// <summary>
/// Maps a source object to a new instance of the specified destination type.
/// Implementations must be safe to use as a singleton.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Creates a new <typeparamref name="TDestination"/> and populates it from <paramref name="source"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no mapping has been registered for the runtime type of <paramref name="source"/>
    /// to <typeparamref name="TDestination"/>.
    /// </exception>
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Populates an existing <paramref name="destination"/> instance from <paramref name="source"/>.
    /// The same mapping configuration applies as for <see cref="Map{TDestination}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no mapping has been registered for the runtime type of <paramref name="source"/>
    /// to <typeparamref name="TDestination"/>.
    /// </exception>
    TDestination MapInto<TDestination>(object source, TDestination destination);
}
