namespace Mercator;

internal sealed class MemberOptions<TSource, TMember>(MemberMappingConfig config)
    : IMemberOptions<TSource, TMember>
{
    public void From(Func<TSource, TMember> resolver)
    {
        config.IsIgnored = false;
        config.Resolver = src => resolver((TSource)src);
    }

    public void When(Func<TSource, bool> condition)
    {
        config.IsIgnored = false;
        config.Condition = src => condition((TSource)src);
    }
    
    public void Ignore()
    {
        config.IsIgnored = true;
        config.Resolver   = null;
        config.Condition  = null;
    }
}
