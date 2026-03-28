using AwesomeAssertions;
using Mercator;
using Xunit;

namespace Mercator.Tests;

public class MapIntoTests
{
    [Fact]
    public void MapInto_PopulatesExistingDestinationInstance()
    {
        var mapper = CreateMapper<Source, Dest>();
        var dest = new Dest { Id = 99, Name = "old" };

        mapper.MapInto(new Source { Id = 1, Name = "Test" }, dest);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Test");
    }

    [Fact]
    public void MapInto_PreservesDestinationPropertiesNotPresentOnSource()
    {
        var mapper = CreateMapper<Source, Dest>();
        var dest = new Dest { Id = 0, Name = "old", Extra = "preserved" };

        mapper.MapInto(new Source { Id = 1, Name = "Test" }, dest);

        dest.Extra.Should().Be("preserved");
    }

    [Fact]
    public void MapInto_MappedPropertiesOverwriteExistingValues()
    {
        var mapper = CreateMapper<Source, Dest>();
        var dest = new Dest { Id = 99, Name = "old" };

        mapper.MapInto(new Source { Id = 1, Name = "New" }, dest);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("New");
    }

    [Fact]
    public void MapInto_NullSource_ThrowsArgumentNullException()
    {
        var mapper = CreateMapper<Source, Dest>();

        Action act = () => mapper.MapInto<Dest>(null!, new Dest());

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void MapInto_NullDestination_ThrowsArgumentNullException()
    {
        var mapper = CreateMapper<Source, Dest>();

        Action act = () => mapper.MapInto<Dest>(new Source { Id = 1 }, null!);

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void MapInto_NoRegisteredMapping_ThrowsInvalidOperationException()
    {
        var mapper = new MercatorMapper(Enumerable.Empty<MappingConfiguration>());

        Action act = () => mapper.MapInto(new Source { Id = 1 }, new Dest());

        act.Should().ThrowExactly<InvalidOperationException>()
           .WithMessage("*No mapping registered*");
    }

    [Fact]
    public void MapInto_AfterMapActions_RunCorrectly()
    {
        string? captured = null;
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .AfterMap((src, dest) => captured = dest.Name));

        var dest = new Dest();
        mapper.MapInto(new Source { Id = 1, Name = "Test" }, dest);

        captured.Should().Be("Test");
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
        public string? Extra { get; set; }
    }
}
