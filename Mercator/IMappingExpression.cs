namespace Mercator;

using System.Linq.Expressions;

/// <summary>
/// Fluent API for configuring a mapping from <typeparamref name="TSource"/>
/// to <typeparamref name="TDestination"/>.
/// Convention-based property matching (same name, assignable type) is applied automatically;
/// use <see cref="BindMember{TMember}(Expression{Func{TDestination,TMember}},Action{IMemberOptions{TSource,TMember}})"/> and <see cref="BindPath{TMember}"/> to override or extend it.
/// </summary>
public interface IMappingExpression<TSource, TDestination>
{
    /// <summary>
    /// Configures an explicit mapping for a single top-level destination property.
    /// Multiple calls targeting the same property accumulate, so <see cref="IMemberOptions{TSource,TMember}.When"/>
    /// and <see cref="IMemberOptions{TSource,TMember}.From"/> may be supplied in separate chained calls.
    /// </summary>
    IMappingExpression<TSource, TDestination> BindMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberOptions<TSource, TMember>> configure);

    /// <summary>
    /// Convenience overload combining <c>When</c> and <c>From</c> in a single call.
    /// Equivalent to calling <see cref="BindMember{TMember}(Expression{Func{TDestination, TMember}}, Action{IMemberOptions{TSource,TMember}})"/>
    /// with both <see cref="IMemberOptions{TSource,TMember}.When"/> and <see cref="IMemberOptions{TSource,TMember}.From"/>.
    /// </summary>
    IMappingExpression<TSource, TDestination> BindMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Func<TSource, bool> when,
        Func<TSource, TMember> from);

    /// <summary>
    /// Configures an explicit mapping for a nested destination member path
    /// (e.g. <c>dest => dest.RequestInfo.RequestName</c>).
    /// Intermediate objects that are <c>null</c> at runtime are instantiated via
    /// <see cref="Activator.CreateInstance(Type)"/> before the value is assigned.
    /// </summary>
    IMappingExpression<TSource, TDestination> BindPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationPath,
        Action<IMemberOptions<TSource, TMember>> configure);

    /// <summary>
    /// Registers a value transform applied to every mapped value whose type is assignable
    /// to <typeparamref name="TValue"/>. The transform runs after each member is resolved
    /// but before it is written to the destination. The most common use is normalising
    /// empty strings to <c>null</c>:
    /// <code>
    /// .Transform&lt;string&gt;(s => string.IsNullOrEmpty(s) ? null! : s)
    /// </code>
    /// </summary>
    IMappingExpression<TSource, TDestination> Transform<TValue>(Func<TValue, TValue> transform);

    /// <summary>
    /// Registers a convention-based reverse mapping from <typeparamref name="TDestination"/>
    /// back to <typeparamref name="TSource"/>. Only same-name, assignable-type properties are
    /// copied; explicit <see cref="BindMember{TMember}(Expression{Func{TDestination,TMember}},Action{IMemberOptions{TSource,TMember}})"/> / <see cref="BindPath{TMember}"/>
    /// configurations are not inverted.
    /// </summary>
    IMappingExpression<TSource, TDestination> AndReverse();
    
    /// <summary>
    /// Suppresses all mapping for the specified destination member.
    /// Convention-based copying is skipped and any prior BindMember configuration is discarded.
    /// </summary>
    IMappingExpression<TSource, TDestination> IgnoreMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember);

    /// <summary>
    /// Registers a callback to invoke after all property assignments are complete.
    /// The callback receives the typed source and destination instances.
    /// Multiple <c>AfterMap</c> calls accumulate and execute in declaration order.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action);

    /// <summary>
    /// Configures a collection property mapping where each source element is mapped
    /// to a destination element through a registered element mapping.
    /// </summary>
    IMappingExpression<TSource, TDestination> BindCollection<TSourceElement, TDestElement>(
        Expression<Func<TDestination, List<TDestElement>?>> destinationMember,
        Func<TSource, IEnumerable<TSourceElement>?> sourceSelector);
}
