# Performance

## Design philosophy

Mercator pays an upfront cost at **construction time** — reflection, type analysis, expression
compilation — so the hot path at runtime is as cheap as possible. Once a `MercatorMapper` is
constructed:

- `Map<T>(source)` performs a single `Dictionary` lookup followed by a delegate invocation.
- No reflection occurs on the hot path for convention-matched properties.
- The mapper is a singleton; constructing it once at startup and reusing it is the intended
  pattern.

## Convention mapping: compiled delegates

Convention pairs are compiled into expression-tree delegates at construction time.

**Getter** — emitted as:
```csharp
(object src) => (object)((TSource)src).PropertyName
```

**Setter** — emitted as:
```csharp
(object dest, object? val) => ((TDest)dest).PropertyName = (TPropType)val
```

Non-nullable value-type properties include an additional null guard (captured at construction
time, not evaluated per-call for value types):
```csharp
(dest, val) => { if (val is not null) rawSetter(dest, val); }
```

## Benchmark

Measured on an **AMD Ryzen 7 5800X3D**, .NET 10.0.3, Release build, using
[BenchmarkDotNet](https://benchmarkdotnet.org). The test maps a 10-property type
(`PurchaseDto` → `Purchase`) where all properties are convention-matched.

| Method | Mean | Ratio | Allocated |
|--------|-----:|------:|----------:|
| Raw `PropertyInfo.GetValue` / `SetValue` | 421.3 ns | 1.00 | 256 B |
| Mercator compiled delegate | 122.8 ns | 0.29 | 256 B |

**3.4× faster** than direct reflection. Allocation is identical because both approaches
allocate the destination instance; neither introduces additional per-call allocation.

The benchmark source is in `Mercator.Benchmarks/Program.cs`.

## Where reflection is still used

The compiled-delegate optimization applies to **convention pairs** (step 1 of the mapping
function). Explicit `BindMember` / `BindPath` mappings and `BindCollection` paths use
`PropertyInfo.GetValue` / `SetValue` via `SetValueAtPath`, because they require runtime path
navigation. These paths are not on the hot path for typical mappings.

| Path | Mechanism |
|------|-----------|
| Convention matching | Compiled expression-tree delegates |
| `BindMember` / `BindPath` with `From` | `PropertyInfo` via `SetValueAtPath` |
| `BindCollection` | `PropertyInfo` via `SetValueAtPath` |
| `AfterMap` | Direct delegate invocation |

## Startup cost

Construction invokes `BuildConventionPairs` per registration, which calls `Expression.Compile()`
once per source/destination property pair. For a mapping with 10 convention properties, that is
10 getter + 10 setter compilations. This is intentionally front-loaded at DI startup.

Typical startup times (not benchmarked formally):
- A registry with 10 mappings of 10 properties each: single-digit milliseconds.
- Large applications with hundreds of mappings: tens of milliseconds.

This cost is paid once at `AddMercator` / `new MercatorMapper(...)` time. Never construct a
mapper per request.

## Memory

The mapper holds:
- One `Dictionary` entry per registered type pair.
- One compiled `Func<object, object?>` getter and one `Action<object, object?>` setter per
  convention property pair.
- Closure-captured references to member config snapshots.

Memory footprint is proportional to the number of registrations and the number of properties per
type. For typical application sizes (dozens to hundreds of mappings) this is negligible.

## Recommendations

- Register one `MercatorMapper` per application lifetime (singleton).
- Enable `ValidationMode.Strict` in development/CI to catch misconfiguration at startup rather
  than at runtime.
- Profile before optimising explicit `BindMember` paths; for most workloads, the hot path is
  convention mapping, which is already compiled.
