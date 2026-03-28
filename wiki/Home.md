# Mercator

Mercator is a lightweight, convention-based object mapper for .NET. It compiles all mapping
logic at construction time and stores it as plain delegates, so the hot path — `Map<T>(source)` —
consists only of a dictionary lookup and delegate invocation.

## Features

| Feature | Description |
|---------|-------------|
| [Convention mapping](Convention-Mapping) | Properties with the same name and compatible types are copied automatically |
| [Record mapping](Record-Mapping) | Map to positional records; the primary constructor is invoked with matched arguments |
| [BindMember](BindMember-and-BindPath) | Override or augment a convention-matched property with a custom resolver and/or condition |
| [BindPath](BindMember-and-BindPath#bindpath) | Map to a nested property path; intermediate objects are created automatically |
| [IgnoreMember](Ignore-Members) | Prevent a destination property from being mapped |
| [Transform](Transforms) | Apply a value transformation to every mapped value of a given type |
| [BindCollection](Collection-Mapping) | Map `IEnumerable<TSource>` to `List<TDest>` using a registered element mapping |
| [AfterMap](AfterMap) | Run a callback after all property assignments are complete |
| [AndReverse](Bidirectional-Mapping) | Register a convention-only reverse mapping in a single call |
| [MapInto](MapInto) | Populate an existing destination instance instead of creating a new one |
| [Validation](Validation) | Detect unmapped destination properties at construction time (`Strict` or `Warn`) |
| [DI integration](DI-Integration) | Register via `AddMercator` with assembly scanning |

## Quick start

```csharp
// 1. Define a registry
public class OrderRegistry : MappingRegistry
{
    public OrderRegistry()
    {
        Register<OrderEntity, OrderDto>()
            .BindMember(d => d.CustomerFullName, opt => opt.From(s => $"{s.FirstName} {s.LastName}"))
            .IgnoreMember(d => d.InternalNotes)
            .Transform<string>(s => s?.Trim() ?? s);
    }
}

// 2. Register with DI
builder.Services.AddMercator(typeof(OrderRegistry).Assembly);

// 3. Inject and use
public class OrderService(IMapper mapper)
{
    public OrderDto GetOrder(int id)
    {
        var entity = _repository.Get(id);
        return mapper.Map<OrderDto>(entity);
    }
}
```

## Requirements

- .NET 6 or later (targets `net10.0` in this repository)
- `Microsoft.Extensions.DependencyInjection` (for DI integration)

## Pages

- [Getting Started](Getting-Started)
- [Convention Mapping](Convention-Mapping)
- [Record Mapping](Record-Mapping)
- [BindMember and BindPath](BindMember-and-BindPath)
- [Ignore Members](Ignore-Members)
- [Transforms](Transforms)
- [Collection Mapping](Collection-Mapping)
- [Bidirectional Mapping](Bidirectional-Mapping)
- [AfterMap](AfterMap)
- [MapInto](MapInto)
- [Validation](Validation)
- [DI Integration](DI-Integration)
- [API Reference](API-Reference)
- [Performance](Performance)
