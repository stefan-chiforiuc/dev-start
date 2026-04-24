using System.Text.Json;
using System.Text.Json.Nodes;

namespace DevStart;

/// <summary>
/// Deep-merges a JSON fragment into a target JSON string. Objects are merged
/// key-by-key (fragment wins on conflicts). Arrays are concatenated with
/// de-duplication of scalar values. Scalars in the fragment overwrite.
///
/// Used by the <c>json-merge</c> injector mode for files where comment-markers
/// aren't possible: <c>package.json</c>, <c>tsconfig.json</c>, <c>.mcp.json</c>.
/// </summary>
public static class JsonMerger
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    public static string Merge(string targetJson, string fragmentJson)
    {
        var target = JsonNode.Parse(targetJson);
        var fragment = JsonNode.Parse(fragmentJson);
        var merged = MergeNodes(target, fragment) ?? target ?? fragment;
        return merged?.ToJsonString(WriteOptions) ?? targetJson;
    }

    private static JsonNode? MergeNodes(JsonNode? target, JsonNode? fragment)
    {
        if (fragment is null) return target;
        if (target is null) return fragment.DeepClone();

        if (target is JsonObject targetObj && fragment is JsonObject fragmentObj)
        {
            foreach (var kv in fragmentObj)
            {
                if (targetObj.ContainsKey(kv.Key))
                {
                    var childTarget = targetObj[kv.Key];
                    targetObj.Remove(kv.Key);
                    var mergedChild = MergeNodes(childTarget, kv.Value);
                    targetObj[kv.Key] = mergedChild?.DeepClone();
                }
                else
                {
                    targetObj[kv.Key] = kv.Value?.DeepClone();
                }
            }
            return targetObj;
        }

        if (target is JsonArray targetArr && fragment is JsonArray fragmentArr)
        {
            var existing = new HashSet<string>(
                targetArr.Select(n => n?.ToJsonString() ?? "null"), StringComparer.Ordinal);
            foreach (var item in fragmentArr)
            {
                var signature = item?.ToJsonString() ?? "null";
                if (existing.Add(signature))
                {
                    targetArr.Add(item?.DeepClone());
                }
            }
            return targetArr;
        }

        // Mismatched shapes or scalars — fragment wins.
        return fragment.DeepClone();
    }
}
