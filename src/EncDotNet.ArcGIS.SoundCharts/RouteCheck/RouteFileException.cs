namespace EncDotNet.SoundCharts.RouteCheck;

/// <summary>
/// A route file that can't be checked: not an S-421 or RTZ route, or one with
/// fewer than two waypoints. The message is the viewer's, ready to show.
/// </summary>
public sealed class RouteFileException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RouteFileException"/> class.</summary>
    public RouteFileException()
    {
    }

    /// <summary>Initializes a new instance with a message to show.</summary>
    /// <param name="message">The message, in the viewer's words.</param>
    public RouteFileException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message to show and the reader's exception.</summary>
    /// <param name="message">The message, in the viewer's words.</param>
    /// <param name="innerException">Why the file couldn't be read.</param>
    public RouteFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
