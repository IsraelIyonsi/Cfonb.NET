namespace Cfonb;

internal static class CfonbAmount
{
    private const int HighWordShift = 32;

    private const char PositiveZero = '{';
    private const char NegativeZero = '}';
    private const char PositiveOne = 'A';
    private const char PositiveNine = 'I';
    private const char NegativeOne = 'J';
    private const char NegativeNine = 'R';

    internal static bool TryDecode(ReadOnlySpan<char> field, int scale, out decimal amount)
    {
        amount = 0m;
        if (field.Length == 0 || scale < CfonbLayout.MinScale || scale > CfonbLayout.MaxScale)
        {
            return false;
        }

        if (!TryDecodeSpecifier(field[^1], out int lastDigit, out bool negative))
        {
            return false;
        }

        ulong magnitude = 0;
        for (int i = 0; i < field.Length - 1; i++)
        {
            char c = field[i];
            if (c < '0' || c > '9')
            {
                return false;
            }

            magnitude = (magnitude * CfonbLayout.DecimalBase) + (ulong)(c - '0');
        }

        magnitude = (magnitude * CfonbLayout.DecimalBase) + (ulong)lastDigit;
        bool isNegative = negative && magnitude != 0;
        amount = new decimal((int)(magnitude & uint.MaxValue), (int)(magnitude >> HighWordShift), 0, isNegative, (byte)scale);
        return true;
    }

    private static bool TryDecodeSpecifier(char c, out int digit, out bool negative)
    {
        digit = 0;
        negative = false;

        if (c == PositiveZero)
        {
            return true;
        }

        if (c == NegativeZero)
        {
            negative = true;
            return true;
        }

        if (c >= PositiveOne && c <= PositiveNine)
        {
            digit = c - PositiveOne + 1;
            return true;
        }

        if (c >= NegativeOne && c <= NegativeNine)
        {
            digit = c - NegativeOne + 1;
            negative = true;
            return true;
        }

        return false;
    }
}
