using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class TokensTests
{
    [Theory]
    [InlineData("my-app", "MyApp", "my-app", "myapp")]
    [InlineData("orders", "Orders", "orders", "orders")]
    [InlineData("a-b-c", "ABC", "a-b-c", "abc")]
    public void Derives_casings_from_kebab(string raw, string pascal, string kebab, string lower)
    {
        var t = new Tokens(raw);
        t.Name.Should().Be(pascal);
        t.KebabName.Should().Be(kebab);
        t.LowerName.Should().Be(lower);
    }

    [Fact]
    public void Applies_tokens_in_content()
    {
        var t = new Tokens("my-app");
        var s = t.Apply("namespace {{Name}}.Api; // {{name}} at {{namelower}}");
        s.Should().Be("namespace MyApp.Api; // my-app at myapp");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1leading-digit")]
    [InlineData("-leading-dash")]
    [InlineData("toolongxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    public void Rejects_invalid_names(string raw)
    {
        var act = () => new Tokens(raw);
        act.Should().Throw<ArgumentException>();
    }
}
