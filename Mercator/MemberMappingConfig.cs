namespace Mercator;

internal sealed class MemberMappingConfig
{
    public required string DestinationPath { get; init; }

    /// <summary>
    /// Optional guard: when not null and returning false the member is skipped entirely.
    /// Set by When; the boxed source object is passed at runtime.
    /// </summary>
    public Func<object, bool>? Condition { get; set; }
   
    /// <summary>
    /// When true, this member is unconditionally skipped during mapping.
    /// Takes precedence over Condition and Resolver.
    /// </summary>
    public bool IsIgnored { get; set; }

    /// <summary>
    /// Value factory called with the boxed source object.
    /// When null and IsIgnored is false, this is a When-only entry.
    /// </summary>
    public Func<object, object?>? Resolver { get; set; }

    /// <summary>
    /// When true, this member is a collection mapping configured via BindCollection.
    /// </summary>
    public bool IsCollection { get; set; }

    /// <summary>
    /// Element type of the source collection for BindCollection mappings.
    /// </summary>
    public Type? CollectionSourceElementType { get; set; }

    /// <summary>
    /// Element type of the destination collection for BindCollection mappings.
    /// </summary>
    public Type? CollectionDestElementType { get; set; }

    /// <summary>
    /// Selector to extract the source collection from the boxed source object.
    /// </summary>
    public Func<object, System.Collections.IEnumerable?>? CollectionSourceSelector { get; set; }
}
