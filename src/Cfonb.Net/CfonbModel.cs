namespace Cfonb;

/// <summary>Identifies the bank account a statement belongs to (fields 1-B, 1-D, 1-E and 1-H).</summary>
/// <param name="BankCode">The 5-character code banque (code etablissement).</param>
/// <param name="BranchCode">The 5-character code guichet.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="AccountNumber">The account number (up to 11 characters).</param>
public sealed record CfonbAccount(string BankCode, string BranchCode, string Currency, string AccountNumber);

/// <summary>A dated balance: the ancien solde (01) or nouveau solde (07).</summary>
/// <param name="Date">The balance date.</param>
/// <param name="Amount">The signed balance; credit is positive and debit is negative.</param>
public sealed record CfonbBalance(DateOnly Date, decimal Amount);

/// <summary>A complement (05) line attached to the preceding transaction.</summary>
/// <param name="Qualifier">The 3-character qualifier, for example LIB or MMO.</param>
/// <param name="Text">The trimmed complementary information (up to 70 characters).</param>
public sealed record CfonbComplement(string Qualifier, string Text);

/// <summary>A movement (04 record) with its complement (05) lines.</summary>
/// <param name="InternalOperationCode">The bank-specific operation code, or null when blank.</param>
/// <param name="InterbankOperationCode">The 2-character code operation interbancaire.</param>
/// <param name="OperationDate">The accounting date of the operation.</param>
/// <param name="RejectionCode">The rejection reason code, or null when blank.</param>
/// <param name="ValueDate">The value date, or null when blank.</param>
/// <param name="Label">The libelle from the 04 record (up to 32 characters), trimmed.</param>
/// <param name="EntryNumber">The numero d'ecriture (zero when blank).</param>
/// <param name="CommissionExemptionIndicator">The commission exemption flag, or null when blank.</param>
/// <param name="UnavailabilityIndicator">The unavailability flag, or null when blank.</param>
/// <param name="Amount">The signed amount; credit is positive and debit is negative.</param>
/// <param name="Reference">The reference zone, or null when blank.</param>
/// <param name="Complements">The 05 complement lines in file order.</param>
public sealed record CfonbTransaction(
    string? InternalOperationCode,
    string InterbankOperationCode,
    DateOnly OperationDate,
    string? RejectionCode,
    DateOnly? ValueDate,
    string Label,
    long EntryNumber,
    string? CommissionExemptionIndicator,
    string? UnavailabilityIndicator,
    decimal Amount,
    string? Reference,
    IReadOnlyList<CfonbComplement> Complements)
{
    /// <summary>Gets the 04 label followed by the text of every LIB complement, separated by single spaces.</summary>
    public string FullLabel
    {
        get
        {
            var parts = new List<string>();
            if (Label.Length > 0)
            {
                parts.Add(Label);
            }

            foreach (CfonbComplement complement in Complements)
            {
                if (complement.Qualifier == CfonbLayout.LabelQualifier && complement.Text.Length > 0)
                {
                    parts.Add(complement.Text);
                }
            }

            return string.Join(CfonbLayout.LabelSeparator, parts);
        }
    }
}

/// <summary>One account statement: an opening balance, its transactions and a closing balance.</summary>
/// <param name="Account">The account identification.</param>
/// <param name="OpeningBalance">The ancien solde (01).</param>
/// <param name="Transactions">The transactions in file order.</param>
/// <param name="ClosingBalance">The nouveau solde (07).</param>
public sealed record CfonbStatement(
    CfonbAccount Account,
    CfonbBalance OpeningBalance,
    IReadOnlyList<CfonbTransaction> Transactions,
    CfonbBalance ClosingBalance);
