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
    public const int MaxSummaryLength = 1200;
    public const int MaxRecommendedNextActionLength = 500;
    public const int MaxArrayItems = 8;
    public const int MaxArrayItemLength = 300;

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

            if (!TryGetRequiredString(root, "summary", MaxSummaryLength, out var summary)
                || !TryGetRequiredString(root, "recommendedNextAction", MaxRecommendedNextActionLength, out var nextAction))
            {
                return AiOperationResult<LeadAnalysisResult>.Fail("Model response was missing a valid summary or next action.");
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
                Summary = summary,
                RecommendedNextAction = nextAction,
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

            if (!TryGetRequiredString(root, "structuredSummary", MaxSummaryLength, out var summary)
                || !TryGetRequiredString(root, "recommendedNextAction", MaxRecommendedNextActionLength, out var nextAction))
            {
                return AiOperationResult<MeetingNotesResult>.Fail("Model response was missing a valid summary or next action.");
            }

            if (!TryGetRequiredStringArray(root, "keyFacts", out var keyFacts)
                || !TryGetRequiredStringArray(root, "risks", out var risks))
            {
                return AiOperationResult<MeetingNotesResult>.Fail("Model response was missing valid key facts or risks.");
            }

            var result = new MeetingNotesResult
            {
                StructuredSummary = summary,
                RecommendedNextAction = nextAction,
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

    private static bool TryGetRequiredString(JsonElement root, string name, int maxLength, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var trimmed = el.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > maxLength)
        {
            return false;
        }

        value = trimmed;
        return true;
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
        if (!TryGetRequiredString(root, name, 40, out var trimmed))
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

        if (el.GetArrayLength() > MaxArrayItems)
        {
            return false;
        }

        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                return false;
            }

            var trimmed = item.GetString()!.Trim();
            if (trimmed.Length > MaxArrayItemLength)
            {
                return false;
            }

            list.Add(trimmed);
        }

        return true;
    }
}
