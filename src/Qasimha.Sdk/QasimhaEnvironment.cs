namespace Qasimha.Sdk;

/// <summary>The Qasimha environment a client talks to.</summary>
public enum QasimhaEnvironment
{
    /// <summary>The sandbox environment, reached with an <c>sk_test_</c> key.</summary>
    Sandbox,

    /// <summary>The production environment, reached with an <c>sk_live_</c> key.</summary>
    Production,
}
