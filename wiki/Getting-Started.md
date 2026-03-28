# Getting Started

## Installation

Add a project reference (or NuGet package, once published) to `Mercator`.

```xml
<ItemGroup>
  <ProjectReference Include="../Mercator/Mercator.csproj" />
</ItemGroup>
```

## Step 1 — Define types

Mercator works with any POCO classes or records. No attributes or base classes are required on
source or destination types.

```csharp
public class CustomerEntity
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsDeleted { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}
```

## Step 2 — Create a registry

Subclass `MappingRegistry` and call `Register<TSource, TDestination>()` for each pair you need.

```csharp
public class CustomerRegistry : MappingRegistry
{
    public CustomerRegistry()
    {
        Register<CustomerEntity, CustomerDto>();
        // CustomerDto has no IsDeleted property, so it is ignored automatically.
        // Id, FirstName, LastName, Email are matched by name and copied by convention.
    }
}
```

The registry must have an accessible (public, internal, or protected) parameterless constructor.
It does not need to be `public` to work programmatically, but assembly scanning via `AddMercator`
requires it to be instantiable via reflection.

## Step 3 — Register with DI

```csharp
// Program.cs / Startup.cs
builder.Services.AddMercator(typeof(CustomerRegistry).Assembly);
```

This scans the given assemblies for all concrete `MappingRegistry` subclasses, instantiates
them, collects all registrations, and registers a singleton `IMapper`.

Pass multiple assemblies if your registries are spread across projects:

```csharp
builder.Services.AddMercator(
    typeof(CustomerRegistry).Assembly,
    typeof(OrderRegistry).Assembly);
```

## Step 4 — Inject and map

```csharp
public class CustomerService(IMapper mapper, ICustomerRepository repo)
{
    public CustomerDto GetById(int id)
    {
        var entity = repo.FindById(id);
        return mapper.Map<CustomerDto>(entity);
    }
}
```

## Using without DI

Construct `MercatorMapper` directly when you do not need a DI container:

```csharp
var registry = new CustomerRegistry();
var mapper = new MercatorMapper(registry.Configurations);

var dto = mapper.Map<CustomerDto>(entity);
```

`MercatorMapper` is thread-safe and should be treated as a singleton. Creating one is relatively
expensive (reflection and expression compilation happen at construction time), so create it once
and reuse it.

## Validation during startup

Enable `Strict` mode to catch unmapped destination properties at startup instead of at runtime:

```csharp
builder.Services.AddMercator(
    opts => opts.ValidationMode = ValidationMode.Strict,
    typeof(CustomerRegistry).Assembly);
```

See [Validation](Validation) for details.

## Next steps

- [Convention Mapping](Convention-Mapping) — understand which properties are copied automatically
- [Record Mapping](Record-Mapping) — map to positional records
- [BindMember and BindPath](BindMember-and-BindPath) — customise individual property mappings
- [Validation](Validation) — detect unmapped properties early
