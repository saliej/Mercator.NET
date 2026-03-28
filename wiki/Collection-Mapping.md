# Collection Mapping

`BindCollection` maps an `IEnumerable<TSourceElement>` on the source to a `List<TDestElement>`
on the destination. Each element is mapped using a separately registered element mapping.

## Basic usage

```csharp
// Element mapping must be registered separately
Register<AddressEntity, AddressDto>();

// Parent mapping uses BindCollection
Register<CustomerEntity, CustomerDto>()
    .BindCollection<AddressEntity, AddressDto>(
        d => d.Addresses,          // destination property (must be List<TDestElement>?)
        s => s.Addresses);         // source selector (returns IEnumerable<TSourceElement>?)
```

The source selector is a plain `Func<TSource, IEnumerable<TSourceElement>?>` — it does not need
to be an expression. You can project, filter, or order here:

```csharp
Register<Order, OrderDto>()
    .BindCollection<LineItem, LineItemDto>(
        d => d.Lines,
        s => s.Lines.Where(l => !l.IsCancelled).OrderBy(l => l.SortOrder));
```

## Element mapping requirement

The element-level mapping must be registered **before** `Map` is called (it does not need to be
in the same registry). If no element mapping is found at runtime, `Map` throws
`InvalidOperationException`:

```
No mapping registered from 'AddressEntity' to 'AddressDto'.
Collection mapping requires an element-level mapping to be registered.
```

The element mapping can include its own `BindMember`, `Transform`, and `AfterMap` configuration.

## Null source collection

When the source selector returns `null`, the destination property is set to `null` (for reference
types). No exception is thrown, and no elements are iterated.

```csharp
mapper.Map<CustomerDto>(new CustomerEntity { Addresses = null });
// result.Addresses == null
```

## Empty source collection

When the source selector returns an empty collection, the destination property is set to an
empty `List<TDestElement>`:

```csharp
mapper.Map<CustomerDto>(new CustomerEntity { Addresses = new List<AddressEntity>() });
// result.Addresses != null
// result.Addresses.Count == 0
```

## Nested destination path

`BindCollection` supports dot-separated destination paths via the expression parameter:

```csharp
Register<Source, Dest>()
    .BindCollection<ItemEntity, ItemDto>(
        d => d.Container!.Items,   // nested path: Dest.Container.Items
        s => s.Items);
```

If `Container` is `null` at mapping time, Mercator creates a new `Container` instance before
assigning `Items`. All intermediate types must have a parameterless constructor.

When the source collection is `null` and the destination path is nested, Mercator navigates the
path (creating intermediate objects as needed) and sets the final property to `null`.

## Coexistence with convention mapping

`BindCollection` coexists with convention-matched properties. Convention mapping runs first and
does not copy collection properties registered via `BindCollection` (the two paths are separate).

```csharp
Register<Source, Dest>()
    .BindCollection<Item, ItemDto>(d => d.Items, s => s.Items);

// Source.Id and Source.Name are still copied by convention.
// Source.Items is handled by BindCollection in a separate pass.
```

## Validation and BindCollection

A destination property covered by `BindCollection` is recognised as explicitly configured during
[Validation](Validation). Strict mode does not report it as unmapped.

## Step execution order

Collection mappings execute in **step 3** of the mapping function, after convention mapping
(step 1) and explicit per-member mappings (step 2), but before `AfterMap` callbacks (step 4).
This means an `AfterMap` callback can inspect or further modify the mapped collection.

## Full example

```csharp
public class OrderEntity
{
    public int Id { get; set; }
    public string? Reference { get; set; }
    public List<LineItemEntity>? Lines { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string? Reference { get; set; }
    public List<LineItemDto>? Lines { get; set; }
}

public class LineItemEntity
{
    public string? ProductCode { get; set; }
    public int Quantity { get; set; }
}

public class LineItemDto
{
    public string? ProductCode { get; set; }
    public int Quantity { get; set; }
}

public class OrderMappingRegistry : MappingRegistry
{
    public OrderMappingRegistry()
    {
        Register<LineItemEntity, LineItemDto>();

        Register<OrderEntity, OrderDto>()
            .BindCollection<LineItemEntity, LineItemDto>(d => d.Lines, s => s.Lines);
    }
}
```
