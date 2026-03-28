# Validation

Validation checks that every writable destination property is covered by at least one of:
convention mapping, an explicit `BindMember` / `BindPath` / `BindCollection` entry, or
`IgnoreMember`. It runs at **mapper construction time** so misconfigured mappings fail fast,
not during a production `Map` call.

## Modes

| Mode | Behaviour |
|------|-----------|
| `None` (default) | No validation. Unmapped properties are silently left at their default values. |
| `Warn` | Calls `WarnCallback` with a message listing unmapped members. Does not throw. |
| `Strict` | Throws `InvalidOperationException` listing all unmapped members. |

## Enabling validation

### Via DI

```csharp
builder.Services.AddMercator(
    opts => opts.ValidationMode = ValidationMode.Strict,
    typeof(MyRegistry).Assembly);
```

### Without DI

```csharp
var mapper = new MercatorMapper(
    registry.Configurations,
    new MercatorOptions { ValidationMode = ValidationMode.Strict });
```

## Strict mode

```csharp
// OrderDto has a 'ShippingAddress' property not on OrderEntity and not explicitly configured.
// Construction throws:
// "Unmapped destination members detected:
//  OrderEntity -> OrderDto: ShippingAddress"

var mapper = new MercatorMapper(
    registry.Configurations,
    new MercatorOptions { ValidationMode = ValidationMode.Strict });
```

Fix by adding an explicit binding or an ignore:

```csharp
Register<OrderEntity, OrderDto>()
    .BindMember(d => d.ShippingAddress, opt => opt.From(s => s.DeliveryAddress))
    // — or —
    .IgnoreMember(d => d.ShippingAddress);
```

## Warn mode

`Warn` mode calls `WarnCallback` (default: `Trace.TraceWarning`) instead of throwing. Useful
for gradual adoption when a codebase has many existing registrations.

```csharp
builder.Services.AddMercator(
    opts => opts.ValidationMode = ValidationMode.Warn,
    typeof(MyRegistry).Assembly);
```

### Custom warning handler

Supply a custom `WarnCallback` to redirect warnings to your logging infrastructure:

```csharp
builder.Services.AddMercator(
    opts =>
    {
        opts.ValidationMode = ValidationMode.Warn;
        opts.WarnCallback = msg => logger.LogWarning("Mercator: {Message}", msg);
    },
    typeof(MyRegistry).Assembly);
```

`WarnCallback` is an `Action<string>`. The default writes via
`System.Diagnostics.Trace.TraceWarning`, which is **not** stripped in release builds and flows
into any registered `TraceListener`.

## What counts as covered

A destination property named `Prop` is covered when **any** of the following holds:

| Coverage type | Condition |
|---------------|-----------|
| Convention | Source has a same-named readable property with a compatible type |
| Explicit entry | `MemberMappings` contains a key equal to `"Prop"` or starting with `"Prop."` |
| BindPath | `BindPath(d => d.Prop.Nested, ...)` — the top-level `Prop` is counted as covered |
| BindCollection | `BindCollection(d => d.Prop, ...)` |
| IgnoreMember | `IgnoreMember(d => d.Prop)` |

## Properties included in validation

Validation inspects all **writable** destination properties whose setter accessibility is
`public`, `internal`, or `protected`. This matches the same set used for convention mapping,
so a property with an `internal` setter that is covered by convention is not falsely reported
as unmapped.

## Reverse mapping validation

Reverse mappings created by `AndReverse()` are validated **independently** from the forward
mapping. A property that is covered in the forward direction may be unmapped in the reverse
direction if it does not exist on the other type.

```csharp
// SourceWithExtra has 'Id', 'Name', 'Extra'
// Dest has 'Id', 'Name'

Register<SourceWithExtra, Dest>().AndReverse();
// Forward (SourceWithExtra -> Dest): Id, Name covered by convention. OK.
// Reverse (Dest -> SourceWithExtra): Id, Name covered; Extra is NOT covered.
// Strict mode throws for the reverse direction.
```

## Vacuous BindMember detection

Calling `BindMember` with a configure action that applies no configuration (no `From`, `When`,
`Ignore`, and not a collection) is detected at construction time regardless of validation mode:

```csharp
// Always throws InvalidOperationException, even with ValidationMode.None:
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => { });
// "Member 'Name' on '...Dest' has a mapping entry with no resolver, condition,
//  ignore, or collection configuration."
```
