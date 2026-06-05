using ConduitAI.Models.Enums;
using ConduitAI.Services;
using Xunit;

namespace ConduitAI.Tests;

public class AiResponseParserTests
{
    private readonly AiResponseParser _parser = new();

    [Fact]
    public void ParseLeadAnalysis_ValidJson_Succeeds()
    {
        var raw = """
        {
          "summary": "Motivated relocating buyer.",
          "leadScore": 86,
          "urgencyLevel": "High",
          "buyingIntent": "High",
          "recommendedNextAction": "Schedule a tour within 48 hours."
        }
        """;

        var result = _parser.ParseLeadAnalysis(raw);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(86, result.Value!.LeadScore);
        Assert.Equal(UrgencyLevel.High, result.Value.UrgencyLevel);
        Assert.Equal(BuyingIntent.High, result.Value.BuyingIntent);
        Assert.Equal("Motivated relocating buyer.", result.Value.Summary);
    }

    [Fact]
    public void ParseLeadAnalysis_ExtractsJsonFromSurroundingProse()
    {
        var raw = "Sure! Here is the analysis:\n```json\n{\"summary\":\"S\",\"leadScore\":40," +
                  "\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Medium\",\"recommendedNextAction\":\"Call back.\"}\n```\nHope this helps.";

        var result = _parser.ParseLeadAnalysis(raw);

        Assert.True(result.Success);
        Assert.Equal(40, result.Value!.LeadScore);
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(-20, 0)]
    [InlineData(73, 73)]
    public void ParseLeadAnalysis_ClampsScore(int input, int expected)
    {
        var raw = $"{{\"summary\":\"S\",\"leadScore\":{input},\"urgencyLevel\":\"Medium\"," +
                  "\"buyingIntent\":\"Medium\",\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseLeadAnalysis(raw);

        Assert.True(result.Success);
        Assert.Equal(expected, result.Value!.LeadScore);
    }

    [Fact]
    public void ParseLeadAnalysis_UnknownEnum_FallsBackToMedium()
    {
        var raw = "{\"summary\":\"S\",\"leadScore\":50,\"urgencyLevel\":\"URGENT!!\"," +
                  "\"buyingIntent\":\"maybe\",\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseLeadAnalysis(raw);

        Assert.True(result.Success);
        Assert.Equal(UrgencyLevel.Medium, result.Value!.UrgencyLevel);
        Assert.Equal(BuyingIntent.Medium, result.Value.BuyingIntent);
    }

    [Fact]
    public void ParseLeadAnalysis_MissingSummary_Fails()
    {
        var raw = "{\"leadScore\":50,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseLeadAnalysis(raw);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void ParseLeadAnalysis_EmptyNextAction_Fails()
    {
        var raw = "{\"summary\":\"S\",\"leadScore\":50,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"   \"}";

        var result = _parser.ParseLeadAnalysis(raw);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("")]
    [InlineData("{ broken json ")]
    public void ParseLeadAnalysis_Malformed_Fails(string raw)
    {
        var result = _parser.ParseLeadAnalysis(raw);
        Assert.False(result.Success);
    }

    [Fact]
    public void ParseLeadAnalysis_ScoreAsString_IsParsed()
    {
        var raw = "{\"summary\":\"S\",\"leadScore\":\"77\",\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseLeadAnalysis(raw);

        Assert.True(result.Success);
        Assert.Equal(77, result.Value!.LeadScore);
    }

    [Fact]
    public void ParseMeetingNotes_ValidJson_Succeeds()
    {
        var raw = """
        {
          "structuredSummary": "Buyer wants a single-story home.",
          "keyFacts": ["Budget 800k", "Prefers Scottsdale"],
          "risks": ["Selling current home first"],
          "recommendedNextAction": "Send three matching listings."
        }
        """;

        var result = _parser.ParseMeetingNotes(raw);

        Assert.True(result.Success);
        Assert.Equal(2, result.Value!.KeyFacts.Count);
        Assert.Single(result.Value.Risks);
        Assert.Equal("Send three matching listings.", result.Value.RecommendedNextAction);
    }

    [Fact]
    public void ParseMeetingNotes_MissingArrays_DefaultToEmpty()
    {
        var raw = "{\"structuredSummary\":\"S\",\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseMeetingNotes(raw);

        Assert.True(result.Success);
        Assert.Empty(result.Value!.KeyFacts);
        Assert.Empty(result.Value.Risks);
    }

    [Fact]
    public void ParseMeetingNotes_MissingSummary_Fails()
    {
        var raw = "{\"keyFacts\":[],\"risks\":[],\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseMeetingNotes(raw);

        Assert.False(result.Success);
    }
}
