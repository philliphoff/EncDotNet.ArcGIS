namespace EncDotNet.SoundCharts.Server;

/// <summary>A chart server process couldn't be started.</summary>
public sealed class ServerStartException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ServerStartException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What went wrong.</param>
    public ServerStartException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and its cause.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The cause.</param>
    public ServerStartException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
