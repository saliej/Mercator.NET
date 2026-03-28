# Ignore Members

Use `IgnoreMember` (or `BindMember` with `Ignore()`) to prevent a destination property from
being populated during mapping.

## IgnoreMember

```csharp
Register<Source, Dest>()
    .IgnoreMember(d => d.SensitiveField)
    .IgnoreMember(d => d.InternalNotes);
```

`IgnoreMember` is shorthand for `BindMember(d => d.Prop, opt => opt.Ignore())`. The destination
property keeps its default value (typically `null` for reference types, `0` for numerics).

## Ignore via BindMember

```csharp
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.Ignore());
```

Both forms are equivalent.

## Ignore clears prior configuration

Calling `Ignore()` on a member that already has a `From` or `When` configuration removes that
configuration:

```csharp
Register<Source, Dest>()
    .BindMember(d => d.Name, opt => opt.From(s => s.Name.ToUpper())) // sets resolver
    .BindMember(d => d.Name, opt => opt.Ignore()); // clears resolver; Name is now ignored
```

## From/When re-enables after Ignore

`From` and `When` reset `IsIgnored` when called after `Ignore`, so the last call wins:

```csharp
Register<Source, Dest>()
    .IgnoreMember(d => d.Name)                                   // ignored
    .BindMember(d => d.Name, opt => opt.From(s => s.Name));     // re-enabled with resolver

// Name is mapped using the From resolver
```

```csharp
Register<Source, Dest>()
    .IgnoreMember(d => d.Name)
    .BindMember(d => d.Name, opt => opt.When(s => s.Id > 0));   // re-enabled with condition

// Name is copied by convention when Id > 0
```

## Ignore and Validation

An ignored member is **not** reported as unmapped by [Strict or Warn validation](Validation).
The explicit ignore entry is counted as coverage.

```csharp
// DestWithExtra has an 'Extra' property not present on Source.
// Without IgnoreMember, Strict mode throws.
// With IgnoreMember, Strict mode passes.
Register<Source, DestWithExtra>()
    .IgnoreMember(d => d.Extra);
```

## Ignore and reverse mappings

`IgnoreMember` is **not** propagated to reverse mappings created by `AndReverse`. The reverse
mapping is convention-only, so properties that exist on both sides are still copied by convention
in the reverse direction.

```csharp
Register<Source, Dest>()
    .IgnoreMember(d => d.Name)
    .AndReverse();

mapper.Map<Dest>(source).Name;   // null — ignored
mapper.Map<Source>(dest).Name;   // copied by convention — ignore does not apply in reverse
```

## Ignoring nested paths

You can ignore a top-level property that contains nested objects:

```csharp
Register<Source, DestWithNested>()
    .IgnoreMember(d => d.Nested); // entire Nested property is left null
```

To ignore only a specific nested property, combine `BindPath` with `IgnoreMember` style:

```csharp
Register<Source, Dest>()
    .BindPath(d => d.Nested!.SensitiveField, opt => opt.Ignore());
```

## Transforms are not applied to ignored members

A registered `Transform<string>` does not run for properties that are suppressed by ignore:

```csharp
Register<Source, Dest>()
    .IgnoreMember(d => d.Name)
    .Transform<string>(s => s?.ToUpper()); // Does NOT affect Name
```
