using System.Globalization;

namespace Cfonb.Tests;

public class AmountDecodingTests
{
    private const string Prefix = "000000000012";

    // CFONB July 2004, field 3-L / 2-R, ASCII files: 0x7B '{' = +0, 0x41-0x49 'A'-'I' = +1..+9,
    // 0x7D '}' = -0, 0x4A-0x52 'J'-'R' = -1..-9. Prefix "0000000000120" gives 120 hundredths plus the unit digit.
    [Theory]
    [InlineData('{', "12.00")]
    [InlineData('A', "12.01")]
    [InlineData('B', "12.02")]
    [InlineData('C', "12.03")]
    [InlineData('D', "12.04")]
    [InlineData('E', "12.05")]
    [InlineData('F', "12.06")]
    [InlineData('G', "12.07")]
    [InlineData('H', "12.08")]
    [InlineData('I', "12.09")]
    [InlineData('}', "-12.00")]
    [InlineData('J', "-12.01")]
    [InlineData('K', "-12.02")]
    [InlineData('L', "-12.03")]
    [InlineData('M', "-12.04")]
    [InlineData('N', "-12.05")]
    [InlineData('O', "-12.06")]
    [InlineData('P', "-12.07")]
    [InlineData('Q', "-12.08")]
    [InlineData('R', "-12.09")]
    public void Specifier_sets_sign_and_units_digit(char specifier, string expected)
    {
        decimal amount = ParseClosing(Prefix + "0" + specifier, "2");

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), amount);
        Assert.Equal(expected, amount.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("0", "0000000001904}", "-19040")]
    [InlineData("1", "0000000001904}", "-1904.0")]
    [InlineData("2", "0000000001904}", "-190.40")]
    [InlineData("3", "0000000001904}", "-19.040")]
    [InlineData("2", "0000000002412J", "-241.21")]
    [InlineData("2", "0000000000974I", "97.49")]
    [InlineData("3", "0000000012345{", "123.450")]
    [InlineData("0", "9999999999999I", "99999999999999")]
    public void Number_of_decimals_sets_scale(string scale, string field, string expected)
    {
        decimal amount = ParseClosing(field, scale);

        Assert.Equal(expected, amount.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("0000000000000{", "0.00")]
    [InlineData("0000000000000}", "0.00")]
    public void Zero_has_no_sign(string field, string expected)
    {
        Assert.Equal(expected, ParseClosing(field, "2").ToString(CultureInfo.InvariantCulture));
    }

    private static decimal ParseClosing(string amountField, string scale)
    {
        string text = RecordBuilder.Join(
            RecordBuilder.Balance("01", "010119", "0000000000000{"),
            RecordBuilder.Balance("07", "310119", amountField, scale));
        return CfonbParser.Parse(text)[0].ClosingBalance.Amount;
    }
}
