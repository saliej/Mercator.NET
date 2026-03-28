# Record Mapping

Mercator supports C# positional records as mapping destinations without any extra configuration.
When a destination type has public constructors but no parameterless constructor, Mercator selects
the constructor with the most parameters (the *primary constructor*) and invokes it, passing a
resolved value for each parameter.

## How it works

For a type like:

```csharp
public record CustomerSummary(int Id, string? FullName);
```

`Map<CustomerSummary>(source)` is equivalent to:

```csharp
new CustomerSummary(
    Id:      resolvedId,
    FullName: resolvedFullName)
```

Each constructor parameter is resolved in this order:

1. An explicit `BindMember` configuration for the matching property name (see below).
2. A convention match — a source property whose name matches the parameter name
   (case-insensitive).
3. The parameter's declared default value, if any.
4. `default` for the parameter's type (`null` for reference types, `0` / `false` etc. for
   value types).

## Basic example

```csharp
public class CustomerEntity
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public bool IsDeleted { get; set; }
}

public record CustomerSummary(int Id, string? FullName);

// In a MappingRegistry:
Register<CustomerEntity, CustomerSummary>();
// IsDeleted has no matching constructor parameter and is silently ignored.
```

```csharp
var summary = mapper.Map<CustomerSummary>(entity);
// summary.Id == entity.Id
// summary.FullName == entity.FullName
```

## BindMember with records

Use `BindMember` to override how a specific constructor argument is resolved. The expression
targets the record's property, which shares the name of the constructor parameter.

```csharp
public record OrderLine(int ProductId, string Description, decimal Total);

Register<OrderEntity, OrderLine>()
    .BindMember(d => d.Description, opt => opt.From(s => $"{s.Sku} — {s.Name}"))
    .BindMember(d => d.Total, opt => opt.From(s => s.Quantity * s.UnitPrice));
```

`Transform` and `AfterMap` also apply normally:

```csharp
Register<OrderEntity, OrderLine>()
    .Transform<string>(s => string.IsNullOrWhiteSpace(s) ? null! : s.Trim())
    .AfterMap((src, dest) => Console.WriteLine($"Mapped order {dest.ProductId}"));
```

## Default parameter values

If a constructor parameter has a default value and no source property matches it, the default is
used:

```csharp
public record PagedResult(int Page, int PageSize = 20);

public class QueryParams { public int Page { get; set; } }

Register<QueryParams, PagedResult>();
// PagedResult.PageSize will be 20 when there is no PageSize on QueryParams.
```

## Conditions

`When` works the same as for class destinations. When the condition is false, the parameter
receives the type default (`null`, `0`, etc.) rather than the resolved value.

```csharp
Register<Source, MyRecord>()
    .BindMember(d => d.Label, opt => opt.From(s => s.Name.ToUpper()))
    .BindMember(d => d.Label, opt => opt.When(s => s.IsActive));
```

## Detection

Mercator detects record destinations by checking whether the type has **at least one public
constructor** and **none of them are parameterless**. This covers positional records and any
other type that follows the same pattern.

Types with no public constructor at all (e.g. a class with a private constructor) still cause
`Map` to throw `MissingMethodException`, as before.

## MapInto with records

`MapInto` accepts an existing record instance and applies the mapping using property setters.
Because `init`-only setters are accessible via reflection, this works at runtime. Keep in mind
that modifying a record this way bypasses the immutability guarantee that `init` is intended to
enforce.
