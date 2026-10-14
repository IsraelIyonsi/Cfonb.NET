namespace Cfonb;

/// <summary>Thrown when a CFONB 120 file is malformed.</summary>
public sealed class CfonbParseException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="CfonbParseException"/> class.</summary>
    /// <param name="message">The description of the problem.</param>
    /// <param name="lineNumber">The 1-based line number of the offending record, or 0 when not tied to a single line.</param>
    public CfonbParseException(string message, int lineNumber)
        : base(message)
    {
        LineNumber = lineNumber;
    }

    /// <summary>Gets the 1-based line number of the offending record, or 0 when the problem is not tied to a single line.</summary>
    public int LineNumber { get; }
}
