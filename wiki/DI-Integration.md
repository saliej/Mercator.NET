# DI Integration

Mercator integrates with `Microsoft.Extensions.DependencyInjection` via the `AddMercator`
extension method. The mapper is registered as a **singleton** `IMapper`.

## Basic registration

```csharp
// Program.cs
builder.Services.AddMercator(typeof(MyRegistry).Assembly);
```

This scans the given assembly for all concrete, non-abstract subclasses of `MappingRegistry`,
instantiates each one, collects their configurations, and registers a singleton `IMapper`.

## Multiple assemblies

```csharp
builder.Services.AddMercator(
    typeof(OrderRegistry).Assembly,
    typeof(CustomerRegistry).Assembly);
```

Duplicate assemblies are deduplicated automatically.

## With options

```csharp
builder.Services.AddMercator(
    opts =>
    {
        opts.ValidationMode = ValidationMode.Strict;
        opts.WarnCallback   = msg => logger.LogWarning("{Msg}", msg);
    },
    typeof(MyRegistry).Assembly);
```

## Registry discovery rules

`AddMercator` instantiates registries via:

```csharp
Activator.CreateInstance(t, nonPublic: true)
```

This means the registry's parameterless constructor can be `public`, `internal`, or `protected`.
Private constructors are also accepted. The registry class itself must be concrete (not abstract)
and a subclass of `MappingRegistry`.

> **Note:** Private *nested* classes cannot be discovered by assembly scanning because reflection
> cannot create instances of them externally. Make registries top-level classes or at minimum
> non-private nested classes.

## Injecting IMapper

```csharp
// Constructor injection
public class OrderService(IMapper mapper) { ... }

// Minimal API
app.MapGet("/orders/{id}", (int id, IMapper mapper, IOrderRepo repo) =>
{
    var entity = repo.Get(id);
    return mapper.Map<OrderDto>(entity);
});
```

## Singleton lifecycle

`IMapper` is a singleton. `MercatorMapper` is thread-safe — all compilation happens at
construction time and the `Map` / `MapInto` methods only read from the compiled delegate
dictionary.

Do **not** create a `MercatorMapper` per request. The expression compilation in the constructor
is intentionally expensive (done once) so the hot path is cheap.

## Chaining with other registrations

`AddMercator` returns `IServiceCollection` for fluent chaining:

```csharp
builder.Services
    .AddMercator(typeof(MyRegistry).Assembly)
    .AddScoped<IOrderService, OrderService>()
    .AddDbContext<AppDbContext>(...);
```

## Testing with DI

In integration tests, create a real `ServiceCollection` and resolve `IMapper`:

```csharp
var services = new ServiceCollection();
services.AddMercator(typeof(MyRegistry).Assembly);
var mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
```

## Testing without DI

For unit tests, construct `MercatorMapper` directly from a `MappingConfiguration`:

```csharp
var config = new MappingConfiguration
{
    SourceType      = typeof(Source),
    DestinationType = typeof(Dest)
};
new MappingExpression<Source, Dest>(config)
    .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()));

var mapper = new MercatorMapper(new[] { config });
```

`MappingConfiguration` and `MappingExpression` are `internal`, so they are accessible from test
projects that have `InternalsVisibleTo` set (the default in this repository for
`Mercator.Tests`).
