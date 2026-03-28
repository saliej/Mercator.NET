using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Mercator;
using Microsoft.Extensions.DependencyInjection;

BenchmarkRunner.Run<MapperBenchmarks>();

/// <summary>
/// Compares the cost of convention-based property mapping using:
///   - raw PropertyInfo.GetValue / SetValue (simulates the pre-F14 hot path)
///   - compiled expression-tree delegates (the post-F14 hot path)
///
/// The PropertyInfo baseline caches the pairs at field-initialisation time,
/// matching what the old BuildConventionPairs did, so the benchmark measures
/// pure per-call dispatch overhead, not setup cost.
/// </summary>
[MemoryDiagnoser]
public class MapperBenchmarks
{
    private IMapper _mapper = null!;
    private PurchaseDto _source = null!;

    // PropertyInfo baseline: pairs built once, mirroring old construction-time caching
    private static readonly (PropertyInfo Dest, PropertyInfo Src)[] PropertyInfoPairs =
        BuildPropertyInfoPairs();

    private static (PropertyInfo Dest, PropertyInfo Src)[] BuildPropertyInfoPairs()
    {
        var srcByName = typeof(PurchaseDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name);

        return typeof(Purchase)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(d => srcByName.ContainsKey(d.Name))
            .Select(d => (d, srcByName[d.Name]))
            .ToArray();
    }

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddMercator(typeof(BenchmarkRegistry).Assembly);
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

        _source = new PurchaseDto
        {
            Id         = 42,
            Name       = "Alice Smith",
            Amount     = 99.95m,
            Quantity   = 3,
            Notes      = "Rush order",
            IsActive   = true,
            Priority   = 2,
            Reference  = "REF-001",
            Discount   = 5.0m,
            Tag        = "express"
        };
    }

    [Benchmark(Baseline = true)]
    public Purchase PropertyInfo()
    {
        var dest = new Purchase();
        foreach (var (d, s) in PropertyInfoPairs)
            d.SetValue(dest, s.GetValue(_source));
        return dest;
    }

    [Benchmark]
    public Purchase CompiledDelegate()
        => _mapper.Map<Purchase>(_source);
}

public class BenchmarkRegistry : MappingRegistry
{
    public BenchmarkRegistry()
    {
        Register<PurchaseDto, Purchase>();
    }
}

public class PurchaseDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public string? Reference { get; set; }
    public decimal Discount { get; set; }
    public string? Tag { get; set; }
}

public class Purchase
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public string? Reference { get; set; }
    public decimal Discount { get; set; }
    public string? Tag { get; set; }
}
