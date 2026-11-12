namespace Qasimha.Sdk;

/// <summary>One field-level error from the envelope's <c>errors</c> array, or from the SDK's own pre-validation.</summary>
/// <param name="Field">The field, in the API's PascalCase (<c>TotalAmount</c>, <c>Items[0].Quantity</c>); <see langword="null"/> for a whole-request error.</param>
/// <param name="Message">What is wrong with it.</param>
public sealed record ApiFieldError(string? Field, string? Message);
