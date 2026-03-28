using AwesomeAssertions;
using Mercator;
using Xunit;

namespace Mercator.Tests;

public class RecordMappingTests
{
    [Fact]
    public void ConventionMapping_MapsMatchingPropertiesToRecord()
    {
        var mapper = CreateMapper<Source, DestRecord>();
        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<DestRecord>(source);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public void ConventionMapping_Record_IgnoresUnmatchedProperties()
    {
        var mapper = CreateMapper<Source, DestRecord>();
        var source = new Source { Id = 42, Name = "Hello", Extra = "ignored" };

        var result = mapper.Map<DestRecord>(source);

        result.Id.Should().Be(42);
        result.Name.Should().Be("Hello");
    }

    [Fact]
    public void BindMember_OverridesConventionForRecord()
    {
        var mapper = CreateMapper<Source, DestRecord>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => $"Mapped: {s.Name}")));

        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<DestRecord>(source);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Mapped: Test");
    }

    [Fact]
    public void BindMember_WithCondition_UsesDefaultWhenFalse()
    {
        var mapper = CreateMapper<Source, DestRecord>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()))
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10)));

        var source = new Source { Id = 5, Name = "test" };

        var result = mapper.Map<DestRecord>(source);

        result.Name.Should().BeNull();
    }

    [Fact]
    public void BindMember_WithCondition_UsesResolverWhenTrue()
    {
        var mapper = CreateMapper<Source, DestRecord>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()))
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10)));

        var source = new Source { Id = 15, Name = "test" };

        var result = mapper.Map<DestRecord>(source);

        result.Name.Should().Be("TEST");
    }

    [Fact]
    public void Transform_AppliesToRecordConstructorArgs()
    {
        var mapper = CreateMapper<Source, DestRecord>(cfg => cfg
            .Transform<string>(s => string.IsNullOrEmpty(s) ? null! : s.ToUpper()));

        var source = new Source { Id = 1, Name = "test" };

        var result = mapper.Map<DestRecord>(source);

        result.Name.Should().Be("TEST");
    }

    [Fact]
    public void AfterMap_IsCalledAfterRecordCreation()
    {
        string? captured = null;
        var mapper = CreateMapper<Source, DestRecord>(cfg => cfg
            .AfterMap((src, dest) => captured = dest.Name));

        var source = new Source { Id = 1, Name = "Hello" };

        var result = mapper.Map<DestRecord>(source);

        captured.Should().Be("Hello");
    }

    [Fact]
    public void Record_WithDefaultParameterValue_UsesDefaultWhenNoSourceMatch()
    {
        var mapper = CreateMapper<SourceNoExtra, DestRecordWithDefault>();
        var source = new SourceNoExtra { Id = 7 };

        var result = mapper.Map<DestRecordWithDefault>(source);

        result.Id.Should().Be(7);
        result.Label.Should().Be("default-label");
    }

    private static MercatorMapper CreateMapper<TSource, TDest>(
        Action<IMappingExpression<TSource, TDest>>? configure = null)
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(TSource),
            DestinationType = typeof(TDest)
        };

        if (configure != null)
        {
            var expression = new MappingExpression<TSource, TDest>(config);
            configure(expression);
        }

        return new MercatorMapper(new[] { config });
    }

    private class Source
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Extra { get; set; }
    }

    private class SourceNoExtra
    {
        public int Id { get; set; }
    }

    private record DestRecord(int Id, string? Name);

    private record DestRecordWithDefault(int Id, string Label = "default-label");
}
