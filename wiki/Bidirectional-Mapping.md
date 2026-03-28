# Bidirectional Mapping

`AndReverse()` registers a convention-only mapping in the opposite direction
(`TDestination` → `TSource`) in a single call.

## Basic usage

```csharp
Register<Source, Dest>()
    .AndReverse();

// Now both mappings are available:
mapper.Map<Dest>(source);    // Source -> Dest
mapper.Map<Source>(dest);    // Dest -> Source (reverse, convention only)
```

## What the reverse mapping includes

The reverse mapping applies **only** convention matching. It does not invert or carry over:

- `BindMember` / `BindPath` explicit configurations
- `IgnoreMember` settings
- `Transform` registrations
- `AfterMap` callbacks

```csharp
Register<Source, Dest>()
    .BindMember(d => d.FullName, opt => opt.From(s => $"{s.First} {s.Last}"))
    .IgnoreMember(d => d.InternalCode)
    .Transform<string>(s => s?.ToUpper())
    .AfterMap((src, dest) => dest.AuditLog = "mapped")
    .AndReverse();

// Forward: FullName computed, InternalCode ignored, strings uppercased, AuditLog set
// Reverse: only same-name/compatible-type properties are copied; none of the above applies
```

## Detecting unmapped properties in reverse

[Validation](Validation) treats the reverse mapping independently. If `TSource` has properties
that do not exist on `TDest`, those become unmapped destination properties in the reverse
direction and are reported by `Strict` or `Warn` mode.

```csharp
// SourceWithExtra has an 'Extra' property that Dest does not have.
// Reverse mapping: Dest -> SourceWithExtra; SourceWithExtra.Extra is unmapped.
Register<SourceWithExtra, Dest>()
    .AndReverse();

// With Strict mode, this throws because Dest -> SourceWithExtra has 'Extra' unmapped.
```

To fix, explicitly ignore the property in the reverse direction by registering a separate
reverse mapping:

```csharp
Register<SourceWithExtra, Dest>();
Register<Dest, SourceWithExtra>()
    .IgnoreMember(d => d.Extra);
```

## AndReverse vs. two separate Register calls

`AndReverse()` is a convenience for the common case where both types share the same properties.
For asymmetric mappings (different property sets, different types, custom logic in both
directions), register each direction explicitly:

```csharp
Register<CreateOrderRequest, OrderEntity>()
    .BindMember(d => d.CreatedAt, opt => opt.From(_ => DateTime.UtcNow));

Register<OrderEntity, OrderDto>()
    .BindMember(d => d.Age, opt => opt.From(s => (DateTime.UtcNow - s.CreatedAt).Days));
```

## Chaining after AndReverse

`AndReverse()` returns the fluent expression so you can continue configuring the forward mapping:

```csharp
Register<Source, Dest>()
    .AndReverse()
    .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()));
// The additional BindMember applies only to the forward direction.
```
