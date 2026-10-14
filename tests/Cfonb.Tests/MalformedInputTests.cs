namespace Cfonb.Tests;

public class MalformedInputTests
{
    private static readonly string Open = RecordBuilder.Balance("01", "311218", "0000000100000{");
    private static readonly string Close = RecordBuilder.Balance("07", "310119", "0000000100000{");
    private static readonly string Move = RecordBuilder.Movement("020119", "030119", "X", "0000000000100{");

    public static TheoryData<string, int, string> Cases => new()
    {
        { Open[..119] + "\n" + Close, 1, "too short" },
        { Open + "\n" + Close + " ", 2, "too long" },
        { Open + "\n" + RecordBuilder.Build((1, "02")) + "\n" + Close, 2, "Unknown record type '02'" },
        { Open + "\n" + RecordBuilder.Build((1, "XY")) + "\n" + Close, 2, "Unknown record type 'XY'" },
        { RecordBuilder.Balance("01", "311318", "0000000100000{") + "\n" + Close, 1, "Bad date" },
        { RecordBuilder.Balance("01", "300218", "0000000100000{") + "\n" + Close, 1, "Bad date" },
        { RecordBuilder.Balance("01", "31AB18", "0000000100000{") + "\n" + Close, 1, "Bad date" },
        { RecordBuilder.Balance("01", "      ", "0000000100000{") + "\n" + Close, 1, "date is blank" },
        { Open + "\n" + RecordBuilder.Movement("000119", "030119", "X", "0000000000100{") + "\n" + Close, 2, "Bad date" },
        { Open + "\n" + RecordBuilder.Movement("020119", "0301XX", "X", "0000000000100{") + "\n" + Close, 2, "Bad date" },
        { RecordBuilder.Balance("01", "311218", "00000001000000") + "\n" + Close, 1, "Bad amount" },
        { RecordBuilder.Balance("01", "311218", "0000000100000S") + "\n" + Close, 1, "Bad amount" },
        { RecordBuilder.Balance("01", "311218", "0000 00100000{") + "\n" + Close, 1, "Bad amount" },
        { RecordBuilder.Balance("01", "311218", "0000000100000a") + "\n" + Close, 1, "Bad amount" },
        { RecordBuilder.Balance("01", "311218", "0000000100000{", scale: "X") + "\n" + Close, 1, "number of decimals" },
        { Open + "\n" + RecordBuilder.Movement("020119", "030119", "X", "0000000000100{", entry: "00AB042") + "\n" + Close, 2, "entry number" },
        { Move + "\n" + Close, 1, "no preceding opening" },
        { Close, 1, "no preceding opening" },
        { RecordBuilder.Complement("LIB", "X") + "\n" + Close, 1, "no preceding opening" },
        { Open + "\n" + RecordBuilder.Complement("LIB", "X") + "\n" + Close, 2, "without a preceding movement" },
        { Open + "\n" + Open + "\n" + Close, 2, "no closing balance" },
        { Open + "\n" + Move, 0, "missing its closing balance" },
        { Open + "\n" + Move + "\n" + Close + "\n" + Close, 4, "no preceding opening" },
        { Open + "\n" + RecordBuilder.Movement("020119", "030119", "X", "0000000000100{", account: "99999999999") + "\n" + Close, 2, "differs" },
        { Open + "\n" + Close.Replace("EUR", "USD"), 2, "differs" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Throws_with_clear_message(string text, int lineNumber, string messagePart)
    {
        var ex = Assert.Throws<CfonbParseException>(() => CfonbParser.Parse(text));

        Assert.Equal(lineNumber, ex.LineNumber);
        Assert.Contains(messagePart, ex.Message, StringComparison.Ordinal);
        if (lineNumber > 0)
        {
            Assert.StartsWith($"Line {lineNumber}:", ex.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Null_input_throws_argument_null()
    {
        Assert.Throws<ArgumentNullException>(() => CfonbParser.Parse((string)null!));
        Assert.Throws<ArgumentNullException>(() => CfonbParser.Parse((TextReader)null!));
    }

    [Fact]
    public void TryParse_returns_false_instead_of_throwing()
    {
        Assert.False(CfonbParser.TryParse(Open + "\n" + Move, out _));
    }
}
