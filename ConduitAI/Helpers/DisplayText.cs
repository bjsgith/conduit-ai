using System.Text;

namespace ConduitAI.Helpers;

/// <summary>
/// Presentation helpers for turning enum names and values into readable text.
/// </summary>
public static class DisplayText
{
    /// <summary>
    /// Splits a PascalCase identifier into spaced words (e.g. "TourScheduled" -> "Tour Scheduled").
    /// </summary>
    public static string Humanize(Enum value) => Humanize(value.ToString());

    public static string Humanize(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(pascalCase.Length + 4);
        for (var i = 0; i < pascalCase.Length; i++)
        {
            var c = pascalCase[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(pascalCase[i - 1]))
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// CSS modifier suffix for a value, e.g. "TourScheduled" -> "tourscheduled".
    /// </summary>
    public static string Slug(Enum value) => value.ToString().ToLowerInvariant();

    /// <summary>
    /// CSS modifier class for a 0-100 lead score (hot / warm / cool).
    /// </summary>
    public static string ScoreClass(int score) => score switch
    {
        >= 75 => "score-hot",
        >= 50 => "score-warm",
        _ => "score-cool"
    };
}
