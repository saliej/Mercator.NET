namespace Mercator;

internal sealed class MappingConfiguration
{
    public required Type SourceType { get; init; }
    public required Type DestinationType { get; init; }

    /// <summary>
    /// Explicit per-member configurations keyed by dot-separated destination path.
    /// Multiple BindMember / BindPath calls for the same path accumulate into one entry.
    /// </summary>
    public Dictionary<string, MemberMappingConfig> MemberMappings { get; } = new();

    /// <summary>
    /// Value transforms registered via Transform. Applied in declaration order after
    /// each member value is resolved, before it is written to the destination.
    /// </summary>
    public List<(Type ValueType, Func<object?, object?> Transform)> Transforms { get; } = new();

    /// <summary>
    /// When true, the mapper also registers a convention-only reverse mapping
    /// from DestinationType back to SourceType.
    /// </summary>
    public bool HasReverse { get; set; }

    /// <summary>
    /// Callbacks to invoke after all property assignments are complete.
    /// Stored as untyped delegates to enable uniform storage and invocation.
    /// </summary>
    public List<Action<object, object>> AfterMapActions { get; } = new();
}
