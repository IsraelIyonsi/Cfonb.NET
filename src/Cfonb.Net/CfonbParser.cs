using System.Diagnostics.CodeAnalysis;

namespace Cfonb;

/// <summary>Parses CFONB 120 bank statement files (releve de compte sur support informatique).</summary>
public static class CfonbParser
{
    /// <summary>Parses a CFONB 120 file held in a string.</summary>
    /// <param name="text">The file content; records are separated by line breaks.</param>
    /// <returns>The statements in file order.</returns>
    /// <exception cref="CfonbParseException">The input is malformed.</exception>
    public static IReadOnlyList<CfonbStatement> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        using var reader = new StringReader(text);
        return Parse(reader);
    }

    /// <summary>Parses a CFONB 120 file from a reader.</summary>
    /// <param name="reader">The reader positioned at the first record.</param>
    /// <returns>The statements in file order.</returns>
    /// <exception cref="CfonbParseException">The input is malformed.</exception>
    public static IReadOnlyList<CfonbStatement> Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var session = new ParseSession();
        int lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (line.AsSpan().Trim().IsEmpty)
            {
                continue;
            }

            session.Accept(line, lineNumber);
        }

        return session.Finish();
    }

    /// <summary>Attempts to parse a CFONB 120 file held in a string.</summary>
    /// <param name="text">The file content.</param>
    /// <param name="statements">The parsed statements when the method returns true; otherwise null.</param>
    /// <returns>True when the input was parsed; otherwise false.</returns>
    public static bool TryParse(string? text, [NotNullWhen(true)] out IReadOnlyList<CfonbStatement>? statements)
    {
        return TryParse(text, out statements, out _);
    }

    /// <summary>Attempts to parse a CFONB 120 file held in a string and reports why it failed.</summary>
    /// <param name="text">The file content.</param>
    /// <param name="statements">The parsed statements when the method returns true; otherwise null.</param>
    /// <param name="error">The parse failure when the method returns false; otherwise null.</param>
    /// <returns>True when the input was parsed; otherwise false.</returns>
    public static bool TryParse(
        string? text,
        [NotNullWhen(true)] out IReadOnlyList<CfonbStatement>? statements,
        [NotNullWhen(false)] out CfonbParseException? error)
    {
        statements = null;
        error = null;
        if (text is null)
        {
            error = new CfonbParseException("Input is null.", 0);
            return false;
        }

        try
        {
            statements = Parse(text);
            return true;
        }
        catch (CfonbParseException ex)
        {
            error = ex;
            return false;
        }
    }

    private sealed class ParseSession
    {
        private readonly List<CfonbStatement> _statements = [];
        private readonly List<CfonbTransaction> _transactions = [];
        private readonly List<CfonbComplement> _complements = [];
        private CfonbAccount? _account;
        private CfonbBalance? _opening;
        private TransactionHead? _pending;

        internal void Accept(string line, int lineNumber)
        {
            if (line.Length != CfonbLayout.RecordLength)
            {
                string kind = line.Length < CfonbLayout.RecordLength ? "too short" : "too long";
                throw Fail(lineNumber, $"Record is {kind}: {line.Length} characters, expected {CfonbLayout.RecordLength}.");
            }

            string code = line.Substring(CfonbLayout.CodeOffset, CfonbLayout.CodeLength);
            switch (code)
            {
                case CfonbLayout.OpeningBalanceCode:
                    AcceptOpening(line, lineNumber);
                    break;
                case CfonbLayout.TransactionCode:
                    AcceptTransaction(line, lineNumber);
                    break;
                case CfonbLayout.ComplementCode:
                    AcceptComplement(line, lineNumber);
                    break;
                case CfonbLayout.ClosingBalanceCode:
                    AcceptClosing(line, lineNumber);
                    break;
                default:
                    throw Fail(lineNumber, $"Unknown record type '{code}'.");
            }
        }

        internal IReadOnlyList<CfonbStatement> Finish()
        {
            if (_account is not null)
            {
                throw Fail(0, $"Statement is missing its closing balance record ({CfonbLayout.ClosingBalanceCode}).");
            }

            return _statements;
        }

        private static CfonbParseException Fail(int lineNumber, string message)
        {
            string prefix = lineNumber > 0 ? $"Line {lineNumber}: " : string.Empty;
            return new CfonbParseException(prefix + message, lineNumber);
        }

        private static string Slice(string line, int offset, int length)
        {
            return line.Substring(offset, length).Trim();
        }

        private static string? SliceOrNull(string line, int offset, int length)
        {
            string value = Slice(line, offset, length);
            return value.Length == 0 ? null : value;
        }

        private static CfonbAccount ReadAccount(string line, int lineNumber)
        {
            string bank = Slice(line, CfonbLayout.BankOffset, CfonbLayout.BankLength);
            string branch = Slice(line, CfonbLayout.BranchOffset, CfonbLayout.BranchLength);
            string currency = Slice(line, CfonbLayout.CurrencyOffset, CfonbLayout.CurrencyLength);
            string account = Slice(line, CfonbLayout.AccountOffset, CfonbLayout.AccountLength);
            if (bank.Length == 0 || currency.Length == 0 || account.Length == 0)
            {
                throw Fail(lineNumber, "Bank code, currency and account number are mandatory.");
            }

            return new CfonbAccount(bank, branch, currency, account);
        }

        private static int ReadScale(string line, int lineNumber)
        {
            char c = line[CfonbLayout.ScaleOffset];
            if (c < '0' || c > '9')
            {
                throw Fail(lineNumber, $"Bad number of decimals '{c}'.");
            }

            return c - '0';
        }

        private static decimal ReadAmount(string line, int lineNumber)
        {
            int scale = ReadScale(line, lineNumber);
            ReadOnlySpan<char> field = line.AsSpan(CfonbLayout.AmountOffset, CfonbLayout.AmountLength);
            if (!CfonbAmount.TryDecode(field, scale, out decimal amount))
            {
                throw Fail(lineNumber, $"Bad amount '{field}'.");
            }

            return amount;
        }

        private static DateOnly? ReadDate(string line, int offset, int lineNumber, bool required)
        {
            ReadOnlySpan<char> field = line.AsSpan(offset, CfonbLayout.DateLength);
            if (field.Trim().IsEmpty)
            {
                return required ? throw Fail(lineNumber, "Mandatory date is blank.") : null;
            }

            if (!TryReadDigits(field.Slice(CfonbLayout.DayOffset, CfonbLayout.DatePartLength), out int day)
                || !TryReadDigits(field.Slice(CfonbLayout.MonthOffset, CfonbLayout.DatePartLength), out int month)
                || !TryReadDigits(field.Slice(CfonbLayout.YearOffset, CfonbLayout.DatePartLength), out int shortYear))
            {
                throw Fail(lineNumber, $"Bad date '{field}'.");
            }

            int year = (shortYear > CfonbLayout.YearPivot ? CfonbLayout.TwentiethCenturyBase : CfonbLayout.TwentyFirstCenturyBase) + shortYear;
            if (month < CfonbLayout.FirstMonth || month > CfonbLayout.LastMonth || day < CfonbLayout.FirstDay || day > DateTime.DaysInMonth(year, month))
            {
                throw Fail(lineNumber, $"Bad date '{field}'.");
            }

            return new DateOnly(year, month, day);
        }

        private static bool TryReadDigits(ReadOnlySpan<char> digits, out int value)
        {
            value = 0;
            foreach (char c in digits)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }

                value = (value * CfonbLayout.DecimalBase) + (c - '0');
            }

            return true;
        }

        private static long ReadEntryNumber(string line, int lineNumber)
        {
            string field = Slice(line, CfonbLayout.EntryNumberOffset, CfonbLayout.EntryNumberLength);
            if (field.Length == 0)
            {
                return 0;
            }

            if (!TryReadDigits(field, out int value))
            {
                throw Fail(lineNumber, $"Bad entry number '{field}'.");
            }

            return value;
        }

        private void RequireSameAccount(string line, int lineNumber)
        {
            if (_account is null)
            {
                throw Fail(lineNumber, $"Record has no preceding opening balance record ({CfonbLayout.OpeningBalanceCode}).");
            }

            CfonbAccount current = ReadAccount(line, lineNumber);
            if (current.AccountNumber != _account.AccountNumber || current.Currency != _account.Currency)
            {
                throw Fail(lineNumber, "Account number or currency differs from the opening balance record.");
            }
        }

        private void AcceptOpening(string line, int lineNumber)
        {
            if (_account is not null)
            {
                throw Fail(lineNumber, $"Opening balance record while the previous statement has no closing balance record ({CfonbLayout.ClosingBalanceCode}).");
            }

            CfonbAccount account = ReadAccount(line, lineNumber);
            DateOnly date = ReadDate(line, CfonbLayout.DateOffset, lineNumber, required: true)!.Value;
            _opening = new CfonbBalance(date, ReadAmount(line, lineNumber));
            _account = account;
        }

        private void AcceptTransaction(string line, int lineNumber)
        {
            RequireSameAccount(line, lineNumber);
            FlushPending();
            _pending = new TransactionHead(
                SliceOrNull(line, CfonbLayout.InternalOperationOffset, CfonbLayout.InternalOperationLength),
                Slice(line, CfonbLayout.InterbankOperationOffset, CfonbLayout.InterbankOperationLength),
                ReadDate(line, CfonbLayout.DateOffset, lineNumber, required: true)!.Value,
                SliceOrNull(line, CfonbLayout.RejectionOffset, CfonbLayout.RejectionLength),
                ReadDate(line, CfonbLayout.ValueDateOffset, lineNumber, required: false),
                Slice(line, CfonbLayout.LabelOffset, CfonbLayout.LabelLength),
                ReadEntryNumber(line, lineNumber),
                SliceOrNull(line, CfonbLayout.ExemptionOffset, CfonbLayout.FlagLength),
                SliceOrNull(line, CfonbLayout.UnavailabilityOffset, CfonbLayout.FlagLength),
                ReadAmount(line, lineNumber),
                SliceOrNull(line, CfonbLayout.ReferenceOffset, CfonbLayout.ReferenceLength));
        }

        private void AcceptComplement(string line, int lineNumber)
        {
            RequireSameAccount(line, lineNumber);
            if (_pending is null)
            {
                throw Fail(lineNumber, $"Complement record ({CfonbLayout.ComplementCode}) without a preceding movement record ({CfonbLayout.TransactionCode}).");
            }

            _complements.Add(new CfonbComplement(
                Slice(line, CfonbLayout.QualifierOffset, CfonbLayout.QualifierLength),
                Slice(line, CfonbLayout.ComplementTextOffset, CfonbLayout.ComplementTextLength)));
        }

        private void AcceptClosing(string line, int lineNumber)
        {
            RequireSameAccount(line, lineNumber);
            FlushPending();
            DateOnly date = ReadDate(line, CfonbLayout.DateOffset, lineNumber, required: true)!.Value;
            var closing = new CfonbBalance(date, ReadAmount(line, lineNumber));
            _statements.Add(new CfonbStatement(_account!, _opening!, _transactions.ToArray(), closing));
            _transactions.Clear();
            _account = null;
            _opening = null;
        }

        private void FlushPending()
        {
            if (_pending is null)
            {
                return;
            }

            _transactions.Add(_pending.ToTransaction(_complements.ToArray()));
            _complements.Clear();
            _pending = null;
        }
    }

    private sealed record TransactionHead(
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
        string? Reference)
    {
        internal CfonbTransaction ToTransaction(IReadOnlyList<CfonbComplement> complements)
        {
            return new CfonbTransaction(
                InternalOperationCode,
                InterbankOperationCode,
                OperationDate,
                RejectionCode,
                ValueDate,
                Label,
                EntryNumber,
                CommissionExemptionIndicator,
                UnavailabilityIndicator,
                Amount,
                Reference,
                complements);
        }
    }
}
