# API Reference

All public types are in the `Mercator` namespace.

---

## IMapper

```csharp
public interface IMapper
```

The main runtime interface. Implementations are thread-safe singletons.

### Methods

#### Map

```csharp
TDestination Map<TDestination>(object source);
```

Creates a new `TDestination` instance and populates it from `source`.

- If `TDestination` has a public parameterless constructor, it is created via
  `Activator.CreateInstance<TDestination>()` and properties are set by the mapping function.
- If `TDestination` has public constructors but none are parameterless (e.g. a positional
  record), the constructor with the most parameters is invoked with arguments resolved from
  `source`. See [Record Mapping](Record-Mapping).

| Parameter | Description |
|-----------|-------------|
| `source` | The source object. Must not be `null`. The **runtime** type is used for the mapping lookup. |

**Throws:**
- `ArgumentNullException` — `source` is `null`
- `InvalidOperationException` — no mapping registered for `(source.GetType(), typeof(TDestination))`
- `MissingMethodException` — `TDestination` has no accessible public constructor

#### MapInto

```csharp
TDestination MapInto<TDestination>(object source, TDestination destination);
```

Populates an existing `destination` instance from `source`. Same mapping logic as `Map`.
Unmapped properties on the destination retain their existing values.

| Parameter | Description |
|-----------|-------------|
| `source` | The source object. Must not be `null`. |
| `destination` | The destination instance to populate. Must not be `null`. |

Returns `destination`.

**Throws:**
- `ArgumentNullException` — `source` or `destination` is `null`
- `InvalidOperationException` — no mapping registered

---

## IMappingExpression\<TSource, TDestination\>

```csharp
public interface IMappingExpression<TSource, TDestination>
```

Fluent builder returned by `MappingRegistry.Register<TSource, TDestination>()`. All methods
return `this` for chaining.

### Methods

#### BindMember (configure overload)

```csharp
IMappingExpression<TSource, TDestination> BindMember<TMember>(
    Expression<Func<TDestination, TMember>> destinationMember,
    Action<IMemberOptions<TSource, TMember>> configure);
```

Configures an explicit mapping for a single top-level destination property. Multiple calls for
the same property accumulate. See [BindMember and BindPath](BindMember-and-BindPath).

#### BindMember (when + from overload)

```csharp
IMappingExpression<TSource, TDestination> BindMember<TMember>(
    Expression<Func<TDestination, TMember>> destinationMember,
    Func<TSource, bool> when,
    Func<TSource, TMember> from);
```

Convenience shorthand. Equivalent to calling `BindMember` twice — once with `When` and once
with `From`.

#### BindPath

```csharp
IMappingExpression<TSource, TDestination> BindPath<TMember>(
    Expression<Func<TDestination, TMember>> destinationPath,
    Action<IMemberOptions<TSource, TMember>> configure);
```

