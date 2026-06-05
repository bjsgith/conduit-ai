using System.Text.Json;
using ConduitAI.Models.Enums;
using ConduitAI.Services.Ai;

namespace ConduitAI.Services;

/// <summary>
/// Validates and normalizes model JSON into typed AI results. Tolerates loose
/// model output (surrounding prose, code fences) by extracting the JSON object,
/// then enforces the documented contract.
/// </summary>
public class AiResponseParser
{
    public AiOperationResult<LeadAnalysisResult> ParseLeadAnalysis(string raw)
    {
        if (!TryExtractJsonObject(raw, out var json))
        {
            return AiOperationResult<LeadAnalysisResult>.Fail("Model response did not contain a JSON object.");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var summary = GetString(root, "summary");
            var nextAction = GetString(root, "recommendedNextAction");

            if (string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(nextAction))
            {
                return AiOperationResult<LeadAnalysisResult>.Fail("Model response was missing a summary or next action.");
            }

            var result = new LeadAnalysisResult
            {
                Summary = summary.Trim(),
                RecommendedNextAction = nextAction.Trim(),
                LeadScore = ClampScore(GetInt(root, "leadScore")),
                UrgencyLevel = ParseEnum(GetString(root, "urgencyLevel"), UrgencyLevel.Medium),
                BuyingIntent = ParseEnum(GetString(root, "buyingIntent"), BuyingIntent.Medium)
            };

            return AiOperationResult<LeadAnalysisResult>.Ok(result);
        }
        catch (JsonException)
        {
            return AiOperationResult<LeadAnalysisResult>.Fail("Model response was not valid JSON.");
        }
    }

    public AiOperationResult<MeetingNotesResult> ParseMeetingNotes(string raw)
    {
        if (!TryExtractJsonObject(raw, out var json))
        {
            return AiOperationResult<MeetingNotesResult>.Fail("Model response did not contain a JSON object.");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var summary = GetString(root, "structuredSummary");
            var nextAction = GetString(root, "recommendedNextAction");

            if (string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(nextAction))
            {
                return AiOperationResult<MeetingNotesResult>.Fail("Model response was missing a summary or next action.");
            }

            var result = new MeetingNotesResult
            {
                StructuredSummary = summary.Trim(),
                RecommendedNextAction = nextAction.Trim(),
                KeyFacts = GetStringArray(root, "keyFacts"),
                Risks = GetStringArray(root, "risks")
            };

            return AiOperationResult<MeetingNotesResult>.Ok(result);
        }
        catch (JsonException)
        {
            return AiOperationResult<MeetingNotesResult>.Fail("Model response was not valid JSON.");
        }
    }

    /// <summary>
    /// Extracts the outermost JSON object from a possibly-noisy string.
    /// </summary>
    internal static bool TryExtractJsonObject(string raw, out string json)
    {
        json = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return false;
        }

        json = raw.Substring(start, end - start + 1);
        return true;
    }

    private static int ClampScore(int score) => Math.Clamp(score, 0, 100);

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.ToString(),
            _ => null
        };
    }

    private static int GetInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el))
        {
            return 0;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
        {
            return n;
        }

        if (el.ValueKind == JsonValueKind.String
            && int.TryParse(el.GetString(), out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static List<string> GetStringArray(JsonElement root, string name)
    {
        var list = new List<string>();
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return list;
        }

        foreach (var item in el.EnumerateArray())
        {
            var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                list.Add(s.Trim());
            }
        }

        return list;
    }
}
