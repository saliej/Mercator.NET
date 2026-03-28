using AwesomeAssertions;
using Mercator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mercator.Tests;

public class ValidationTests
{
    [Fact]
    public void Strict_AllPropertiesMapped_DoesNotThrow()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(FullyMappedDest)
        };

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().NotThrow();
    }

    [Fact]
    public void None_UnmappedProperty_DoesNotThrow()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra)
        };

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.None });

        act.Should().NotThrow();
    }

    [Fact]
    public void Warn_UnmappedProperty_DoesNotThrow()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra)
        };

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Warn });

        act.Should().NotThrow();
    }

    [Fact]
    public void Strict_UnmappedProperty_ThrowsAtConstruction()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra)
        };

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().ThrowExactly<InvalidOperationException>()
           .WithMessage("*Unmapped destination members*");
    }

    [Fact]
    public void Strict_UnmappedProperty_MessageListsTheMember()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra)
        };

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().ThrowExactly<InvalidOperationException>()
           .WithMessage("*Extra*");
    }

    [Fact]
    public void Strict_ExplicitlyIgnoredProperty_NotReportedAsUnmapped()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra)
        };
        new MappingExpression<Source, DestWithExtra>(config)
            .IgnoreMember(d => d.Extra);

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().NotThrow();
    }

    [Fact]
    public void Strict_ExplicitlyBoundProperty_NotReportedAsUnmapped()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra)
        };
        new MappingExpression<Source, DestWithExtra>(config)
            .BindMember(d => d.Extra, opt => opt.From(s => "constant"));

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().NotThrow();
    }

    [Fact]
    public void Strict_NoOptions_DoesNotThrow()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra)
        };

        // Default constructor without options should never throw regardless of unmapped members
        Action act = () => new MercatorMapper(new[] { config });

        act.Should().NotThrow();
    }

    [Fact]
    public void Strict_ReverseMapping_ValidatedIndependently()
    {
        // Source -> DestWithExtra has 'Extra' unmapped.
        // AndReverse creates DestWithExtra -> Source which is convention-only.
        // Source has 'Id' and 'Name', both of which exist on DestWithExtra, so reverse is fine.
        // But DestWithExtra -> Source: Source has Id and Name, DestWithExtra has Id, Name, Extra.
        // Reversed: source=DestWithExtra, dest=Source. Source props: Id, Name. Both in DestWithExtra. OK.
        // So only forward direction has an issue.
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra),
            HasReverse = true
        };
        new MappingExpression<Source, DestWithExtra>(config)
            .IgnoreMember(d => d.Extra);

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().NotThrow();
    }

    [Fact]
    public void Strict_ReverseMapping_UnmappedOnReverseSide_IsDetected()
    {
        // SourceWithExtra -> Dest forward: all Dest props (Id, Name) covered by convention.
        // Reverse: Dest -> SourceWithExtra. SourceWithExtra has Id, Name, Extra.
        // Convention will only match Id and Name (same names). Extra on SourceWithExtra
        // has no matching property on Dest, so convention won't cover it — but we're
        // validating the *destination* of the reverse, which is SourceWithExtra.
        // SourceWithExtra.Extra is a writable public property not covered by convention or explicit.
        var config = new MappingConfiguration
        {
            SourceType = typeof(SourceWithExtra),
            DestinationType = typeof(Dest),
            HasReverse = true
        };

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        // Reverse is DestinationType(Dest) -> SourceType(SourceWithExtra).
        // SourceWithExtra.Extra won't be mapped by convention (Dest has no Extra property).
        act.Should().ThrowExactly<InvalidOperationException>()
           .WithMessage("*Extra*");
    }

    [Fact]
    public void AddMercator_WithStrictValidation_ThrowsWhenUnmappedMembersExist()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMercator(
            opts => opts.ValidationMode = ValidationMode.Strict,
            typeof(UnmappedRegistry).Assembly);

        act.Should().ThrowExactly<InvalidOperationException>()
           .WithMessage("*Unmapped destination members*");
    }

    [Fact]
    public void AddMercator_WithWarnValidation_DoesNotThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMercator(
            opts => opts.ValidationMode = ValidationMode.Warn,
            typeof(UnmappedRegistry).Assembly);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddMercator_WithNoneValidation_DoesNotThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMercator(
            opts => opts.ValidationMode = ValidationMode.None,
            typeof(UnmappedRegistry).Assembly);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddMercator_NoConfigureDelegate_DoesNotThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMercator(typeof(UnmappedRegistry).Assembly);

        act.Should().NotThrow();
    }

    [Fact]
    public void Strict_BindPath_ToNestedMember_DoesNotFlagTopLevelPropertyAsUnmapped()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithNested)
        };
        new MappingExpression<Source, DestWithNested>(config)
            .BindPath(d => d.Nested!.Value, opt => opt.From(s => s.Name));

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().NotThrow();
    }

    [Fact]
    public void Strict_PropertyWithInternalSetter_ConventionMapped_NotFlaggedAsUnmapped()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(SourceWithInternalProp),
            DestinationType = typeof(DestWithInternalSetter)
        };

        Action act = () => new MercatorMapper(
            new[] { config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().NotThrow();
    }

    [Fact]
    public void Strict_BindCollection_MappedProperty_NotFlaggedAsUnmapped()
    {
        var elementConfig = new MappingConfiguration
        {
            SourceType = typeof(ItemDto),
            DestinationType = typeof(Item)
        };
        var config = new MappingConfiguration
        {
            SourceType = typeof(CollectionSource),
            DestinationType = typeof(CollectionDest)
        };
        new MappingExpression<CollectionSource, CollectionDest>(config)
            .BindCollection<ItemDto, Item>(d => d.Items, s => s.Items);

        Action act = () => new MercatorMapper(
            new[] { elementConfig, config },
            new MercatorOptions { ValidationMode = ValidationMode.Strict });

        act.Should().NotThrow();
    }

    [Fact]
    public void BindMember_WithEmptyConfigureAction_ThrowsAtConstruction()
    {
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(FullyMappedDest)
        };
        new MappingExpression<Source, FullyMappedDest>(config)
            .BindMember(d => d.Name, opt => { });

        Action act = () => new MercatorMapper(new[] { config });

        act.Should().ThrowExactly<InvalidOperationException>()
           .WithMessage("*Name*");
    }

    [Fact]
    public void Warn_UnmappedProperty_InvokesWarnCallback()
    {
        var captured = new List<string>();
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(DestWithExtra)
        };

        _ = new MercatorMapper(
            new[] { config },
            new MercatorOptions
            {
                ValidationMode = ValidationMode.Warn,
                WarnCallback = msg => captured.Add(msg)
            });

        captured.Should().ContainSingle(m => m.Contains("Extra"));
    }

    [Fact]
    public void Warn_AllMapped_WarnCallbackNotInvoked()
    {
        var called = false;
        var config = new MappingConfiguration
        {
            SourceType = typeof(Source),
            DestinationType = typeof(FullyMappedDest)
        };

        _ = new MercatorMapper(
            new[] { config },
            new MercatorOptions
            {
                ValidationMode = ValidationMode.Warn,
                WarnCallback = _ => called = true
            });

        called.Should().BeFalse();
    }

    // Registry used for DI tests — has an unmapped destination property.
    public class UnmappedRegistry : MappingRegistry
    {
        public UnmappedRegistry()
        {
            Register<Source, DestWithExtra>();
        }
    }

    private class Source
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class SourceWithExtra
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Extra { get; set; }
    }

    private class Dest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class FullyMappedDest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class DestWithExtra
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Extra { get; set; }
    }

    private class DestWithNested
    {
        public int Id { get; set; }
        public NestedValue? Nested { get; set; }
    }

    private class NestedValue
    {
        public string? Value { get; set; }
    }

    private class SourceWithInternalProp
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class DestWithInternalSetter
    {
        public int Id { get; set; }
        public string? Name { get; internal set; }
    }

    private class CollectionSource
    {
        public int Id { get; set; }
        public List<ItemDto>? Items { get; set; }
    }

    private class CollectionDest
    {
        public int Id { get; set; }
        public List<Item>? Items { get; set; }
    }

    private class ItemDto
    {
        public string? Value { get; set; }
    }

    private class Item
    {
        public string? Value { get; set; }
    }
}
