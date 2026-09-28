namespace Content.IntegrationTests.Tests._NF;

public sealed class FrontierConstants
{
    public static readonly string[] GameMapPrototypes =
#if DEBUG
    {
        "Frontier"
    };
#else
    {
        "Frontier"
    };
#endif
}
