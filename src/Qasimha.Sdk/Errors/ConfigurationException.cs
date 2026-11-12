namespace Qasimha.Sdk;

/// <summary>The client is misconfigured, or an argument is invalid before any request is sent.</summary>
public class ConfigurationException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">Which rule was broken.</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error wrapping another exception.</summary>
    /// <param name="message">Which rule was broken.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ConfigurationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
