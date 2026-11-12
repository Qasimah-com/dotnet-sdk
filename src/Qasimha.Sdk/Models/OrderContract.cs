namespace Qasimha.Sdk;

/// <summary>Result of <see cref="OrdersResource.ContractAsync"/>: the financing contract document.</summary>
public sealed class OrderContract : ApiResource
{
    /// <summary>The document bytes.</summary>
    public byte[] Content { get; init; } = Array.Empty<byte>();

    /// <summary>The <c>Content-Type</c> the API sent, normally <c>application/pdf</c>.</summary>
    public string? ContentType { get; init; }

    /// <summary>The file name from <c>Content-Disposition</c>, when the API sent one.</summary>
    public string? FileName { get; init; }
}
