# Cfonb.Net

[![ci](https://github.com/IsraelIyonsi/Cfonb.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/IsraelIyonsi/Cfonb.NET/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Cfonb.Net.svg)](https://www.nuget.org/packages/Cfonb.Net)

A zero-dependency, AOT-safe parser for the French **CFONB 120** bank statement format (*releve de compte sur support informatique*), the fixed-width, 120-character-per-line file that French banks deliver over EBICS, ETEBAC and web-EDI channels.

## Why

CFONB 120 is the standard way a French bank hands a company its account statements, and accounting and reconciliation software has to read it. Parsers exist for Ruby, PHP and Elixir, but nothing existed for .NET. Cfonb.Net turns a file into typed statements, with amounts as `decimal` and dates as `DateOnly`.

## Install

```
dotnet add package Cfonb.Net
```

Targets .NET 8. No runtime dependencies, no reflection.

## Usage

```csharp
using Cfonb;

IReadOnlyList<CfonbStatement> statements = CfonbParser.Parse(File.ReadAllText("releve.txt"));

foreach (CfonbStatement s in statements)
{
    Console.WriteLine($"{s.Account.BankCode} {s.Account.AccountNumber} {s.Account.Currency}");
    Console.WriteLine($"  opening {s.OpeningBalance.Date:yyyy-MM-dd} {s.OpeningBalance.Amount}");

    foreach (CfonbTransaction t in s.Transactions)
    {
        Console.WriteLine($"  {t.OperationDate:yyyy-MM-dd} {t.InterbankOperationCode} {t.Amount,12} {t.FullLabel}");
    }

    Console.WriteLine($"  closing {s.ClosingBalance.Date:yyyy-MM-dd} {s.ClosingBalance.Amount}");
}
```

Other entry points:

```csharp
CfonbParser.Parse(TextReader reader);
bool ok = CfonbParser.TryParse(text, out var statements);
bool ok2 = CfonbParser.TryParse(text, out var statements2, out CfonbParseException? error);
```

Malformed input throws `CfonbParseException` (or fails `TryParse`) with the 1-based `LineNumber` and a message such as `Line 3: Record is too short: 119 characters, expected 120.`

Amounts are signed: credits are positive, debits are negative. The scale of each `decimal` follows the record's "nombre de decimales" field, so `0000000001904}` with 2 decimals is `-190.40m`.

## Supported records

| Code | Meaning | Parsed into |
|------|---------|-------------|
| `01` | ancien solde (opening balance) | `CfonbStatement.OpeningBalance` |
| `04` | mouvement (transaction) | `CfonbTransaction` |
| `05` | complement | `CfonbTransaction.Complements` (qualifier + text) |
| `07` | nouveau solde (closing balance) | `CfonbStatement.ClosingBalance` |

A file may hold several statements. A statement with no movements (only `01` and `07`) is valid.

`CfonbTransaction.Label` is the 31-character libelle of the `04` record; `FullLabel` appends the text of every `LIB` complement, separated by single spaces.

### Amount encoding

The last character of the 14-character amount carries the units digit and the sign (ASCII files):

| Last char | `{` | `A`-`I` | `}` | `J`-`R` |
|-----------|-----|---------|-----|---------|
| Meaning | +0 | +1 to +9 | -0 | -1 to -9 |

### Dates

`JJMMAA` (day, month, two-digit year). The brochure does not state a century rule; like the Ruby and Elixir parsers, this library treats years above 60 as 19xx and the rest as 20xx.

## Scope and limitations

- CFONB 120 account statements only. CFONB 240 (transfers) is not supported.
- Text input in ASCII (or any encoding .NET has already decoded to a string). Raw EBCDIC bytes are not decoded.
- Records must be exactly 120 characters, one per line. Blank lines are skipped. Files without line breaks are rejected.
- `05` complements are returned as raw qualifier and text. Only `LIB` is interpreted (for `FullLabel`); structured decoding of `MMO` (original currency and amount) and the bank-specific qualifiers is not done.
- Reserved ("zone reservee") fields are ignored, not required to be blank.
- Within a statement, every record must carry the same account number and currency as the `01` record; bank and branch codes are not cross-checked.
- Interbank operation codes are returned as strings; the code list is not interpreted.

## Specification sources

Field positions were taken from the CFONB brochure *Releve de compte sur support informatique* (July 2004), as published at
<https://cxrp.newrest.eu/xrppro/hlp/qualiac/fr/oct/annexes/TIFB-releves_de_comptes-CFONB120-v2004.07.pdf> (record tables 1, 2, 2bis, 3). The amount sign encoding is from the same brochure's "description detaillee" (fields 1-L, 2-R, 3-L), reproduced in Credit Agricole's EDI guide at <https://www.ca-aquitaine.fr/Vitrine/ObjCommun/Fic/Aquitaine/edi/parametrage_releves_edi.pdf> and in the CFONB brochure excerpt at <https://www.ca-centreloire.fr/Vitrine/ObjCommun/Fic/CentreLoire/Entreprise/GuideParamRel_EDI.pdf>. The interbank operation code table and the `LIB`/`MMO` qualifiers are in the same documents.

The test suite uses a real sample file from [pennylane-hq/cfonb](https://github.com/pennylane-hq/cfonb) (MIT) with its independently written expected values, plus records built position by position from the brochure tables.

## License

MIT, copyright 2026 Israel Iyonsi.
