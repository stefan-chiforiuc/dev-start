using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace {{Name}}.ArchitectureTests;

public class LayeringTests
{
    private const string Domain = "{{Name}}.Domain";
    private const string Application = "{{Name}}.Application";
    private const string Infrastructure = "{{Name}}.Infrastructure";
    private const string Api = "{{Name}}.Api";

    [Fact]
    public void Domain_should_not_reference_anything_else()
    {
        var result = Types.InAssembly(typeof({{Name}}.Domain.IDomainEvent).Assembly)
            .Should()
            .NotHaveDependencyOnAny(Application, Infrastructure, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"domain must stay pure; violators: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_should_not_reference_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(typeof({{Name}}.Application.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOnAny(Infrastructure, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"application must not depend on infrastructure or web; violators: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Api_should_not_reference_persistence_types_directly()
    {
        var result = Types.InAssembly(typeof(Program).Assembly)
            .That()
            .ResideInNamespaceStartingWith(Api)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: $"API layer must not reach through Application into EF; violators: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
