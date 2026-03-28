using System.Linq.Expressions;

namespace Mercator;

internal sealed class MappingExpression<TSource, TDestination>(MappingConfiguration configuration)
    : IMappingExpression<TSource, TDestination>
{
    public MappingConfiguration Configuration { get; } = configuration;
    public IMappingExpression<TSource, TDestination> BindMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberOptions<TSource, TMember>> configure)
        => ConfigureMember(ExtractPath(destinationMember), configure);

    public IMappingExpression<TSource, TDestination> BindMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Func<TSource, bool> when,
        Func<TSource, TMember> from)
        => ConfigureMember<TMember>(ExtractPath(destinationMember), opt =>
        {
            opt.When(when);
            opt.From(from);
        });

    public IMappingExpression<TSource, TDestination> BindPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationPath,
        Action<IMemberOptions<TSource, TMember>> configure)
        => ConfigureMember(ExtractPath(destinationPath), configure);

    public IMappingExpression<TSource, TDestination> IgnoreMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember)
        => ConfigureMember<TMember>(ExtractPath(destinationMember), opt => opt.Ignore());

    public IMappingExpression<TSource, TDestination> Transform<TValue>(Func<TValue, TValue> transform)
    {
        configuration.Transforms.Add(
            (typeof(TValue), v =>
            {
                if (v is null && !typeof(TValue).IsValueType)
                {
                    TValue nullValue = default!;
                    return transform(nullValue);
                }

                if (v is TValue typed)
                    return transform(typed);

                return v;
            }));
        return this;
    }

    public IMappingExpression<TSource, TDestination> AndReverse()
    {
        configuration.HasReverse = true;
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action)
    {
        configuration.AfterMapActions.Add((src, dest) =>
            action((TSource)src, (TDestination)dest));
        return this;
    }

    public IMappingExpression<TSource, TDestination> BindCollection<TSourceElement, TDestElement>(
        Expression<Func<TDestination, List<TDestElement>?>> destinationMember,
        Func<TSource, IEnumerable<TSourceElement>?> sourceSelector)
    {
        var path = ExtractPath(destinationMember);

        if (!configuration.MemberMappings.TryGetValue(path, out var memberConfig))
        {
            memberConfig = new MemberMappingConfig { DestinationPath = path };
            configuration.MemberMappings[path] = memberConfig;
        }

        memberConfig.IsCollection = true;
        memberConfig.CollectionSourceElementType = typeof(TSourceElement);
        memberConfig.CollectionDestElementType = typeof(TDestElement);
        memberConfig.CollectionSourceSelector = src => sourceSelector((TSource)src);

        return this;
    }

    private IMappingExpression<TSource, TDestination> ConfigureMember<TMember>(
        string path,
        Action<IMemberOptions<TSource, TMember>> configure)
    {
        if (!configuration.MemberMappings.TryGetValue(path, out var memberConfig))
        {
            memberConfig = new MemberMappingConfig { DestinationPath = path };
            configuration.MemberMappings[path] = memberConfig;
        }
        configure(new MemberOptions<TSource, TMember>(memberConfig));
        return this;
    }

    private static string ExtractPath<T, TMember>(Expression<Func<T, TMember>> expression)
    {
        var parts = new Stack<string>();
        Expression? node = expression.Body;

        while (node is MemberExpression member)
        {
            parts.Push(member.Member.Name);
            node = member.Expression;
        }

        if (parts.Count == 0)
            throw new ArgumentException(
                "Expression must be a member access expression such as 'dest => dest.Property' " +
                $"or 'dest => dest.Child.Property'. Got: {expression}",
                nameof(expression));

        return string.Join('.', parts);
    }
}
