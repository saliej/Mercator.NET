# BindMember and BindPath

`BindMember` and `BindPath` let you override or augment the automatic convention mapping for a
specific destination property. Both return the fluent expression so calls can be chained.

---

## BindMember

Use `BindMember` for a **top-level** destination property.

### Signature

```csharp
IMappingExpression<TSource, TDestination> BindMember<TMember>(
    Expression<Func<TDestination, TMember>> destinationMember,
    Action<IMemberOptions<TSource, TMember>> configure);
```

### IMemberOptions

Inside the `configure` callback you have access to three options:

| Method | Effect |
|--------|--------|
| `From(Func<TSource, TMember> resolver)` | Provide a value factory called with the source object |
| `When(Func<TSource, bool> condition)` | Skip this member when the condition returns `false` |
| `Ignore()` | Suppress mapping for this member entirely |

### Resolver examples

```csharp
Register<Source, Dest>()
    // Rename: copy from a differently-named source property
    .BindMember(d => d.CustomerName, opt => opt.From(s => s.Name))

    // Computed: combine multiple source fields
    .BindMember(d => d.FullName, opt => opt.From(s => $"{s.First} {s.Last}"))

    // Constant: always the same value
    .BindMember(d => d.Source, opt => opt.From(_ => "Salesforce"))

    // Conversion: change type
    .BindMember(d => d.IdText, opt => opt.From(s => s.Id.ToString()))

    // Helper method
    .BindMember(d => d.Slug, opt => opt.From(s => Slugify(s.Name)));
```

### Condition examples

```csharp
Register<Source, Dest>()
    // Only copy Name when Id is positive
    .BindMember(d => d.Name, opt => opt.When(s => s.Id > 0))

    // Only copy Status when the entity is active
    .BindMember(d => d.Status, opt => opt.When(s => s.IsActive));
```

When the condition is `false`, the destination property keeps its default value (or whatever was
set by a prior step).

### Combining When and From

Both options can be set on the same member by calling `BindMember` twice, or by using the
convenience overload:

```csharp
// Two-call form: accumulates on the same member config entry
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()))
    .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10));

// Convenience overload: equivalent one-liner
Register<Source, Dest>()
    .BindMember(d => d.Name, when: s => s.Id > 10, from: s => s.Name.ToUpper());
```

When the condition is `false`, the `From` resolver is also skipped.

### Accumulation and last-call-wins

Multiple `BindMember` calls targeting the same property **accumulate** into one configuration
entry. Each call applies its option on top of whatever was set before:

```csharp
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.From(s => "first"))
    .BindMember(d => d.Name, opt => opt.From(s => "second")); // replaces "first"
```

This includes `Ignore` overriding a prior `From`, and `From` overriding a prior `Ignore`:

```csharp
// Ignore wins
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper()))
    .BindMember(d => d.Name, opt => opt.Ignore()); // Name is now ignored

// From wins (re-enables after ignore)
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.Ignore())
    .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper())); // Name is now mapped
```

### Empty configure action

Calling `BindMember` with a configure action that does nothing (i.e., no `From`, `When`, or
`Ignore` call) is a configuration error and throws `InvalidOperationException` at **construction
time**:

```csharp
// Throws at mapper construction: "Member 'Name' ... has a mapping entry with no resolver..."
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => { });
```

### When-only (no From)

`When` without `From` uses the **convention value** when the condition is true:

```csharp
// When Id > 10, copy Name by convention; otherwise leave Name at its default
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.When(s => s.Id > 10));
```

---

## BindPath

Use `BindPath` for a **nested** destination property. The path expression is a dot-separated
member access chain:

```csharp
Register<Source, Dest>()
    .BindPath(d => d.Request.Info.Name, opt => opt.From(s => s.Name));
```

### Signature

```csharp
IMappingExpression<TSource, TDestination> BindPath<TMember>(
    Expression<Func<TDestination, TMember>> destinationPath,
    Action<IMemberOptions<TSource, TMember>> configure);
```

### Null-safe intermediate objects

If any intermediate object in the path is `null` at the time of assignment, Mercator creates a
new instance via `Activator.CreateInstance` and assigns it before continuing:

```csharp
// Dest.Request is null; Mercator creates new Request(), then sets Info, then sets Name
mapper.Map<Dest>(source);
// result.Request != null
// result.Request.Info != null
// result.Request.Info.Name == "..."
```

All intermediate types must have a parameterless constructor.

### BindPath and Validation

When using `Strict` or `Warn` [validation](Validation), a `BindPath` call on
`d => d.Container.Value` covers the **top-level** `Container` property. Validation does not
report `Container` as unmapped even though the MemberMappings key is `Container.Value`.

### Null reference in resolver

If the `From` resolver throws `NullReferenceException` (e.g., because a nested source property
is `null`), Mercator catches the exception silently and skips the member. The destination
property retains its default value.

```csharp
Register<Source, Dest>()
    .BindPath(d => d.Info.Name, opt => opt.From(s => s.Address!.Street)); // safe if Address is null
```

---

## Interaction with transforms

`From` resolvers run **before** transforms. Any [Transform](Transforms) registered on the
mapping applies to the value returned by the resolver:

```csharp
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.From(s => s.Name))
    .Transform<string>(s => s?.Trim());
// Resolved value is trimmed before being written
```
