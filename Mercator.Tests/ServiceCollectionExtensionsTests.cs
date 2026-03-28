using AwesomeAssertions;
using Mercator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mercator.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMercator_RegistersMapperAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddMercator(typeof(TestRegistry).Assembly);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMapper));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMercator_WithMultipleAssemblies_RegistersAllMappings()
    {
        var services = new ServiceCollection();
        services.AddMercator(
            typeof(TestRegistry).Assembly,
            typeof(ServiceCollectionExtensionsTests).Assembly);

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var source = new TestSource { Id = 1, Name = "Test" };
        var result = mapper.Map<TestDest>(source);

        result.Id.Should().Be(1);
    }

    [Fact]
    public void AddMercator_WithDuplicateAssemblies_IgnoresDuplicates()
    {
        var services = new ServiceCollection();
        var assembly = typeof(TestRegistry).Assembly;

        services.AddMercator(assembly, assembly);

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var source = new TestSource { Id = 1 };
        var result = mapper.Map<TestDest>(source!);

        result.Id.Should().Be(1);
    }

    [Fact]
    public void AddMercator_ResolvesMapperFromServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMercator(typeof(TestRegistry).Assembly);

        var provider = services.BuildServiceProvider();

        var mapper1 = provider.GetRequiredService<IMapper>();
        var mapper2 = provider.GetRequiredService<IMapper>();

        mapper1.Should().BeSameAs(mapper2);
    }

    [Fact]
    public void AddMercator_WithMultipleRegistries_AllMappingsAvailable()
    {
        var services = new ServiceCollection();
        services.AddMercator(typeof(TestRegistry).Assembly);

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var source = new TestSource { Id = 1, Name = "Test" };

        var dest1 = mapper.Map<TestDest>(source);
        var dest2 = mapper.Map<TestDestWithCustomMapping>(source);

        dest1.Id.Should().Be(1);
        dest2.CustomValue.Should().Be("custom");
    }

    [Fact]
    public void AddMercator_WithRegistryUsingAllFeatures_WorksCorrectly()
    {
        var services = new ServiceCollection();
        services.AddMercator(typeof(ComprehensiveRegistry).Assembly);

        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var source = new TestSource { Id = 15, Name = "test", Count = 5 };

        var result = mapper.Map<ComprehensiveDest>(source);

        result.Name.Should().Be("TEST");
        result.TextCount.Should().Be("5");
    }

    [Fact]
    public void AddMercator_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddMercator(typeof(TestRegistry).Assembly);

        result.Should().BeSameAs(services);
    }

    public class TestRegistry : MappingRegistry
    {
        public TestRegistry()
        {
            Register<TestSource, TestDest>();
            Register<TestSource, TestDestWithCustomMapping>()
                .BindMember(d => d.CustomValue, opt => opt.From(_ => "custom"));
        }
    }

    public class ComprehensiveRegistry : MappingRegistry
    {
        public ComprehensiveRegistry()
        {
            Register<TestSource, ComprehensiveDest>()
                .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10))
                .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()))
                .BindMember(d => d.TextCount, opt => opt.From(s => s.Count.ToString()))
                .Transform<string>(s => s is null ? null! : s.ToUpper());
        }
    }

    public class Source
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    public class Dest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? TextCount { get; set; }
    }

    public class DestWithCustomMapping
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CustomValue { get; set; }
    }
}

// Test classes for DI
public class TestSource
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Count { get; set; }
}

public class TestDest
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TextCount { get; set; }
}

public class TestDestWithCustomMapping
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? CustomValue { get; set; }
}

public class ComprehensiveDest
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TextCount { get; set; }
}
