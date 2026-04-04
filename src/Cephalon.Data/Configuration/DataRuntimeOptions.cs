namespace Cephalon.Data.Configuration;

/// <summary>
/// Describes the host-owned options for the runtime-neutral Cephalon data pack.
/// </summary>
public sealed class DataRuntimeOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataRuntimeOptions" /> class.
    /// </summary>
    public DataRuntimeOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pack should register the default read-store dispatcher.
    /// </summary>
    public bool RegisterReadStore { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the pack should register the default write-store dispatcher.
    /// </summary>
    public bool RegisterWriteStore { get; set; } = true;
}
