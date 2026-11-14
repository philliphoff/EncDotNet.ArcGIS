namespace EncDotNet.SoundCharts.Export;

/// <summary>A SoundCharts layer couldn't be rendered for an export.</summary>
public sealed class ChartRasterException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ChartRasterException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What failed.</param>
    public ChartRasterException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and its cause.</summary>
    /// <param name="message">What failed.</param>
    /// <param name="innerException">The cause.</param>
    public ChartRasterException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
