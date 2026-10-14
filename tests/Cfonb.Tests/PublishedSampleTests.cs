using System.Globalization;

namespace Cfonb.Tests;

/// <summary>
/// Fixture: spec/files/example.txt from https://github.com/pennylane-hq/cfonb (MIT, Copyright 2023 Johan Le Bray
/// and Frantisek Rokusek). Expected values are the ones asserted in that project's spec/cfonb/parser_spec.rb,
/// which were written independently of this parser.
/// </summary>
public class PublishedSampleTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pennylane-example.cfonb");

    private static IReadOnlyList<CfonbStatement> Load()
    {
        return CfonbParser.Parse(File.ReadAllText(FixturePath));
    }

    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static DateOnly Day(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    [Fact]
    public void File_contains_two_statements()
    {
        Assert.Equal(2, Load().Count);
    }

    [Theory]
    [InlineData(0, "15589", "00000", "EUR", "98765432100", "2019-05-15", "-190.40", "2019-05-16", "-241.21", 3)]
    [InlineData(1, "18706", "00000", "EUR", "00123456789", "2019-05-16", "-241.21", "2019-05-17", "-163.72", 3)]
    public void Statement_header_and_balances(int index, string bank, string branch, string currency, string account, string openDate, string open, string closeDate, string close, int count)
    {
        CfonbStatement statement = Load()[index];

        Assert.Equal(new CfonbAccount(bank, branch, currency, account), statement.Account);
        Assert.Equal(Day(openDate), statement.OpeningBalance.Date);
        Assert.Equal(D(open), statement.OpeningBalance.Amount);
        Assert.Equal(Day(closeDate), statement.ClosingBalance.Date);
        Assert.Equal(D(close), statement.ClosingBalance.Amount);
        Assert.Equal(count, statement.Transactions.Count);
    }

    [Theory]
    [InlineData(0, 0, "9162", "B1", "2019-05-16", "2019-05-16", "PRLV SEPA TEST CABINET", "-32.21", "0", null)]
    [InlineData(0, 1, "9162", "B1", "2019-05-16", "2019-05-16", "VIR  SEPA DEMONSTRATION", "-10.70", "0", "REFERENCE")]
    [InlineData(0, 2, "0117", "62", "2019-05-15", "2019-05-15", "F COMMISSION D INTERVENTION", "-7.90", "1", null)]
    [InlineData(1, 0, "0158", "A3", "2019-05-17", "2019-05-15", "PRLV SEPA GROUPAMA CEN", "97.49", null, null)]
    [InlineData(1, 1, "0337", "62", "2019-05-15", "2019-05-15", "F FRAIS PRLV IMP 97 49EUR", "-12.10", "1", null)]
    [InlineData(1, 2, "0117", "62", "2019-05-16", "2019-05-16", "F COMMISSION D INTERVENTION", "-7.90", "1", null)]
    public void Transaction_fields(int statementIndex, int index, string internalCode, string interbank, string date, string valueDate, string label, string amount, string? exemption, string? reference)
    {
        CfonbTransaction tx = Load()[statementIndex].Transactions[index];

        Assert.Equal(internalCode, tx.InternalOperationCode);
        Assert.Equal(interbank, tx.InterbankOperationCode);
        Assert.Equal(Day(date), tx.OperationDate);
        Assert.Equal(Day(valueDate), tx.ValueDate);
        Assert.Equal(label, tx.Label);
        Assert.Equal(D(amount), tx.Amount);
        Assert.Equal(exemption, tx.CommissionExemptionIndicator);
        Assert.Equal(reference, tx.Reference);
        Assert.Equal("0", tx.UnavailabilityIndicator);
        Assert.Null(tx.RejectionCode);
        Assert.Equal(0, tx.EntryNumber);
    }

    [Fact]
    public void Complements_are_attached_in_order_to_their_movement()
    {
        IReadOnlyList<CfonbTransaction> txs = Load()[0].Transactions;

        Assert.Equal(12, txs[0].Complements.Count);
        Assert.Equal(new CfonbComplement("LIB", "MENSUEAUHTR13133"), txs[0].Complements[0]);
        Assert.Equal(new CfonbComplement("LIB", "MENSUEAUHTR13DUP"), txs[0].Complements[1]);
        Assert.Equal(new CfonbComplement("REF", "REFERENCE"), txs[0].Complements[2]);
        Assert.Equal(new CfonbComplement("NPY", "INTERNET SFR"), txs[0].Complements[4]);
        Assert.Single(txs[1].Complements);
        Assert.Equal(new CfonbComplement("NPY", "ELEC ERDF"), txs[1].Complements[0]);
        Assert.Empty(txs[2].Complements);
    }

    [Fact]
    public void Full_label_appends_lib_complements()
    {
        IReadOnlyList<CfonbTransaction> txs = Load()[0].Transactions;

        Assert.Equal("PRLV SEPA TEST CABINET MENSUEAUHTR13133 MENSUEAUHTR13DUP", txs[0].FullLabel);
        Assert.Equal("VIR  SEPA DEMONSTRATION", txs[1].FullLabel);
        Assert.Equal("PRLV SEPA GROUPAMA CEN P051928612   22793301700040", Load()[1].Transactions[0].FullLabel);
    }

    [Fact]
    public void Closing_balance_of_first_statement_is_opening_of_second()
    {
        IReadOnlyList<CfonbStatement> all = Load();

        Assert.Equal(all[0].ClosingBalance.Amount, all[1].OpeningBalance.Amount);
    }
}
