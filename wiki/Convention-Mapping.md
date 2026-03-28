# Convention Mapping

Convention mapping runs automatically for every registered pair. There is no opt-in: if a source
property and a destination property share a name and have compatible types, the value is copied.

## Matching rules

A source property and destination property are matched when **all** of the following hold:

1. **Same name** — exact, case-sensitive match on `PropertyInfo.Name`.
2. **Readable source** — the source property has a `public`, `internal`, or `protected` getter.
3. **Writable destination** — the destination property has a `public`, `internal`, or `protected`
   setter, including `init`-only setters (used by record properties).
4. **Compatible types** — the types pass the compatibility check described below.

Unmatched properties on either side are silently ignored unless [Validation](Validation) is
enabled.

## Type compatibility

Types are compatible when any of the following is true:

- `destination.IsAssignableFrom(source)` — the standard .NET assignability check, which covers
  same type, derived-to-base (covariance), and interface implementation.
- After unwrapping `Nullable<T>`, the underlying types are assignable. This means `int` ↔ `int?`
  and `int?` ↔ `int` are both considered compatible.

| Source type | Destination type | Compatible? |
|-------------|-----------------|-------------|
| `string` | `string` | Yes |
| `string` | `object` | Yes (string is assignable to object) |
| `DerivedClass` | `BaseClass` | Yes (derived is assignable to base) |
| `BaseClass` | `DerivedClass` | No |
| `int` | `int?` | Yes (nullable unwrap) |
| `int?` | `int` | Yes (nullable unwrap) |
| `int` | `string` | No |
| `List<int>` | `List<string>` | No |

> **Inheritance note:** Mercator uses the *declared* property type for compatibility checks, not
> the runtime type of the value. A source property declared as `DerivedAnimal` paired with a
> destination property declared as `BaseAnimal` will be convention-matched because `DerivedAnimal`
> is assignable to `BaseAnimal`. The reverse (`BaseAnimal` source → `DerivedAnimal` destination)
> is not matched.

## Non-public and init-only setters

Properties with `internal set`, `protected set`, or `init` are included in both convention
matching and [Validation](Validation) checks.

```csharp
public class Dest
{
    public int Id { get; set; }                  // public setter — matched
    public string? Name { get; internal set; }   // internal setter — matched
}

public record RecordDest(int Id, string? Name);  // init-only setters — matched
```

For `internal set`, the mapping code needs access to the assembly (e.g. via
`InternalsVisibleTo`).

> **Note:** When the destination is a positional record (no parameterless constructor), convention
> matches are used to resolve constructor arguments rather than to call setters directly. See
> [Record Mapping](Record-Mapping) for details.

## Runtime type lookup

`Map<TDestination>(object source)` looks up the mapping using `source.GetType()` — the **runtime
type**, not the declared type of the variable. This means passing a subclass instance uses
whatever mapping is registered for that subclass.

```csharp
// Only DerivedSource -> Dest is registered.
object src = new DerivedSource { Id = 1 };
mapper.Map<Dest>(src);  // works — runtime type is DerivedSource

BaseSource base = new DerivedSource { Id = 1 };
mapper.Map<Dest>(base); // also works — runtime type is still DerivedSource

BaseSource plain = new BaseSource { Id = 1 };
mapper.Map<Dest>(plain); // throws — runtime type is BaseSource, no mapping registered
```

## Precedence order

When a destination property appears in both convention pairs and `BindMember` / `IgnoreMember`
configurations, the following order applies:

1. **IgnoreMember** — the property is always skipped.
2. **BindMember with `When` condition only** — the convention value is copied when the condition
   is true; the property is skipped when false.
3. **BindMember with `From` resolver** — the resolver value is used instead of the convention
   value.
4. **Pure convention** — no `BindMember` entry; the source property value is copied directly.

## Execution order within a Map call

1. Convention pairs (compiled delegate hot path)
2. Explicit per-member mappings (`BindMember` / `BindPath`)
3. Collection mappings (`BindCollection`)
4. `AfterMap` callbacks

## Example

```csharp
public class Source
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ExtraValue { get; set; }
    public int Count { get; set; }       // no matching property on Dest
}

public class Dest
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ExtraValue { get; set; }
    public string? TextCount { get; set; } // no matching property on Source
}

// Result of Map<Dest>(new Source { Id=1, Name="A", ExtraValue="B", Count=5 }):
// Id = 1          — convention matched
// Name = "A"      — convention matched
// ExtraValue = "B"— convention matched
// TextCount = null — no convention match; stays at default
```

To map `Count` to `TextCount` explicitly, use `BindMember`:

```csharp
Register<Source, Dest>()
    .BindMember(d => d.TextCount, opt => opt.From(s => s.Count.ToString()));
```
