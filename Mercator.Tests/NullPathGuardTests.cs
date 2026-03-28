using AwesomeAssertions;
using Mercator;
using Xunit;

namespace Mercator.Tests;

public class NullPathGuardTests
{
    [Fact]
    public void BindMember_FromWithNullIntermediate_SilentlySkipsDestinationMember()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.City, opt => opt.From(s => s.Address!.City)));

        var result = mapper.Map<Dest>(new Source { Id = 1, Address = null });

        result.Id.Should().Be(1);
        result.City.Should().BeNull();
    }

    [Fact]
    public void BindMember_FromWithNullIntermediate_DestinationRetainsDefaultValue()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.City, opt => opt.From(s => s.Address!.City)));

        var result = mapper.Map<Dest>(new Source { Id = 1, Address = null });

        result.City.Should().BeNull();
    }

    [Fact]
    public void BindMember_FromWithNonNullIntermediate_MapsCorrectly()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.City, opt => opt.From(s => s.Address!.City)));

        var result = mapper.Map<Dest>(new Source
        {
            Id = 1,
            Address = new AddressInfo { City = "Boston" }
        });

        result.City.Should().Be("Boston");
    }

    [Fact]
    public void BindMember_WhenFalse_TakesPriorityOverNullPath()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.City, opt => opt.When(s => s.Id > 100))
            .BindMember(d => d.City, opt => opt.From(s => s.Address!.City)));

        var result = mapper.Map<Dest>(new Source
        {
            Id = 1,
            Address = new AddressInfo { City = "Boston" }
        });

        result.City.Should().BeNull();
    }

    [Fact]
    public void BindPath_WithNestedNullPath_SilentlySkipsDestinationMember()
    {
        var mapper = CreateMapper<Source, DestWithNested>(cfg => cfg
            .BindPath(d => d.Nested.Value, opt => opt.From(s => s.Address!.City)));

        var result = mapper.Map<DestWithNested>(new Source { Id = 1, Address = null });

        result.Nested.Should().BeNull();
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
        public AddressInfo? Address { get; set; }
    }

    private class Dest
    {
        public int Id { get; set; }
        public string? City { get; set; }
    }

    private class DestWithNested
    {
        public int Id { get; set; }
        public NestedClass? Nested { get; set; }
    }

    private class AddressInfo
    {
        public string? City { get; set; }
    }

    private class NestedClass
    {
        public string? Value { get; set; }
    }
}
