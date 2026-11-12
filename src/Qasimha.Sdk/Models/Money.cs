using System.Globalization;

namespace Qasimha.Sdk;

internal static class Money
{
    public static bool HasAtMostTwoDecimals(decimal value) => decimal.Round(value, 2) == value;

    public static long ToHalalas(decimal value) => (long)decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero);

    public static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    public static string FormatHalalas(long halalas) => Format(halalas / 100m);
}
