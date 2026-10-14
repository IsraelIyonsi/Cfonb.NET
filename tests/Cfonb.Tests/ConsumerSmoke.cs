using Cfonb;

namespace ExternalConsumer.SmokeTests;

public class ConsumerSmoke
{
    [Fact]
    public void Entry_type_is_reachable_from_a_file_scoped_namespace_consumer()
    {
        string record01 = "0130002    00550EUR2 12345678901  311218".PadRight(90) + "0000000010000{".PadRight(30);
        string record07 = "0730002    00550EUR2 12345678901  310119".PadRight(90) + "0000000010000{".PadRight(30);

        IReadOnlyList<CfonbStatement> statements = CfonbParser.Parse(record01 + "\n" + record07);

        Assert.Equal(1000.00m, Assert.Single(statements).OpeningBalance.Amount);
    }
}
