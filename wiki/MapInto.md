# MapInto

`MapInto` populates an **existing** destination instance rather than creating a new one via
`Activator.CreateInstance`. The same mapping configuration (convention, `BindMember`, transforms,
`AfterMap`) applies.

## Signature

```csharp
TDestination MapInto<TDestination>(object source, TDestination destination);
```

Returns the destination instance for fluent use.

## Basic usage

```csharp
var existing = new OrderDto { CreatedBy = "system", Status = "draft" };

mapper.MapInto(orderEntity, existing);

// Properties covered by mapping are overwritten.
// Properties not covered (CreatedBy, Status in this example) are preserved.
```

## Difference from Map

| | `Map<T>` | `MapInto<T>` |
|--|---------|-------------|
| Destination instance | Created via `Activator.CreateInstance<T>()` | Provided by the caller |
| Unmapped properties | Default values | Pre-existing values preserved |
| Mapping logic | Identical | Identical |

`MapInto` is useful when:
- The destination type has no parameterless constructor.
- You want to patch an existing object (e.g., apply a partial update).
- The destination already holds values in properties that are not part of the mapping.

## Preserving existing values

```csharp
public class ProfileDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }   // not mapped from entity
    public DateTime LastSeen { get; set; }   // not mapped from entity
}

// Load the existing DTO (e.g., from cache)
var dto = cache.Get<ProfileDto>(userId);

// Update only the mapped fields from the freshly-loaded entity
mapper.MapInto(entity, dto);

// dto.AvatarUrl and dto.LastSeen are untouched
```

## AfterMap runs normally

`AfterMap` callbacks execute the same way as with `Map`:

```csharp
Register<Source, Dest>()
    .AfterMap((src, dest) => dest.MappedAt = DateTime.UtcNow);

var existing = new Dest();
mapper.MapInto(source, existing);
// existing.MappedAt is set
```

## Error conditions

| Condition | Exception |
|-----------|-----------|
| `source` is `null` | `ArgumentNullException` |
| `destination` is `null` | `ArgumentNullException` |
| No mapping registered for `source.GetType()` → `TDestination` | `InvalidOperationException` |

## No registered mapping

The lookup uses the runtime type of `source`, the same as `Map`:

```csharp
// Throws: no mapping registered from 'OrderEntity' to 'OrderDto'
mapper.MapInto(new OrderEntity(), new OrderDto()); // if no mapping registered
```
