namespace EncDotNet.SoundCharts.Logging;

/// <summary>The severity of a <see cref="LogEntry"/>, lowest first.</summary>
public enum LogLevel
{
    /// <summary>Detail for diagnosing a problem; hidden by default.</summary>
    Debug,

    /// <summary>Something that happened as expected, such as a server start.</summary>
    Info,

    /// <summary>Something unexpected that SoundCharts recovered from, such as a server restart.</summary>
    Warning,

    /// <summary>Something that failed, such as a server that stopped for good.</summary>
    Error,
}
