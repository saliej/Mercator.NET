namespace Mercator;

using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Thread-safe singleton mapper. All reflection and delegate wiring happens at construction time;
/// the hot path (Map calls) performs only dictionary lookup and delegate invocation.
/// </summary>
internal sealed class MercatorMapper : IMapper
{
    // Key: (runtime source type, declared destination type)
    private readonly Dictionary<(Type Source, Type Destination), Action<object, object>> _mappings = new();

    // Factories for destination types that require constructor-based instantiation (e.g. records).
    private readonly Dictionary<(Type Source, Type Destination), Func<object, object>> _recordFactories = new();

    public MercatorMapper(IEnumerable<MappingConfiguration> configurations, MercatorOptions? options = null)
    {
        var configs = configurations.ToList();

        var reverseConfigs = configs
            .Where(c => c.HasReverse)
            .Select(c => new MappingConfiguration
            {
                SourceType = c.DestinationType,
                DestinationType = c.SourceType
            })
            .ToList();

        var allConfigs = configs.Concat(reverseConfigs).ToList();
        var conventionNameSets = new Dictionary<MappingConfiguration, HashSet<string>>();

        foreach (var config in allConfigs)
        {
            var pairs = BuildConventionPairs(config.SourceType, config.DestinationType);
            conventionNameSets[config] = pairs.Select(p => p.DestName).ToHashSet();
            var key = (config.SourceType, config.DestinationType);
            _mappings[key] = BuildMappingFunction(config, pairs, _mappings);

            if (RequiresConstructorInstantiation(config.DestinationType))
            {
                var ctor = FindPrimaryConstructor(config.DestinationType);
                if (ctor != null)
                    _recordFactories[key] = BuildRecordFactory(config, ctor, pairs);
            }
        }

        if (options is { ValidationMode: not ValidationMode.None })
            Validate(allConfigs, options, conventionNameSets);
    }

    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var key = (source.GetType(), typeof(TDestination));

        if (!_mappings.TryGetValue(key, out var mapFn))
            throw new InvalidOperationException(
                $"No mapping registered from '{source.GetType().FullName}' to " +
                $"'{typeof(TDestination).FullName}'. Ensure a Register call exists in a MappingRegistry.");

        if (_recordFactories.TryGetValue(key, out var factory))
            return (TDestination)factory(source);

        var destination = Activator.CreateInstance<TDestination>()
            ?? throw new InvalidOperationException(
                $"Cannot create an instance of '{typeof(TDestination).FullName}'. " +
                "Destination types must have a public parameterless constructor.");

