# Mercator

A simple C# object mapper with a fluent configuration API and convention-based mapping.

## Features

- **Convention-based mapping**: Automatically maps properties with matching names and compatible types
- **Fluent configuration**: Override conventions with explicit member mappings
- **Nested paths**: Map to deeply nested destination properties
- **Conditional mapping**: Skip members based on runtime conditions
- **Value transforms**: Apply transformations to mapped values
- **Reverse mapping**: Generate bidirectional mappings
- **DI integration**: Built-in support for `Microsoft.Extensions.DependencyInjection`

## Installation

Add the Mercator package to your project:

```bash
dotnet add package Mercator
```

## Quick Start

### 1. Define Your Mapping Registry

Create a class that inherits from `MappingRegistry` and define your mappings:

```csharp
using Mercator;

public class MyMappingRegistry : MappingRegistry
{
    public MyMappingRegistry()
    {
        Register<Source, Destination>()
            .BindMember(dest => dest.FullName, opt => opt
                .From(src => $"{src.FirstName} {src.LastName}")
                .When(src => !string.IsNullOrEmpty(src.FirstName)))
            .BindPath(dest => dest.Address.Street, opt => opt
                .From(src => src.StreetAddress))
            .Transform<string>(s => string.IsNullOrEmpty(s) ? null! : s)
            .AndReverse();
    }
}
```

### 2. Register with DI

In your `Program.cs` or startup configuration:

```csharp
builder.Services.AddMercator(typeof(MyMappingRegistry).Assembly);
```

### 3. Use the Mapper

Inject `IMapper` and map objects:

```csharp
public class MyService
{
    private readonly IMapper _mapper;

    public MyService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public DestinationDto Process(SourceEntity source)
    {
        return _mapper.Map<DestinationDto>(source);
    }
}
```

## API Reference

### MappingRegistry

Base class for declaring type mappings. Subclass and call `Register<TSource, TDestination>()` to create mappings.

**Methods:**
- `Register<TSource, TDestination>()` - Creates a mapping from TSource to TDestination

### IMappingExpression

Fluent API for configuring a mapping.

**Methods:**
- `BindMember<TMember>(Expression<Func<TDestination, TMember>> destinationMember, Action<IMemberOptions<TSource, TMember>> configure)` - Configures a single top-level destination property
- `BindPath<TMember>(Expression<Func<TDestination, TMember>> destinationPath, Action<IMemberOptions<TSource, TMember>> configure)` - Configures a nested destination member path
- `Transform<TValue>(Func<TValue, TValue> transform)` - Registers a value transform for all mapped values of type TValue
- `AndReverse()` - Registers a convention-based reverse mapping

### IMemberOptions

Configuration options for individual destination members.

**Methods:**
- `From(Func<TSource, TMember> resolver)` - Resolves the destination member value from the source
- `When(Func<TSource, bool> condition)` - Skips the member when the condition is false

### IMapper

Maps a source object to a new destination instance.

**Methods:**
- `Map<TDestination>(object source)` - Creates a new TDestination and populates it from source

## Examples

### Custom Property Mapping

```csharp
Register<Source, Dest>()
    .BindMember(dest => dest.DisplayName, opt => opt
        .From(src => $"{src.FirstName} {src.LastName}"));
```

### Conditional Mapping

```csharp
Register<Source, Dest>()
    .BindMember(dest => dest.Discount, opt => opt
        .From(src => src.Price * 0.1m)
        .When(src => src.IsPremiumCustomer));
```

### Nested Path Mapping

```csharp
Register<Source, Dest>()
    .BindPath(dest => dest.ContactInfo.Email, opt => opt
        .From(src => src.EmailAddress));
```

### Value Transforms

```csharp
Register<Source, Dest>()
    .Transform<string>(s => string.IsNullOrEmpty(s) ? null! : s)
    .Transform<DateTime>(dt => dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt);
```

### Bidirectional Mapping

```csharp
Register<Entity, Dto>()
    .BindMember(dest => dest.Created, opt => opt
        .From(src => src.CreatedAt.ToString("O")))
    .AndReverse();
```

### Multiple Calls on Same Member

Multiple calls for the same member accumulate:

```csharp
Register<Source, Dest>()
    .BindMember(dest => dest.Value, opt => opt
        .When(src => src.IsActive))
    .BindMember(dest => dest.Value, opt => opt
        .From(src => CalculateValue(src)));
```

## Convention-Based Mapping

By default, Mercator copies all properties that:
- Have the same name on source and destination
- Have compatible types (including nullable conversions)

You can override specific properties with `BindMember` or `BindPath` without affecting convention-based mapping for other properties.

## License

MIT License
