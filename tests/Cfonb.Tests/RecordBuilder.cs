using System.Text;

namespace Cfonb.Tests;

/// <summary>
/// Builds 120-character records from the 1-based positions published in the CFONB brochure
/// "Releve de compte sur support informatique" (July 2004), tables 1, 2, 2bis and 3.
/// </summary>
internal static class RecordBuilder
{
    internal const int RecordLength = 120;

    internal static string Build(params (int Position, string Value)[] fields)
    {
        var chars = new StringBuilder(new string(' ', RecordLength));
        foreach ((int position, string value) in fields)
        {
            for (int i = 0; i < value.Length; i++)
            {
                chars[position - 1 + i] = value[i];
            }
        }

        return chars.ToString();
    }

    internal static (int, string)[] Header(string code, string scale = "2", string account = "12345678901", string currency = "EUR", string bank = "30002", string branch = "00550")
    {
        return
        [
            (1, code), (3, bank), (12, branch), (17, currency), (20, scale), (22, account),
        ];
    }

    internal static string Balance(string code, string date, string amount, string scale = "2", string account = "12345678901")
    {
        return Build([.. Header(code, scale, account), (35, date), (91, amount)]);
    }

    internal static string Movement(
        string date,
        string valueDate,
        string label,
        string amount,
        string interbank = "05",
        string internalCode = "1234",
        string entry = "0000042",
        string reference = "",
        string scale = "2",
        string account = "12345678901")
    {
        return Build(
        [
            .. Header("04", scale, account),
            (8, internalCode), (33, interbank), (35, date), (43, valueDate), (49, label),
            (82, entry), (89, "0"), (90, "1"), (91, amount), (105, reference),
        ]);
    }

    internal static string Complement(string qualifier, string text, string date = "150119", string account = "12345678901")
    {
        return Build([.. Header("05", "2", account), (33, "05"), (35, date), (46, qualifier), (49, text)]);
    }

    internal static string Join(params string[] records)
    {
        return string.Join('\n', records);
    }
}
