namespace Mercator;

/// <summary>
/// Configuration options for a single destination member within a mapping definition.
/// Multiple calls for the same member accumulate, allowing <see cref="When"/> and
/// <see cref="From"/> to be chained as separate <c>BindMember</c> / <c>BindPath</c> calls.
/// </summary>
public interface IMemberOptions<TSource, TMember>
{
    /// <summary>
    /// Resolves the destination member value from the source object using the provided delegate.
    /// Supports constants (<c>_ => "Salesforce"</c>), renames (<c>src => src.Id__c</c>),
    /// computed values (<c>_ => Guid.NewGuid()</c>), and helper calls.
    /// </summary>
    void From(Func<TSource, TMember> resolver);

    /// <summary>
    /// Skips this member entirely when the condition evaluates to <c>false</c>.
    /// The destination member retains its default value on skip.
    /// </summary>
    void When(Func<TSource, bool> condition);
    
    /// <summary>
    /// Prevents this member from being mapped entirely.
    /// Suppresses both convention-based and any previously registered From/When configuration.
    /// </summary>
    void Ignore();
}
