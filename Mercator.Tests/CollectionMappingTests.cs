using AwesomeAssertions;
using Mercator;
using Xunit;

namespace Mercator.Tests;

public class CollectionMappingTests
{
    [Fact]
    public void BindCollection_RegisteredElementMapping_IsAppliedToEachItem()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindCollection<AddressDto, Address>(d => d.Addresses, s => s.Addresses));

        var source = new Source
        {
            Id = 1,
            Addresses = new List<AddressDto>
            {
                new() { Street = "123 Main", City = "Boston" },
                new() { Street = "456 Oak", City = "Cambridge" }
            }
        };

        var result = mapper.Map<Dest>(source);

        result.Id.Should().Be(1);
        result.Addresses.Should().HaveCount(2);
        result.Addresses[0].Street.Should().Be("123 Main");
        result.Addresses[0].City.Should().Be("Boston");
        result.Addresses[1].Street.Should().Be("456 Oak");
        result.Addresses[1].City.Should().Be("Cambridge");
    }

    [Fact]
    public void BindCollection_NullSourceCollection_ProducesNullDestination()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindCollection<AddressDto, Address>(d => d.Addresses, s => s.Addresses));

        var result = mapper.Map<Dest>(new Source { Id = 1, Addresses = null });

        result.Addresses.Should().BeNull();
    }

    [Fact]
    public void BindCollection_EmptySourceCollection_ProducesEmptyList()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindCollection<AddressDto, Address>(d => d.Addresses, s => s.Addresses));

        var result = mapper.Map<Dest>(new Source
        {
            Id = 1,
            Addresses = new List<AddressDto>()
        });

        result.Addresses.Should().NotBeNull();
        result.Addresses.Should().BeEmpty();
    }

    [Fact]
    public void BindCollection_CoexistsWithConventionMapping()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindCollection<AddressDto, Address>(d => d.Addresses, s => s.Addresses));

        var result = mapper.Map<Dest>(new Source
        {
            Id = 42,
            Name = "Test",
            Addresses = new List<AddressDto>
            {
                new() { Street = "123 Main", City = "Boston" }
            }
        });

        result.Id.Should().Be(42);
        result.Name.Should().Be("Test");
        result.Addresses.Should().HaveCount(1);
    }

    [Fact]
    public void BindCollection_MissingElementMapping_ThrowsInvalidOperationException()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindCollection<AddressDto, Address>(d => d.Addresses, s => s.Addresses),
            skipElementMapping: true);

        var source = new Source
        {
            Id = 1,
            Addresses = new List<AddressDto> { new() { Street = "123 Main", City = "Boston" } }
        };

        Action act = () => mapper.Map<Dest>(source);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*No mapping registered*");
    }

    [Fact]
    public void BindCollection_WithExplicitMemberMappings_WorksCorrectly()
    {
        var mapper = CreateMapper<Source, Dest>(cfg => cfg
            .BindMember(d => d.Name, opt => opt.From(s => $"Mapped: {s.Name}"))
            .BindCollection<AddressDto, Address>(d => d.Addresses, s => s.Addresses));

        var result = mapper.Map<Dest>(new Source
        {
            Id = 1,
            Name = "Test",
            Addresses = new List<AddressDto>
            {
                new() { Street = "123 Main", City = "Boston" }
            }
        });

        result.Name.Should().Be("Mapped: Test");
        result.Addresses.Should().HaveCount(1);
    }

    [Fact]
    public void BindCollection_NestedDestinationPath_NullSourceCollection_DoesNotThrow()
    {
        var mapper = CreateMapperWithContainer<SourceWithItems, DestWithContainer>(cfg => cfg
            .BindCollection<AddressDto, Address>(d => d.Container!.Items, s => s.Items));

        Action act = () => mapper.Map<DestWithContainer>(new SourceWithItems { Items = null });

        act.Should().NotThrow();
    }

    [Fact]
    public void BindCollection_NestedDestinationPath_NonNullSourceCollection_MapsCorrectly()
    {
        var mapper = CreateMapperWithContainer<SourceWithItems, DestWithContainer>(cfg => cfg
            .BindCollection<AddressDto, Address>(d => d.Container!.Items, s => s.Items));

        var source = new SourceWithItems
        {
            Items = new List<AddressDto> { new() { Street = "123 Main", City = "Boston" } }
        };

        var result = mapper.Map<DestWithContainer>(source);

        result.Container.Should().NotBeNull();
        result.Container!.Items.Should().HaveCount(1);
        result.Container.Items![0].Street.Should().Be("123 Main");
    }

    private static MercatorMapper CreateMapperWithContainer<TSource, TDest>(
        Action<IMappingExpression<TSource, TDest>>? configure = null)
        where TDest : new()
    {
        var configs = new List<MappingConfiguration>
        {
            new()
            {
                SourceType = typeof(TSource),
                DestinationType = typeof(TDest)
            }
        };

        configs.Add(new()
        {
            SourceType = typeof(AddressDto),
            DestinationType = typeof(Address)
        });

        if (configure is not null)
            configure(new MappingExpression<TSource, TDest>(configs[0]));

        return new MercatorMapper(configs);
    }

    private static MercatorMapper CreateMapper<TSource, TDest>(
        Action<IMappingExpression<TSource, TDest>>? configure = null,
        bool skipElementMapping = false)
        where TDest : new()
    {
        var configs = new List<MappingConfiguration>
        {
            new()
            {
                SourceType = typeof(TSource),
                DestinationType = typeof(TDest)
            }
        };

        if (!skipElementMapping)
        {
            configs.Add(new()
            {
                SourceType = typeof(AddressDto),
                DestinationType = typeof(Address)
            });
        }

        if (configure is not null)
            configure(new MappingExpression<TSource, TDest>(configs[0]));

        return new MercatorMapper(configs);
    }

    private class Source
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<AddressDto>? Addresses { get; set; }
    }

    private class Dest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Address>? Addresses { get; set; }
    }

    private class AddressDto
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    private class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    private class SourceWithItems
    {
        public List<AddressDto>? Items { get; set; }
    }

    private class AddressContainer
    {
        public List<Address>? Items { get; set; }
    }

    private class DestWithContainer
    {
        public AddressContainer? Container { get; set; }
    }
}