Configures an explicit mapping for a nested destination path (e.g., `d => d.Info.Address.City`).
Null intermediate objects are created via `Activator.CreateInstance`. See
[BindMember and BindPath](BindMember-and-BindPath#bindpath).

#### IgnoreMember

```csharp
IMappingExpression<TSource, TDestination> IgnoreMember<TMember>(
    Expression<Func<TDestination, TMember>> destinationMember);
```

Prevents the destination property from being populated. Shorthand for
`BindMember(d => d.Prop, opt => opt.Ignore())`. See [Ignore Members](Ignore-Members).

#### Transform

```csharp
IMappingExpression<TSource, TDestination> Transform<TValue>(Func<TValue, TValue> transform);
```

Registers a post-resolution value transform for all values of type `TValue`. Multiple calls
accumulate and execute in declaration order. See [Transforms](Transforms).

#### AfterMap

```csharp
IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action);
```

Registers a callback invoked after all property assignments complete. Multiple callbacks
accumulate and execute in declaration order. See [AfterMap](AfterMap).

#### AndReverse

```csharp
IMappingExpression<TSource, TDestination> AndReverse();
```

Registers a convention-only reverse mapping (`TDestination` → `TSource`). Explicit
configurations and callbacks are **not** inverted. See [Bidirectional Mapping](Bidirectional-Mapping).

#### BindCollection

```csharp
IMappingExpression<TSource, TDestination> BindCollection<TSourceElement, TDestElement>(
    Expression<Func<TDestination, List<TDestElement>?>> destinationMember,
    Func<TSource, IEnumerable<TSourceElement>?> sourceSelector);
```

Maps a source collection to a `List<TDestElement>` on the destination. A separate element-level
mapping (`TSourceElement` → `TDestElement`) must be registered. See
[Collection Mapping](Collection-Mapping).

---

## IMemberOptions\<TSource, TMember\>

```csharp
public interface IMemberOptions<TSource, TMember>
```

Passed to the `configure` callback in `BindMember` / `BindPath`.

### Methods

#### From

```csharp
void From(Func<TSource, TMember> resolver);
```

Sets a value factory for this member. Resets `IsIgnored` if previously set.

#### When

```csharp
void When(Func<TSource, bool> condition);
```

Sets a guard condition. The member is skipped when the condition returns `false`. When used
without `From`, the convention value is used when the condition is `true`. Resets `IsIgnored`
if previously set.

#### Ignore

```csharp
void Ignore();
```

Suppresses all mapping for this member. Clears any prior `From` and `When` configuration.

---

## MappingRegistry

```csharp
public abstract class MappingRegistry
```

Base class for declaring type mappings. Subclass and call `Register` from the constructor.

### Methods

#### Register

```csharp
protected IMappingExpression<TSource, TDestination> Register<TSource, TDestination>();
```

Declares a mapping. Returns the fluent expression for configuration. Multiple calls to
`Register` for the same pair override each other; the last registration wins.

---

## MercatorOptions

```csharp
public sealed class MercatorOptions
```

Options passed to `MercatorMapper` or configured via `AddMercator`.

### Properties

#### ValidationMode

```csharp
public ValidationMode ValidationMode { get; set; } = ValidationMode.None;
```

Controls whether unmapped destination properties are reported at construction time.

#### WarnCallback

```csharp
public Action<string> WarnCallback { get; init; }
```

Called with the warning message when `ValidationMode` is `Warn`. Defaults to
`Trace.TraceWarning(msg)`, which is not stripped in release builds. Override to redirect to
`ILogger` or another sink.

---

## ValidationMode

```csharp
public enum ValidationMode
{
    None,    // No validation (default)
    Warn,    // Call WarnCallback; do not throw
    Strict   // Throw InvalidOperationException
}
```

---

## MercatorServiceCollectionExtensions

```csharp
public static class MercatorServiceCollectionExtensions
```

### AddMercator (no options)

```csharp
public static IServiceCollection AddMercator(
    this IServiceCollection services,
    params Assembly[] assemblies);
```

Scans assemblies and registers `IMapper` as a singleton with default options.

### AddMercator (with options)

```csharp
public static IServiceCollection AddMercator(
    this IServiceCollection services,
    Action<MercatorOptions>? configure,
    params Assembly[] assemblies);
```

Scans assemblies and registers `IMapper` as a singleton. The `configure` delegate (if provided)
is invoked to mutate a fresh `MercatorOptions` before the mapper is constructed.

Both overloads return `IServiceCollection` for chaining.

---

## Exception reference

| Exception | When |
|-----------|------|
| `ArgumentNullException` | `Map` or `MapInto` called with a null argument |
| `InvalidOperationException` — no mapping | `Map` / `MapInto` called for an unregistered type pair |
| `InvalidOperationException` — unmapped members | `Strict` validation at construction time |
| `InvalidOperationException` — vacuous BindMember | `BindMember` called with an empty configure action |
| `InvalidOperationException` — missing element mapping | `BindCollection` mapped at runtime but element mapping is not registered |
| `InvalidOperationException` — path segment | A segment in a `BindPath` / `BindCollection` nested path cannot be resolved or instantiated |
| `MissingMethodException` | `TDestination` has no accessible public constructor (e.g. only private constructors) |
