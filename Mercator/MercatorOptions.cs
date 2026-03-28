namespace Mercator;

/// <summary>
/// Options that control mapper construction-time behaviour such as validation and warning output.
/// </summary>
public sealed class MercatorOptions
{
    /// <summary>
    /// Controls whether unmapped destination properties are reported at construction time.
    /// Defaults to <see cref="ValidationMode.None"/>.
    /// </summary>
    public ValidationMode ValidationMode { get; set; } = ValidationMode.None;

    /// <summary>
    /// Called with the warning message when <see cref="ValidationMode"/> is
    /// <see cref="ValidationMode.Warn"/>. Defaults to writing via
    /// <see cref="System.Diagnostics.Trace.TraceWarning(string)"/>, which flows into any
    /// registered <see cref="System.Diagnostics.TraceListener"/> and is not stripped in
    /// release builds. Set to a custom delegate to redirect output (e.g., to a test logger).
    /// </summary>
    public Action<string> WarnCallback { get; init; } =
        msg => System.Diagnostics.Trace.TraceWarning(msg);
}

/// <summary>
/// Controls whether unmapped destination properties are reported during mapper construction.
/// </summary>
public enum ValidationMode
{
    /// <summary>No validation. Unmapped properties are silently left at their default values.</summary>
    None,
    /// <summary>Calls <see cref="MercatorOptions.WarnCallback"/> with the warning message. Does not throw.</summary>
    Warn,
    /// <summary>Throws <see cref="System.InvalidOperationException"/> listing all unmapped members.</summary>
    Strict
}
