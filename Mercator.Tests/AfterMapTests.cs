using AwesomeAssertions;
using Mercator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mercator.Tests;

public class AfterMapTests
{
    [Fact]
    public void AfterMap_SingleAction_RunsAfterAllPropertyAssignments()
    {
        string? capturedValue = null;

        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .AfterMap((src, dest) => capturedValue = dest.Name));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "Test" });

        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
        capturedValue.Should().Be("Test");
    }

    [Fact]
    public void AfterMap_MultipleActions_RunInDeclarationOrder()
    {
        var order = new List<string>();

        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .AfterMap((src, dest) => order.Add("first"))
            .AfterMap((src, dest) => order.Add("second"))
            .AfterMap((src, dest) => order.Add("third")));

        mapper.Map<Dest>(new Source { Id = 1, Name = "Test" });

        order.Should().BeEquivalentTo(new[] { "first", "second", "third" });
    }

    [Fact]
    public void AfterMap_CanOverwriteValueSetByConvention()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .AfterMap((src, dest) => dest.Name = "Overwritten"));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "Test" });

        result.Id.Should().Be(1);
        result.Name.Should().Be("Overwritten");
    }

    [Fact]
    public void AfterMap_AndReverse_ReverseMappingHasNoAfterMapActions()
    {
        var callCount = 0;

        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .AfterMap((src, dest) => callCount++)
            .AndReverse());

        mapper.Map<Dest>(new Source { Id = 1, Name = "Test" });
        callCount.Should().Be(1);

        mapper.Map<Source>(new Dest { Id = 1, Name = "Test" });
        callCount.Should().Be(1); // reverse must not invoke the forward AfterMap action
    }

    [Fact]
    public void AfterMap_WithExplicitMapping_StillRunsAfterExplicitMapping()
    {
        string? capturedValue = null;

        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => "Explicit"))
            .AfterMap((src, dest) => capturedValue = dest.Name));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "Test" });

        result.Name.Should().Be("Explicit");
        capturedValue.Should().Be("Explicit");
    }

    private static MercatorMapper CreateMapper<TSource, TDest>(
        Action<IMappingExpression<TSource, TDest>>? configure = null)
        where TDest : new()
    {
        var config = new MappingConfiguration
        {
            SourceType      = typeof(TSource),
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
