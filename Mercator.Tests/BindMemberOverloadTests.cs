using AwesomeAssertions;
using Mercator;
using Xunit;

namespace Mercator.Tests;

public class BindMemberOverloadTests
{
    [Fact]
    public void BindMember_WithWhenAndFrom_SingleCallProducesIdenticalBehaviourToTwoCalls()
    {
        var mapper1 = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 0))
            .BindMember(d => d.Name, opt => opt.From(s => s.Name!.ToUpper())));

        var mapper2 = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, s => s.Id > 0, s => s.Name!.ToUpper()));

        var source = new Source { Id = 1, Name = "test" };

        var result1 = mapper1.Map<Dest>(source);
        var result2 = mapper2.Map<Dest>(source);

        result1.Name.Should().Be("TEST");
        result2.Name.Should().Be("TEST");
        result1.Name.Should().Be(result2.Name);
    }

    [Fact]
    public void BindMember_WithWhenAndFrom_ConditionFalse_MemberIsSkipped()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, s => s.Id > 100, s => s.Name!.ToUpper()));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().BeNull();
    }

    [Fact]
    public void BindMember_WithWhenAndFrom_ConditionTrue_ResolverResultIsWritten()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, s => s.Id > 0, s => $"Processed: {s.Name}"));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().Be("Processed: test");
    }

    [Fact]
    public void BindMember_WithWhenAndFrom_WorksWithTransforms()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, s => s.Id > 0, s => s.Name)
            .Transform<string>(s => s is null ? null! : s.ToUpper()));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().Be("TEST");
    }

    private static MercatorMapper CreateMapper<TSource, TDest>(
        Action<IMappingExpression<TSource, TDest>>? configure = null)
        where TDest : new()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(TSource),
            DestinationType = typeof(TDest)
        };

        if (configure is not null)
            configure(new MappingExpression<TSource, TDest>(config));

        return new MercatorMapper(new[] { config });
    }

    private class Source
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class Dest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
