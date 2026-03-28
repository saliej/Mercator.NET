# Transforms

A transform is a function applied to every mapped value whose type matches `TValue`. Transforms
run after each individual member value is resolved but before it is written to the destination.

## Registering a transform

```csharp
Register<Source, Dest>()
    .Transform<string>(s => string.IsNullOrWhiteSpace(s) ? null : s.Trim());
```

The type parameter `TValue` determines which values the transform applies to.

## Scope

A transform applies to **all** values of the matching type within the same mapping registration —
both convention-matched properties and explicitly resolved values from `BindMember` / `BindPath`.

```csharp
Register<Source, Dest>()
    .BindMember(d => d.Code, opt => opt.From(s => s.RawCode))
    .Transform<string>(s => s?.ToUpper());

// Convention-matched string properties: transformed
// Code (from RawCode via From): also transformed
```

## Multiple transforms

Multiple `Transform` calls for the same `TValue` accumulate and execute in **declaration order**:

```csharp
Register<Source, Dest>()
    .Transform<string>(s => s?.Trim())
    .Transform<string>(s => string.IsNullOrEmpty(s) ? null : s);
// First trims, then converts empty string to null
```

## Value type transforms

Transforms work for value types too:

```csharp
Register<Source, Dest>()
    .Transform<decimal>(d => Math.Round(d, 2))
    .Transform<int>(i => Math.Abs(i));
```

Value type transforms only run when the value is non-null and is an instance of `TValue`. They
do not receive null inputs (null is not a valid boxed value type).

## Null handling for reference types

Reference type transforms **do** receive null:

```csharp
Register<Source, Dest>()
    .Transform<string>(s => s ?? "N/A"); // s can be null here
```

If the transform itself returns null, the value written to the destination is null (subject to
the non-nullable value-type guard that silently skips null-to-struct assignments).

## Transforms are not applied to ignored members

Properties suppressed by `IgnoreMember` or `Ignore()` are never assigned, so transforms do not
run for them:

```csharp
Register<Source, Dest>()
    .IgnoreMember(d => d.Name)
    .Transform<string>(s => s?.ToUpper()); // does not affect Name
```

## Type matching

The transform applies when the value is an instance of `TValue`. For reference types, this
includes null. For value types, null is excluded. Subtype values match a base-type transform:

```csharp
Register<Source, Dest>()
    .Transform<object>(v => ...); // matches all non-null reference types
```

## Common patterns

### Trim all strings

```csharp
.Transform<string>(s => s?.Trim())
```

### Normalise empty strings to null

```csharp
.Transform<string>(s => string.IsNullOrWhiteSpace(s) ? null : s)
```

### Replace null strings with a placeholder

```csharp
.Transform<string>(s => s ?? string.Empty)
```

### Round all decimal values

```csharp
.Transform<decimal>(d => Math.Round(d, 2))
```

### Uppercase all strings

```csharp
.Transform<string>(s => s?.ToUpper() ?? s)
```
