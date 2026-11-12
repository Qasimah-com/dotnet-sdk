namespace Qasimha.Sdk;

/// <summary>DNS, TLS, socket or timeout failure. <see cref="Exception.InnerException"/> is the transport's own exception.</summary>
public class ConnectionException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public ConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error wrapping the transport exception.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The transport exception.</param>
    public ConnectionException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
