using Aletheia.Core;

namespace Aletheia.Core.Tests;

public sealed class IsinTests
{
    [Fact]
    public void Constructor_WithLowercaseValue_NormalizesToUppercase()
    {
        var isin = new Isin("es0123456789");

        Assert.Equal("ES0123456789", isin.Value);
    }

    [Fact]
    public void IsValid_WithWrongLength_ReturnsFalse()
    {
        Assert.False(Isin.IsValid("ES123"));
    }
}