        mapFn(source, destination);
        return destination;
    }

    public TDestination MapInto<TDestination>(object source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var key = (source.GetType(), typeof(TDestination));

        if (!_mappings.TryGetValue(key, out var mapFn))
            throw new InvalidOperationException(
                $"No mapping registered from '{source.GetType().FullName}' to " +
                $"'{typeof(TDestination).FullName}'. Ensure a Register call exists in a MappingRegistry.");

        mapFn(source, destination!);
        return destination;
    }

    private static void Validate(
        List<MappingConfiguration> configs,
        MercatorOptions options,
        Dictionary<MappingConfiguration, HashSet<string>> conventionNameSets)
    {
        var unmapped = new List<string>();

        foreach (var config in configs)
        {
            var conventionNames = conventionNameSets[config];

            var destProps = GetWritableDestinationProperties(config.DestinationType)
                .Select(p => p.Name);

            foreach (var name in destProps)
            {
                bool isCovered =
                    conventionNames.Contains(name) ||
                    config.MemberMappings.Keys.Any(k => k == name || k.StartsWith(name + "."));

                if (!isCovered)
                    unmapped.Add($"{config.SourceType.Name} -> {config.DestinationType.Name}: {name}");
            }
        }

        if (unmapped.Count == 0)
            return;

        var message = "Unmapped destination members detected:" + Environment.NewLine +
                      string.Join(Environment.NewLine, unmapped);

        if (options.ValidationMode == ValidationMode.Warn)
            options.WarnCallback(message);
        else
            throw new InvalidOperationException(message);
    }

    private static Action<object, object> BuildMappingFunction(
        MappingConfiguration config,
        List<(string DestName, Func<object, object?> Get, Action<object, object?> Set)> conventionPairs,
        Dictionary<(Type Source, Type Destination), Action<object, object>> mappings)
    {
        var memberMappings  = config.MemberMappings;
        var transforms      = config.Transforms;
        var afterMapActions = config.AfterMapActions;

        // F7: Detect vacuous member config entries at construction time
        foreach (var (path, memberConfig) in memberMappings)
        {
            if (!memberConfig.IsIgnored &&
                !memberConfig.IsCollection &&
                memberConfig.Resolver is null &&
                memberConfig.Condition is null)
            {
                throw new InvalidOperationException(
                    $"Member '{path}' on '{config.DestinationType.FullName}' has a mapping entry " +
                    "with no resolver, condition, ignore, or collection configuration. " +
                    "Remove the BindMember call or supply a From, When, or Ignore option.");
            }
        }

        return (src, dest) =>
        {
            // 1. Convention-based copy (compiled getter/setter delegates — no reflection on hot path)
            foreach (var (destName, get, set) in conventionPairs)
            {
                if (memberMappings.TryGetValue(destName, out var memberConfig))
                {
                    if (memberConfig.IsIgnored)
                        continue;

                    if (memberConfig.Resolver is null)
                    {
                        if (memberConfig.Condition is not null && memberConfig.Condition(src))
                        {
                            var conventionValue = get(src);
                            conventionValue = ApplyTransforms(conventionValue, transforms);
                            set(dest, conventionValue);
                        }
                    }
                    continue;
                }

                var value = get(src);
                value = ApplyTransforms(value, transforms);
                set(dest, value);
            }

            // 2. Explicit per-member mappings
            foreach (var (_, memberConfig) in memberMappings)
            {
                if (memberConfig.IsCollection) continue;  // handled in step 3
                if (memberConfig.IsIgnored)
                    continue;

                if (memberConfig.Resolver is null)
                    continue;

                if (memberConfig.Condition is not null && !memberConfig.Condition(src))
                    continue;

                object? value;
                try
                {
                    value = memberConfig.Resolver(src);
                }
                catch (NullReferenceException)
                {
                    continue;
                }

                value = ApplyTransforms(value, transforms);
                SetValueAtPath(dest, memberConfig.DestinationPath, value);
            }

            // 3. Collection mappings
            foreach (var (_, memberConfig) in memberMappings)
            {
                if (!memberConfig.IsCollection)
                    continue;

                if (memberConfig.CollectionSourceSelector is null)
                    continue;

                var sourceCollection = memberConfig.CollectionSourceSelector(src);
                if (sourceCollection is null)
                {
                    SetValueAtPath(dest, memberConfig.DestinationPath, null);
                    continue;
                }

                var sourceElementType = memberConfig.CollectionSourceElementType
                    ?? throw new InvalidOperationException($"Collection source element type not set for '{memberConfig.DestinationPath}'.");

                var destElementType = memberConfig.CollectionDestElementType
                    ?? throw new InvalidOperationException($"Collection destination element type not set for '{memberConfig.DestinationPath}'.");

                var listType = typeof(List<>).MakeGenericType(destElementType);
                var destList = (System.Collections.IList)(Activator.CreateInstance(listType)
                    ?? throw new InvalidOperationException($"Cannot create list of type '{listType.FullName}'."))!;

                var elementMappingKey = (sourceElementType, destElementType);
                if (!mappings.TryGetValue(elementMappingKey, out var elementMapFn))
                    throw new InvalidOperationException(
                        $"No mapping registered from '{sourceElementType.FullName}' to " +
                        $"'{destElementType.FullName}'. Collection mapping requires an element-level mapping to be registered.");

                foreach (var sourceElement in sourceCollection)
                {
                    var destElement = Activator.CreateInstance(destElementType)
                        ?? throw new InvalidOperationException($"Cannot create instance of '{destElementType.FullName}'.");
                    elementMapFn(sourceElement!, destElement);
                    destList.Add(destElement);
                }

                SetValueAtPath(dest, memberConfig.DestinationPath, destList);
            }

            // 4. AfterMap callbacks
            foreach (var action in afterMapActions)
                action(src, dest);
        };
    }

    private static IEnumerable<PropertyInfo> GetWritableDestinationProperties(Type destType)
        => destType
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p =>
                p.CanWrite &&
                (p.SetMethod?.IsPublic == true ||
                 p.SetMethod?.IsAssembly == true ||
                 p.SetMethod?.IsFamily == true));

    private static List<(string DestName, Func<object, object?> Get, Action<object, object?> Set)>
        BuildConventionPairs(Type sourceType, Type destType)
    {
        var srcByName = sourceType
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.CanRead && (p.GetMethod?.IsPublic == true || p.GetMethod?.IsAssembly == true || p.GetMethod?.IsFamily == true))
            .ToDictionary(p => p.Name);

        var pairs = new List<(string, Func<object, object?>, Action<object, object?>)>();

        foreach (var destProp in GetWritableDestinationProperties(destType))
        {
            if (!srcByName.TryGetValue(destProp.Name, out var srcProp))
                continue;
            if (!AreTypesCompatible(srcProp.PropertyType, destProp.PropertyType))
                continue;

            pairs.Add((destProp.Name, CompileGetter(srcProp, sourceType), CompileSetter(destProp, destType)));
        }

        return pairs;
    }

    private static Func<object, object?> CompileGetter(PropertyInfo srcProp, Type sourceType)
    {
        var param = Expression.Parameter(typeof(object), "src");
        var body = Expression.Convert(
            Expression.Property(Expression.Convert(param, sourceType), srcProp),
            typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, param).Compile();
    }

    private static Action<object, object?> CompileSetter(PropertyInfo destProp, Type destType)
    {
        // Init-only setters carry a modreq that expression trees cannot target in all runtimes;
        // use reflection for them so both convention-based and MapInto paths work on records.
        if (IsInitOnlySetter(destProp))
        {
            if (destProp.PropertyType.IsValueType && Nullable.GetUnderlyingType(destProp.PropertyType) is null)
                return (dest, val) => { if (val is not null) destProp.SetValue(dest, val); };
            return (dest, val) => destProp.SetValue(dest, val);
        }

        var destParam = Expression.Parameter(typeof(object), "dest");
        var valParam  = Expression.Parameter(typeof(object), "val");
        var body = Expression.Assign(
            Expression.Property(Expression.Convert(destParam, destType), destProp),
            Expression.Convert(valParam, destProp.PropertyType));
        var rawSetter = Expression.Lambda<Action<object, object?>>(body, destParam, valParam).Compile();

        // Preserve the null guard from TrySetProperty: skip assignment when value is null
        // and destination property type is a non-nullable value type.
        if (destProp.PropertyType.IsValueType && Nullable.GetUnderlyingType(destProp.PropertyType) is null)
            return (dest, val) => { if (val is not null) rawSetter(dest, val); };

        return rawSetter;
    }

    private static bool IsInitOnlySetter(PropertyInfo prop)
    {
        var setter = prop.SetMethod;
        if (setter == null) return false;
        return setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
    }

    // Returns true when the destination type has public constructors but none are parameterless,
    // meaning it requires constructor-based instantiation (e.g. a positional record).
    private static bool RequiresConstructorInstantiation(Type type)
    {
        var publicCtors = type.GetConstructors();
        return publicCtors.Length > 0 && publicCtors.All(c => c.GetParameters().Length > 0);
    }

    private static ConstructorInfo? FindPrimaryConstructor(Type type)
        => type.GetConstructors()
               .OrderByDescending(c => c.GetParameters().Length)
               .FirstOrDefault();

    private static Func<object, object> BuildRecordFactory(
        MappingConfiguration config,
        ConstructorInfo ctor,
        List<(string DestName, Func<object, object?> Get, Action<object, object?> Set)> conventionPairs)
    {
        var parameters = ctor.GetParameters();
        var memberMappings  = config.MemberMappings;
        var transforms      = config.Transforms;
        var afterMapActions = config.AfterMapActions;

        var conventionGetters = conventionPairs.ToDictionary(
            p => p.DestName,
            p => p.Get,
            StringComparer.OrdinalIgnoreCase);

        var paramResolvers = new List<Func<object, object?>>(parameters.Length);
        foreach (var param in parameters)
        {
            var pascalName = char.ToUpperInvariant(param.Name![0]) + param.Name[1..];

            if (memberMappings.TryGetValue(pascalName, out var memberConfig) &&
                !memberConfig.IsIgnored &&
                memberConfig.Resolver != null)
            {
                var resolver  = memberConfig.Resolver;
                var condition = memberConfig.Condition;
                var paramDefault = GetParameterDefault(param);
                paramResolvers.Add(src =>
                {
                    if (condition != null && !condition(src)) return paramDefault;
                    try { return resolver(src); }
                    catch (NullReferenceException) { return paramDefault; }
                });
            }
            else if (conventionGetters.TryGetValue(pascalName, out var getter))
            {
                paramResolvers.Add(getter);
            }
            else
            {
                var paramDefault = GetParameterDefault(param);
                paramResolvers.Add(_ => paramDefault);
            }
        }

        return src =>
        {
            var args = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var value = paramResolvers[i](src);
                value = ApplyTransforms(value, transforms);
                args[i] = value;
            }

            var dest = ctor.Invoke(args);

            foreach (var action in afterMapActions)
                action(src, dest);

            return dest;
        };
    }

    private static object? GetParameterDefault(ParameterInfo param)
    {
        if (param.HasDefaultValue) return param.DefaultValue;
        if (param.ParameterType.IsValueType) return Activator.CreateInstance(param.ParameterType);
        return null;
    }

    private static bool AreTypesCompatible(Type source, Type destination)
    {
        if (destination.IsAssignableFrom(source))
            return true;

        var srcCore  = Nullable.GetUnderlyingType(source)      ?? source;
        var destCore = Nullable.GetUnderlyingType(destination) ?? destination;
        return destCore.IsAssignableFrom(srcCore);
    }

    private static object? ApplyTransforms(
        object? value,
        List<(Type ValueType, Func<object?, object?> Transform)> transforms)
    {
        if (transforms.Count == 0)
            return value;

        foreach (var (valueType, transform) in transforms)
        {
            if (valueType.IsValueType)
            {
                if (value is not null && valueType.IsInstanceOfType(value))
                    value = transform(value);
            }
            else
            {
                if (value is null || valueType.IsInstanceOfType(value))
                    value = transform(value);
            }
        }
        return value;
    }

    private static void TrySetProperty(PropertyInfo property, object target, object? value)
    {
        if (value is null &&
            property.PropertyType.IsValueType &&
            Nullable.GetUnderlyingType(property.PropertyType) is null)
            return;

        property.SetValue(target, value);
    }

    private static void SetValueAtPath(object root, string path, object? value)
    {
        var segments = path.Split('.');
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var prop = ResolveProperty(current, segments[i]);
            var next = prop.GetValue(current);

            if (next is null)
            {
                next = Activator.CreateInstance(prop.PropertyType)
                    ?? throw new InvalidOperationException(
                        $"Cannot create instance of '{prop.PropertyType.FullName}' " +
                        $"for path segment '{segments[i]}'.");
                prop.SetValue(current, next);
            }

            current = next;
        }

        TrySetProperty(ResolveProperty(current, segments[^1]), current, value);
    }

    private static PropertyInfo ResolveProperty(object target, string name)
        => target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
           ?? throw new InvalidOperationException(
               $"Property '{name}' not found on '{target.GetType().FullName}'.");
}
