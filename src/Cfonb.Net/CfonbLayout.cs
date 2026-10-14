namespace Cfonb;

internal static class CfonbLayout
{
    internal const int RecordLength = 120;

    internal const string OpeningBalanceCode = "01";
    internal const string TransactionCode = "04";
    internal const string ComplementCode = "05";
    internal const string ClosingBalanceCode = "07";

    internal const int CodeOffset = 0;
    internal const int CodeLength = 2;
    internal const int BankOffset = 2;
    internal const int BankLength = 5;
    internal const int InternalOperationOffset = 7;
    internal const int InternalOperationLength = 4;
    internal const int BranchOffset = 11;
    internal const int BranchLength = 5;
    internal const int CurrencyOffset = 16;
    internal const int CurrencyLength = 3;
    internal const int ScaleOffset = 19;
    internal const int AccountOffset = 21;
    internal const int AccountLength = 11;
    internal const int InterbankOperationOffset = 32;
    internal const int InterbankOperationLength = 2;
    internal const int DateOffset = 34;
    internal const int DateLength = 6;
    internal const int RejectionOffset = 40;
    internal const int RejectionLength = 2;
    internal const int ValueDateOffset = 42;
    internal const int LabelOffset = 48;
    internal const int LabelLength = 32;
    internal const int EntryNumberOffset = 81;
    internal const int EntryNumberLength = 7;
    internal const int ExemptionOffset = 88;
    internal const int UnavailabilityOffset = 89;
    internal const int AmountOffset = 90;
    internal const int AmountLength = 14;
    internal const int ReferenceOffset = 104;
    internal const int ReferenceLength = 16;
    internal const int QualifierOffset = 45;
    internal const int QualifierLength = 3;
    internal const int ComplementTextOffset = 48;
    internal const int ComplementTextLength = 70;

    internal const string LabelQualifier = "LIB";
    internal const char LabelSeparator = ' ';

    internal const int FlagLength = 1;
    internal const int DecimalBase = 10;
    internal const int FirstMonth = 1;
    internal const int LastMonth = 12;
    internal const int FirstDay = 1;
    internal const int MinScale = 0;
    internal const int MaxScale = 9;
    internal const int DatePartLength = 2;
    internal const int DayOffset = 0;
    internal const int MonthOffset = 2;
    internal const int YearOffset = 4;
    internal const int YearPivot = 60;
    internal const int TwentiethCenturyBase = 1900;
    internal const int TwentyFirstCenturyBase = 2000;
}
