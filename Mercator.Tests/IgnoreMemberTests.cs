using AwesomeAssertions;
using Mercator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mercator.Tests;

public class IgnoreMemberTests
{
    [Fact]
    public void IgnoreMember_PreventsMappingOfNamedProperty()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "Test" });

        result.Id.Should().Be(1);
        result.Name.Should().BeNull();
    }

    [Fact]
    public void IgnoreMember_MultipleMembers_AllAreSkipped()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name)
            .IgnoreMember(d => d.ExtraValue));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "Test", ExtraValue = "extra" });

        result.Id.Should().Be(1);
        result.Name.Should().BeNull();
        result.ExtraValue.Should().BeNull();
    }

    [Fact]
    public void IgnoreMember_DoesNotAffectOtherConventionProperties()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name));

        var result = mapper.Map<Dest>(new Source { Id = 42, Name = "Test", ExtraValue = "extra" });

        result.Id.Should().Be(42);
        result.ExtraValue.Should().Be("extra");
    }

    [Fact]
    public void Ignore_ViaBindMember_PreventsMappingOfNamedProperty()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.Ignore()));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "Test" });

        result.Name.Should().BeNull();
    }

    [Fact]
    public void Ignore_ClearsPriorFromOnSameEntry()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => s.Name!.ToUpper()))
            .BindMember(d => d.Name, opt => opt.Ignore()));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().BeNull();
    }

    [Fact]
    public void Ignore_ClearsPriorWhenOnSameEntry()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 0))
            .BindMember(d => d.Name, opt => opt.Ignore()));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().BeNull();
    }

    [Fact]
    public void IgnoreMember_DoesNotSuppressExplicitMappingOnOtherMember()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name)
            .BindMember(d => d.ExtraValue, opt => opt.From(s => s.ExtraValue!.ToUpper())));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "Test", ExtraValue = "extra" });

        result.Name.Should().BeNull();
        result.ExtraValue.Should().Be("EXTRA");
    }

    [Fact]
    public void IgnoreMember_TransformIsNotAppliedToIgnoredMember()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name)
            .Transform<string>(s => s is null ? "transformed" : s.ToUpper()));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().BeNull();
    }

    [Fact]
    public void IgnoreMember_TransformStillAppliesTo_NonIgnoredMembers()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name)
            .Transform<string>(s => s is null ? null! : s.ToUpper()));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test", ExtraValue = "extra" });

        result.Name.Should().BeNull();
        result.ExtraValue.Should().Be("EXTRA");
    }

    [Fact]
    public void IgnoreMember_OnNestedPath_LeavesNestedPropertyUnset()
    {
        var mapper = CreateMapper<Source, DestWithNested>(cfg => cfg
            .IgnoreMember(d => d.Nested));

        var result = mapper.Map<DestWithNested>(new Source { Id = 1, Name = "Test" });

        result.Nested.Should().BeNull();
    }

    [Fact]
    public void IgnoreMember_AndReverse_IgnoreDoesNotApplyToReverse()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name)
            .AndReverse());

        var source = new Source { Id = 1, Name = "Test" };
        var dest = mapper.Map<Dest>(source);

        dest.Name.Should().BeNull();

        var reversed = mapper.Map<Source>(dest!);
        reversed.Id.Should().Be(1);
    }

    [Fact]
    public void IgnoreMember_ViaRegistry_WorksCorrectly()
    {
        var services = new ServiceCollection();
        services.AddMercator(typeof(IgnoreRegistry).Assembly);

        var mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

        var result = mapper.Map<Dest>(new Source { Id = 7, Name = "secret", ExtraValue = "visible" });

        result.Id.Should().Be(7);
        result.Name.Should().BeNull();
        result.ExtraValue.Should().Be("visible");
    }

    [Fact]
    public void From_AfterIgnore_OnSameMember_ReenablesMapping()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name)
            .BindMember(d => d.Name, opt => opt.From(s => s.Name!.ToUpper())));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().Be("TEST");
    }

    [Fact]
    public void When_AfterIgnore_OnSameMember_ReenablesConventionMapping()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name)
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 0)));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().Be("test");
    }

    [Fact]
    public void When_AfterIgnore_ConditionFalse_MemberIsSkipped()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .IgnoreMember(d => d.Name)
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 100)));

        var result = mapper.Map<Dest>(new Source { Id = 1, Name = "test" });

        result.Name.Should().BeNull();
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

    public class IgnoreRegistry : MappingRegistry
    {
        public IgnoreRegistry()
        {
            Register<Source, Dest>()
                .IgnoreMember(d => d.Name);
        }
    }

    private class Source
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ExtraValue { get; set; }
    }

    private class Dest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ExtraValue { get; set; }
    }

    private class DestWithNested
    {
        public int Id { get; set; }
        public NestedClass? Nested { get; set; }
    }

    private class NestedClass
    {
        public string? Value { get; set; }
    }
}
