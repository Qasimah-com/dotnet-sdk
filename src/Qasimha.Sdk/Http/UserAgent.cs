using System.Runtime.InteropServices;
using System.Text;

namespace Qasimha.Sdk.Http;

internal static class UserAgent
{
    public static string Build(string? suffix)
    {
        var product = FormattableString.Invariant(
            $"qasimha-sdk-dotnet/{SdkVersion.Current} (.NET/{System.Environment.Version}; {RuntimeInformation.RuntimeIdentifier})");
        var extra = Sanitize(suffix);
        return extra.Length == 0 ? product : product + " " + extra;
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(c is >= ' ' and <= '~' ? c : ' ');
        }

        return builder.ToString().Trim();
    }
}
