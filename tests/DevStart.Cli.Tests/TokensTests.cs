using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

public class TokensTests
{
    [Theory]
    // Plain kebab / single segment — folder matches input, Pascal is canonical.
    [InlineData("my-app", "MyApp", "my-app", "myapp", "my-app")]
    [InlineData("orders", "Orders", "orders", "orders", "orders")]
    [InlineData("a-b-c", "ABC", "a-b-c", "abc", "a-b-c")]
    // PascalCase input is preserved verbatim in the folder.
    [InlineData("MyApp", "MyApp", "myapp", "myapp", "MyApp")]
    // Underscores and spaces normalize to dashes.
    [InlineData("my_app", "MyApp", "my-app", "myapp", "my-app")]
    [InlineData("my app", "MyApp", "my-app", "myapp", "my-app")]
    public void Derives_casings_for_kebab_and_pascal_inputs(
        string raw, string pascal, string kebab, string lower, string folder)
    {
        var t = new Tokens(raw);
        t.Name.Should().Be(pascal);
        t.KebabName.Should().Be(kebab);
        t.LowerName.Should().Be(lower);
        t.FolderName.Should().Be(folder);
    }

    [Theory]
    // Regression for bug: `My.Cool.App` previously became `MyCoolApp` in the
    // .sln/.csproj/namespace, losing the dotted .NET namespace convention.
    [InlineData("My.Cool.App", "My.Cool.App", "my-cool-app", "mycoolapp", "My.Cool.App")]
    [InlineData("my.app", "My.App", "my-app", "myapp", "my.app")]
    [InlineData("foo.bar.baz", "Foo.Bar.Baz", "foo-bar-baz", "foobarbaz", "foo.bar.baz")]
    public void Preserves_dotted_dotnet_namespaces(
        string raw, string pascal, string kebab, string lower, string folder)
    {
        var t = new Tokens(raw);
        t.Name.Should().Be(pascal);
        t.DotName.Should().Be(pascal);
        t.KebabName.Should().Be(kebab);
        t.LowerName.Should().Be(lower);
        t.FolderName.Should().Be(folder);
    }

    [Fact]
    public void Applies_tokens_in_content()
    {
        var t = new Tokens("my-app");
        var s = t.Apply("namespace {{Name}}.Api; // {{name}} at {{namelower}}");
        s.Should().Be("namespace MyApp.Api; // my-app at myapp");
    }

    [Fact]
    public void Applies_dotted_name_in_content()
    {
        var t = new Tokens("My.Cool.App");
        t.Apply("project {{Name}}.Api ref {{DotName}}").Should()
            .Be("project My.Cool.App.Api ref My.Cool.App");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1leading-digit")]
    [InlineData("-leading-dash")]
    [InlineData(".leading-dot")]
    [InlineData("trailing-dot.")]
    [InlineData("double..dot")]
    [InlineData("toolongxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    public void Rejects_invalid_names(string raw)
    {
        var act = () => new Tokens(raw);
        act.Should().Throw<DevStartUserException>();
    }
}
