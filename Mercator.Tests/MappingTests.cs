using AwesomeAssertions;
using Mercator;
using Xunit;

namespace Mercator.Tests;

public class MappingTests
{
    [Fact]
    public void ConventionMapping_MapsMatchingProperties()
    {
        var mapper = CreateMapper<Source, Dest>();
        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<Dest>(source);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public void ConventionMapping_IgnoresPropertiesWithDifferentNames()
    {
        var mapper = CreateMapper<Source, Dest>();
        var source = new Source { Id = 1, Name = "Test", ExtraValue = "extra" };

        var result = mapper.Map<Dest>(source);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public void ConventionMapping_IgnoresPropertiesWithIncompatibleTypes()
    {
        var mapper = CreateMapper<Source, Dest>();
        var source = new Source { Id = 1, Name = "Test", Count = 5 };

        var result = mapper.Map<Dest>(source);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
        result.TextCount.Should().BeNull();
    }

    [Fact]
    public void ConventionMapping_HandlesNullableConversions()
    {
        var mapper = CreateMapper<SourceWithNullables, DestWithNullables>();
        var source = new SourceWithNullables { Value = 42, Optional = null };

        var result = mapper.Map<DestWithNullables>(source);

        result.Value.Should().Be(42);
        result.Optional.Should().BeNull();
    }

    [Fact]
    public void BindMember_OverridesConventionMapping()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => $"Custom: {s.Name}")));

        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<Dest>(source);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Custom: Test");
    }

    [Fact]
    public void BindMember_MapsToNonexistentSourceProperty()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.TextCount, opt => opt.From(s => s.Count.ToString())));

        var source = new Source { Id = 1, Count = 5 };

        var result = mapper.Map<Dest>(source);

        result.TextCount.Should().Be("5");
    }

    [Fact]
    public void BindMember_WithConstantValue()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(_ => "Constant")));

        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().Be("Constant");
    }

    [Fact]
    public void BindPath_MapsToNestedProperty()
    {
        var mapper = CreateMapper<Source, DestWithNested>(cfg => cfg
            .BindPath(d => d.Nested!.Value, opt => opt.From(s => s.Name)));

        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<DestWithNested>(source);

        result.Nested.Should().NotBeNull();
        result.Nested!.Value.Should().Be("Test");
    }

    [Fact]
    public void BindPath_CreatesIntermediateObjects()
    {
        var mapper = CreateMapper<Source, DestWithNested>(cfg => cfg
            .BindPath(d => d.Nested!.NestedAgain!.Id, opt => opt.From(s => s.Id)));

        var source = new Source { Id = 1 };

        var result = mapper.Map<DestWithNested>(source);

        result.Nested.Should().NotBeNull();
        result.Nested!.NestedAgain.Should().NotBeNull();
        result.Nested.NestedAgain!.Id.Should().Be(1);
    }

    [Fact]
    public void When_SkipsMemberWhenConditionIsFalse()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()))
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10)));

        var source = new Source { Id = 5, Name = "test" };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().BeNull();
    }

    [Fact]
    public void When_MapsMemberWhenConditionIsTrue()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()))
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10)));

        var source = new Source { Id = 15, Name = "test" };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().Be("TEST");
    }

    [Fact]
    public void When_WithoutFrom_SkipsMemberWhenFalse()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10)));

        var source = new Source { Id = 5, Name = "test" };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().BeNull();
    }

    [Fact]
    public void When_WithoutFrom_UsesConventionWhenTrue()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10)));

        var source = new Source { Id = 15, Name = "test" };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().Be("test");
    }

    [Fact]
    public void Transform_AppliesToAllValuesOfSpecifiedType()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .Transform<string>(s => string.IsNullOrEmpty(s) ? null! : s.ToUpper()));

        var source = new Source { Id = 1, Name = "test", ExtraValue = "" };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().Be("TEST");
        result.ExtraValue.Should().BeNull();
    }

    [Fact]
    public void Transform_AppliesToCustomMappings()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.TextCount, opt => opt.From(s => "custom"))
            .Transform<string>(s => s is null ? null! : s.ToUpper()));

        var source = new Source { Id = 1 };

        var result = mapper.Map<Dest>(source);

        result.TextCount.Should().Be("CUSTOM");
    }

    [Fact]
    public void Transform_HandlesNullValues()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .Transform<string>(s => s is null ? "replaced" : s.ToUpper()));

        var source = new Source { Id = 1, Name = null };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().Be("replaced");
    }

    [Fact]
    public void AndReverse_CreatesBidirectionalMapping()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.TextCount, opt => opt.From(s => s.Id.ToString()))
            .AndReverse());

        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<Dest>(source);
        var reversed = mapper.Map<Source>(result!);

        reversed.Id.Should().Be(1);
        reversed.Name.Should().Be("Test");
    }

    [Fact]
    public void AndReverse_OnlyUsesConventionForReverse()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.TextCount, opt => opt.From(s => s.Id.ToString()))
            .AndReverse());

        var source = new Source { Id = 1, Name = "Test" };

        var dest = mapper.Map<Dest>(source);
        var reversed = mapper.Map<Source>(dest!);

        reversed.Id.Should().Be(1);
        reversed.Name.Should().Be("Test");
    }

    [Fact]
    public void MultipleRegistries_AllMappingsAreAvailable()
    {
        var config1 = CreateMappingConfiguration<Source, Dest>();
        var config2 = CreateMappingConfiguration<Source, DestWithNested>();

        var mapper = new MercatorMapper(new[] { config1, config2 });

        var source = new Source { Id = 1, Name = "Test" };

        var dest1 = mapper.Map<Dest>(source);
        var dest2 = mapper.Map<DestWithNested>(source);

        dest1.Id.Should().Be(1);
        dest1.Name.Should().Be("Test");
        dest2.Id.Should().Be(1);
        dest2.Name.Should().Be("Test");
    }

    [Fact]
    public void Map_WithNullSource_ThrowsArgumentNullException()
    {
        var mapper = CreateMapper<Source, Dest>();

        Action act = () => mapper.Map<Dest>(null!);

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Map_WithNoMappingRegistered_ThrowsInvalidOperationException()
    {
        var mapper = new MercatorMapper(Enumerable.Empty<MappingConfiguration>());
        var source = new Source { Id = 1 };

        Action act = () => mapper.Map<Dest>(source);

        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("*No mapping registered*");
    }

    [Fact]
    public void Map_WithDestinationWithoutParameterlessConstructor_ThrowsMissingMethodException()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithoutConstructor)
        };

        var mapper = new MercatorMapper(new[] { config });
        var source = new Source { Id = 1 };

        Action act = () => mapper.Map<DestWithoutConstructor>(source);

        act.Should().ThrowExactly<MissingMethodException>();
    }

    [Fact]
    public void LastRegistration_WinsForDuplicateMappings()
    {
        var config1 = CreateMappingConfiguration<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => "first")));

        var config2 = CreateMappingConfiguration<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => "second")));

        var mapper = new MercatorMapper(new[] { config1, config2 });
        var source = new Source { Id = 1 };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().Be("second");
    }

    [Fact]
    public void MultipleCallsToBindMember_ForSameMember_Accumulate()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10))
            .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper())));

        var source = new Source { Id = 15, Name = "test" };

        var result = mapper.Map<Dest>(source);

        result.Name.Should().Be("TEST");
    }

    [Fact]
    public void BindMember_WithComplexObject()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.ExtraValue, opt => opt.From(s => new string((s.Name ?? string.Empty).Reverse().ToArray()))));

        var source = new Source { Id = 1, Name = "Test" };

        var result = mapper.Map<Dest>(source);

        result.ExtraValue.Should().Be("tseT");
    }

    [Fact]
    public void ConventionMapping_WithStructProperties()
    {
        var mapper = CreateMapper<SourceWithStruct, DestWithStruct>();
        var source = new SourceWithStruct { Id = 1, Amount = 100.50m };

        var result = mapper.Map<DestWithStruct>(source);

        result.Id.Should().Be(1);
        result.Amount.Should().Be(100.50m);
    }

    [Fact]
    public void Transform_WithValueType()
    {
        var mapper = CreateMapper<SourceWithStruct, DestWithStruct>(cfg => cfg
            .Transform<decimal>(d => d * 2));

        var source = new SourceWithStruct { Id = 1, Amount = 100m };

        var result = mapper.Map<DestWithStruct>(source);

        result.Amount.Should().Be(200m);
    }

    [Fact]
    public void NestedPath_WithMultipleLevels()
    {
        var mapper = CreateMapper<Source, DestWithNested>(cfg => cfg
            .BindPath(d => d.Nested!.NestedAgain!.Value, opt => opt.From(s => s.Name)));

        var source = new Source { Id = 1, Name = "deep" };

        var result = mapper.Map<DestWithNested>(source);

        result.Nested!.NestedAgain!.Value.Should().Be("deep");
    }

    [Fact]
    public void ConventionMapping_DerivedSourceType_BaseDestinationProperty_MapsCorrectly()
    {
        var mapper = CreateMapper<SourceWithDerived, DestWithBase>();

        var source = new SourceWithDerived { Animal = new DerivedAnimal { Name = "Dog", Breed = "Lab" } };
        var result = mapper.Map<DestWithBase>(source);

        result.Animal.Should().NotBeNull();
        result.Animal!.Name.Should().Be("Dog");
    }

    [Fact]
    public void ConventionMapping_BaseSourceType_DerivedDestinationProperty_NotPaired()
    {
        var mapper = CreateMapper<SourceWithBase, DestWithDerived>();

        var source = new SourceWithBase { Animal = new BaseAnimal { Name = "Cat" } };
        var result = mapper.Map<DestWithDerived>(source);

        result.Animal.Should().BeNull();
    }

    [Fact]
    public void ConventionMapping_RuntimeDerivedType_UsesRuntimeType()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(DerivedSource),
            DestinationType = typeof(Dest)
        };
        var mapper = new MercatorMapper(new[] { config });

        Action act = () => mapper.Map<Dest>(new BaseSource { Id = 1, Name = "Test" });

        act.Should().ThrowExactly<InvalidOperationException>()
           .WithMessage("*No mapping registered*");
    }

    private static MercatorMapper CreateMapper<TSource, TDest>(Action<IMappingExpression<TSource, TDest>>? configure = null)
        where TDest : new()
    {
        var config = CreateMappingConfiguration(configure);
        return new MercatorMapper(new[] { config });
    }

    private static MappingConfiguration CreateMappingConfiguration<TSource, TDest>(Action<IMappingExpression<TSource, TDest>>? configure = null)
        where TDest : new()
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

        return config;
    }

    private class Source
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ExtraValue { get; set; }
        public int Count { get; set; }
    }

    private class Dest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ExtraValue { get; set; }
        public string? TextCount { get; set; }
    }

    private class SourceWithNullables
    {
        public int Value { get; set; }
        public int? Optional { get; set; }
    }

    private class DestWithNullables
    {
        public int Value { get; set; }
        public int? Optional { get; set; }
    }

    private class DestWithNested
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public NestedClass? Nested { get; set; }
    }

    private class NestedClass
    {
        public string? Value { get; set; }
        public NestedAgainClass? NestedAgain { get; set; }
    }

    private class NestedAgainClass
    {
        public int Id { get; set; }
        public string? Value { get; set; }
    }

    private class DestWithoutConstructor
    {
        public int Id { get; set; }

        private DestWithoutConstructor()
        {
        }
    }

    private class SourceWithStruct
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
    }

    private class DestWithStruct
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
    }

    private class BaseAnimal
    {
        public string? Name { get; set; }
    }

    private class DerivedAnimal : BaseAnimal
    {
        public string? Breed { get; set; }
    }

    private class SourceWithDerived
    {
        public DerivedAnimal? Animal { get; set; }
    }

    private class DestWithBase
    {
        public BaseAnimal? Animal { get; set; }
    }

    private class SourceWithBase
    {
        public BaseAnimal? Animal { get; set; }
    }

    private class DestWithDerived
    {
        public DerivedAnimal? Animal { get; set; }
    }

    private class BaseSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class DerivedSource : BaseSource
    {
        public string? Extra { get; set; }
    }
}
