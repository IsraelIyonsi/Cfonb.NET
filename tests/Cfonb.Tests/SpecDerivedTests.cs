namespace Cfonb.Tests;

/// <summary>
/// Records constructed strictly from the field positions in the CFONB brochure
/// "Releve de compte sur support informatique" (July 2004); expected values are hand-written.
/// </summary>
public class SpecDerivedTests
{
    private static readonly string Statement = RecordBuilder.Join(
        RecordBuilder.Balance("01", "311218", "0000000010000{"),
        RecordBuilder.Movement("020119", "030119", "VIREMENT RECU DUPONT", "0000000005000{", reference: "REF0000000000001"),
        RecordBuilder.Complement("LIB", "FACTURE 2018-0042"),
        RecordBuilder.Complement("MMO", "USD2000000000100{"),
        RecordBuilder.Movement("040119", "040119", "CARTE ACHAT", "0000000000199N", interbank: "11", entry: "0001234"),
        RecordBuilder.Balance("07", "310119", "0000000014800E"));

    [Fact]
    public void Parses_one_statement_with_account_identification()
    {
        CfonbStatement s = Assert.Single(CfonbParser.Parse(Statement));

        Assert.Equal(new CfonbAccount("30002", "00550", "EUR", "12345678901"), s.Account);
    }

    [Fact]
    public void Parses_balances()
    {
        CfonbStatement s = CfonbParser.Parse(Statement)[0];

        Assert.Equal(new CfonbBalance(new DateOnly(2018, 12, 31), 1000.00m), s.OpeningBalance);
        Assert.Equal(new CfonbBalance(new DateOnly(2019, 1, 31), 1480.05m), s.ClosingBalance);
    }

    [Fact]
    public void Parses_transactions_by_spec_positions()
    {
        IReadOnlyList<CfonbTransaction> txs = CfonbParser.Parse(Statement)[0].Transactions;

        Assert.Equal(2, txs.Count);
        Assert.Equal(
            new CfonbTransaction(
                "1234", "05", new DateOnly(2019, 1, 2), null, new DateOnly(2019, 1, 3), "VIREMENT RECU DUPONT", 42,
                "0", "1", 500.00m, "REF0000000000001", txs[0].Complements),
            txs[0]);
        Assert.Equal(-19.95m, txs[1].Amount);
        Assert.Equal("11", txs[1].InterbankOperationCode);
        Assert.Equal(1234, txs[1].EntryNumber);
        Assert.Null(txs[1].Reference);
    }

    [Fact]
    public void Attaches_complements_to_the_preceding_movement()
    {
        CfonbTransaction tx = CfonbParser.Parse(Statement)[0].Transactions[0];

        Assert.Equal(2, tx.Complements.Count);
        Assert.Equal(new CfonbComplement("LIB", "FACTURE 2018-0042"), tx.Complements[0]);
        Assert.Equal(new CfonbComplement("MMO", "USD2000000000100{"), tx.Complements[1]);
        Assert.Equal("VIREMENT RECU DUPONT FACTURE 2018-0042", tx.FullLabel);
    }

    [Fact]
    public void Statement_without_movements_has_only_balances()
    {
        string text = RecordBuilder.Join(
            RecordBuilder.Balance("01", "311218", "0000000010000{"),
            RecordBuilder.Balance("07", "310119", "0000000010000{"));

        CfonbStatement s = Assert.Single(CfonbParser.Parse(text));

        Assert.Empty(s.Transactions);
    }

    [Fact]
    public void Parses_several_statements_in_one_file()
    {
        string text = RecordBuilder.Join(
            RecordBuilder.Balance("01", "311218", "0000000010000{", account: "AAAAAAAAAAA"),
            RecordBuilder.Balance("07", "310119", "0000000010000{", account: "AAAAAAAAAAA"),
            RecordBuilder.Balance("01", "311218", "0000000000050}", account: "BBBBBBBBBBB"),
            RecordBuilder.Balance("07", "310119", "0000000000070}", account: "BBBBBBBBBBB"));

        IReadOnlyList<CfonbStatement> all = CfonbParser.Parse(text);

        Assert.Equal(["AAAAAAAAAAA", "BBBBBBBBBBB"], all.Select(s => s.Account.AccountNumber));
        Assert.Equal(-7.00m, all[1].ClosingBalance.Amount);
    }

    [Theory]
    [InlineData("010160", 2060)]
    [InlineData("010161", 1961)]
    [InlineData("290200", 2000)]
    [InlineData("311299", 1999)]
    public void Two_digit_year_pivot_is_60(string date, int expectedYear)
    {
        string text = RecordBuilder.Join(
            RecordBuilder.Balance("01", date, "0000000000000{"),
            RecordBuilder.Balance("07", date, "0000000000000{"));

        Assert.Equal(expectedYear, CfonbParser.Parse(text)[0].OpeningBalance.Date.Year);
    }

    [Fact]
    public void Blank_value_date_is_null()
    {
        string text = RecordBuilder.Join(
            RecordBuilder.Balance("01", "311218", "0000000000000{"),
            RecordBuilder.Movement("020119", "      ", "X", "0000000000100{"),
            RecordBuilder.Balance("07", "310119", "0000000000100{"));

        Assert.Null(CfonbParser.Parse(text)[0].Transactions[0].ValueDate);
    }

    [Fact]
    public void Accepts_crlf_blank_lines_and_trailing_newline()
    {
        string text = Statement.Replace("\n", "\r\n") + "\r\n\r\n";

        Assert.Equal(2, CfonbParser.Parse(text)[0].Transactions.Count);
    }

    [Fact]
    public void Reader_overload_matches_string_overload()
    {
        using var reader = new StringReader(Statement);

        Assert.Equal(CfonbParser.Parse(Statement)[0].ClosingBalance, CfonbParser.Parse(reader)[0].ClosingBalance);
    }

    [Fact]
    public void Empty_input_yields_no_statements()
    {
        Assert.Empty(CfonbParser.Parse(string.Empty));
    }

    [Fact]
    public void TryParse_succeeds_on_valid_input()
    {
        Assert.True(CfonbParser.TryParse(Statement, out IReadOnlyList<CfonbStatement>? statements));
        Assert.Single(statements);
    }

    [Fact]
    public void TryParse_reports_error_and_line()
    {
        Assert.False(CfonbParser.TryParse("short", out IReadOnlyList<CfonbStatement>? statements, out CfonbParseException? error));
        Assert.Null(statements);
        Assert.Equal(1, error.LineNumber);
    }

    [Fact]
    public void TryParse_null_returns_false()
    {
        Assert.False(CfonbParser.TryParse(null, out _));
    }

    [Fact]
    public void Movement_label_keeps_all_32_characters_including_position_80()
    {
        // The 04 libelle occupies positions 49 to 80 (32 characters). A full-width label
        // whose final character sits at position 80 must not be truncated.
        const string label = "PRLV SEPA LONG MERCHANT NAME 204";
        Assert.Equal(32, label.Length);
        Assert.NotEqual(' ', label[^1]);

        string statement = RecordBuilder.Join(
            RecordBuilder.Balance("01", "010119", "0000000000000{"),
            RecordBuilder.Movement("020119", "020119", label, "0000000000100{"),
            RecordBuilder.Balance("07", "020119", "0000000000100{"));

        CfonbTransaction tx = CfonbParser.Parse(statement)[0].Transactions[0];

        Assert.Equal(label, tx.Label);
        Assert.Equal(label, tx.FullLabel);
    }
}
