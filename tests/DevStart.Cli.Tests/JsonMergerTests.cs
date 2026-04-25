using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace DevStart.Tests;

public class JsonMergerTests
{
    [Fact]
    public void Merges_dependency_dicts()
    {
        var target = /* lang=json */ """{ "dependencies": { "fastify": "5.0.0" } }""";
        var fragment = /* lang=json */ """{ "dependencies": { "kysely": "0.27.4", "pg": "8.13.1" } }""";

        var merged = JsonMerger.Merge(target, fragment);
        var doc = JsonDocument.Parse(merged);
        var deps = doc.RootElement.GetProperty("dependencies");
        deps.GetProperty("fastify").GetString().Should().Be("5.0.0");
        deps.GetProperty("kysely").GetString().Should().Be("0.27.4");
        deps.GetProperty("pg").GetString().Should().Be("8.13.1");
    }

    [Fact]
    public void Fragment_wins_on_scalar_conflict()
    {
        var target = /* lang=json */ """{ "dependencies": { "fastify": "4.0.0" } }""";
        var fragment = /* lang=json */ """{ "dependencies": { "fastify": "5.0.0" } }""";

        var merged = JsonMerger.Merge(target, fragment);
        var doc = JsonDocument.Parse(merged);
        doc.RootElement.GetProperty("dependencies").GetProperty("fastify").GetString()
            .Should().Be("5.0.0");
    }

    [Fact]
    public void Arrays_are_concatenated_with_dedup()
    {
        var target = /* lang=json */ """{ "tags": ["a", "b"] }""";
        var fragment = /* lang=json */ """{ "tags": ["b", "c"] }""";

        var merged = JsonMerger.Merge(target, fragment);
        var doc = JsonDocument.Parse(merged);
        var tags = doc.RootElement.GetProperty("tags").EnumerateArray()
            .Select(e => e.GetString()).ToList();
        tags.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void Tolerates_jsonc_comments_and_trailing_commas_in_target()
    {
        var target = """
        {
            // a comment
            "compilerOptions": {
                "strict": true, /* trailing ok */
            },
        }
        """;
        var fragment = /* lang=json */ """{ "compilerOptions": { "target": "ES2022" } }""";

        var act = () => JsonMerger.Merge(target, fragment);
        act.Should().NotThrow();

        var doc = JsonDocument.Parse(act());
        var co = doc.RootElement.GetProperty("compilerOptions");
        co.GetProperty("strict").GetBoolean().Should().BeTrue();
        co.GetProperty("target").GetString().Should().Be("ES2022");
    }
}
