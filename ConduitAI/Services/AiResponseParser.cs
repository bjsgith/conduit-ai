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

            if (!TryGetRequiredInt(root, "leadScore", out var leadScore))
            {
                return AiOperationResult<LeadAnalysisResult>.Fail("Model response was missing a valid lead score.");
            }

            if (!TryGetRequiredEnum(root, "urgencyLevel", out UrgencyLevel urgencyLevel)
                || !TryGetRequiredEnum(root, "buyingIntent", out BuyingIntent buyingIntent))
            {
                return AiOperationResult<LeadAnalysisResult>.Fail("Model response was missing a valid urgency level or buying intent.");
            }

            var result = new LeadAnalysisResult
            {
                Summary = summary.Trim(),
                RecommendedNextAction = nextAction.Trim(),
                LeadScore = ClampScore(leadScore),
                UrgencyLevel = urgencyLevel,
                BuyingIntent = buyingIntent
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

            if (!TryGetRequiredStringArray(root, "keyFacts", out var keyFacts)
                || !TryGetRequiredStringArray(root, "risks", out var risks))
            {
                return AiOperationResult<MeetingNotesResult>.Fail("Model response was missing valid key facts or risks.");
            }

            var result = new MeetingNotesResult
            {
                StructuredSummary = summary.Trim(),
                RecommendedNextAction = nextAction.Trim(),
                KeyFacts = keyFacts,
                Risks = risks
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

    private static bool TryGetRequiredInt(JsonElement root, string name, out int value)
    {
        value = 0;
        if (!root.TryGetProperty(name, out var el))
        {
            return false;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
        {
            value = n;
            return true;
        }

        if (el.ValueKind == JsonValueKind.String
            && int.TryParse(el.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetRequiredEnum<TEnum>(JsonElement root, string name, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        var raw = GetString(root, name);
        var trimmed = raw?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        if (int.TryParse(trimmed, out _) || trimmed.Contains(','))
        {
            return false;
        }

        return Enum.TryParse(trimmed, ignoreCase: true, out value)
            && Enum.IsDefined(typeof(TEnum), value);
    }

    private static bool TryGetRequiredStringArray(JsonElement root, string name, out List<string> list)
    {
        list = new List<string>();
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                return false;
            }

            list.Add(item.GetString()!.Trim());
        }

        return true;
    }
}
