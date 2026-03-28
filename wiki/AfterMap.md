# AfterMap

`AfterMap` registers a callback that runs after all property assignments (convention, explicit,
and collection) are complete. The callback receives the typed source and destination instances.

## Basic usage

```csharp
Register<Source, Dest>()
    .AfterMap((src, dest) =>
    {
        dest.MappedAt = DateTime.UtcNow;
        dest.SourceId  = src.Id;
    });
```

## Multiple callbacks

Multiple `AfterMap` calls accumulate and execute in **declaration order**:

```csharp
Register<Source, Dest>()
    .AfterMap((src, dest) => dest.Step1 = true)
    .AfterMap((src, dest) => dest.Step2 = true)
    .AfterMap((src, dest) => dest.Step3 = true);
// Executes in order: Step1, Step2, Step3
```

## Overwriting convention values

`AfterMap` runs after all mapping steps, so it can overwrite any property that was set
by convention or `BindMember`:

```csharp
Register<Source, Dest>()
    .AfterMap((src, dest) => dest.Name = "overwritten");
// dest.Name is always "overwritten" regardless of the convention value
```

## Interaction with explicit mappings

`AfterMap` can read values that were set by `BindMember` or `BindPath`:

```csharp
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()))
    .AfterMap((src, dest) =>
    {
        // dest.Name is already "UPPER CASE" here
        dest.NameLength = dest.Name?.Length ?? 0;
    });
```

## AfterMap and reverse mappings

`AfterMap` callbacks are **not** propagated to reverse mappings created by `AndReverse`. Only
the forward direction runs the callbacks.

```csharp
var count = 0;

Register<Source, Dest>()
    .AfterMap((src, dest) => count++)
    .AndReverse();

mapper.Map<Dest>(source);   // count == 1
mapper.Map<Source>(dest);   // count still == 1 — reverse does not run AfterMap
```

## AfterMap and MapInto

`AfterMap` runs normally when using `MapInto`:

```csharp
Register<Source, Dest>()
    .AfterMap((src, dest) => dest.AuditUser = CurrentUser.Name);

var existing = new Dest { Id = 1 };
mapper.MapInto(source, existing);
// existing.AuditUser is set
```

## Common patterns

### Audit fields

```csharp
Register<CreateRequest, Entity>()
    .AfterMap((req, entity) =>
    {
        entity.CreatedBy = req.UserId;
        entity.CreatedAt = DateTime.UtcNow;
    });
```

### Computed fields that depend on mapped values

```csharp
Register<OrderEntity, OrderDto>()
    .AfterMap((entity, dto) =>
    {
        dto.TotalFormatted = dto.Total.ToString("C");
        dto.IsLargeOrder   = dto.Total > 1000m;
    });
```

### Conditional post-processing

```csharp
Register<Source, Dest>()
    .AfterMap((src, dest) =>
    {
        if (src.IsArchived)
            dest.DisplayName = $"[Archived] {dest.DisplayName}";
    });
```
