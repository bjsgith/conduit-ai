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
    public void ParseLeadAnalysis_UnknownEnum_Fails()
    {
        var raw = "{\"summary\":\"S\",\"leadScore\":50,\"urgencyLevel\":\"URGENT!!\"," +
                  "\"buyingIntent\":\"maybe\",\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseLeadAnalysis(raw);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("{\"summary\":\"S\",\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"summary\":\"S\",\"leadScore\":\"high\",\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"summary\":\"S\",\"leadScore\":null,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    public void ParseLeadAnalysis_MissingOrInvalidScore_Fails(string raw)
    {
        var result = _parser.ParseLeadAnalysis(raw);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("{\"summary\":\"S\",\"leadScore\":50,\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"summary\":\"S\",\"leadScore\":50,\"urgencyLevel\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"summary\":\"S\",\"leadScore\":50,\"urgencyLevel\":\"1\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    public void ParseLeadAnalysis_MissingOrInvalidEnums_Fails(string raw)
    {
        var result = _parser.ParseLeadAnalysis(raw);

        Assert.False(result.Success);
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
    [InlineData("{\"summary\":123,\"leadScore\":50,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"summary\":\"S\",\"leadScore\":50,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":123}")]
    [InlineData("{\"summary\":true,\"leadScore\":50,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"summary\":{\"text\":\"S\"},\"leadScore\":50,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}")]
    public void ParseLeadAnalysis_NonStringTextFields_Fail(string raw)
    {
        var result = _parser.ParseLeadAnalysis(raw);

        Assert.False(result.Success);
    }

    [Fact]
    public void ParseLeadAnalysis_OverlongTextFields_Fail()
    {
        var longSummary = new string('x', AiResponseParser.MaxSummaryLength + 1);
        var longAction = new string('x', AiResponseParser.MaxRecommendedNextActionLength + 1);

        var summaryResult = _parser.ParseLeadAnalysis(
            $"{{\"summary\":\"{longSummary}\",\"leadScore\":50,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"A\"}}");
        var actionResult = _parser.ParseLeadAnalysis(
            $"{{\"summary\":\"S\",\"leadScore\":50,\"urgencyLevel\":\"Low\",\"buyingIntent\":\"Low\",\"recommendedNextAction\":\"{longAction}\"}}");

        Assert.False(summaryResult.Success);
        Assert.False(actionResult.Success);
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
    public void ParseMeetingNotes_EmptyArrays_Succeeds()
    {
        var raw = "{\"structuredSummary\":\"S\",\"keyFacts\":[],\"risks\":[],\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseMeetingNotes(raw);

        Assert.True(result.Success);
        Assert.Empty(result.Value!.KeyFacts);
        Assert.Empty(result.Value.Risks);
    }

    [Fact]
    public void ParseMeetingNotes_MissingArrays_Fails()
    {
        var raw = "{\"structuredSummary\":\"S\",\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseMeetingNotes(raw);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("{\"structuredSummary\":\"S\",\"keyFacts\":\"Budget 800k\",\"risks\":[],\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"structuredSummary\":\"S\",\"keyFacts\":[],\"risks\":null,\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"structuredSummary\":\"S\",\"keyFacts\":[123],\"risks\":[],\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"structuredSummary\":\"S\",\"keyFacts\":[],\"risks\":[\"   \"],\"recommendedNextAction\":\"A\"}")]
    public void ParseMeetingNotes_MalformedArrays_Fails(string raw)
    {
        var result = _parser.ParseMeetingNotes(raw);

        Assert.False(result.Success);
    }

    [Fact]
    public void ParseMeetingNotes_MissingSummary_Fails()
    {
        var raw = "{\"keyFacts\":[],\"risks\":[],\"recommendedNextAction\":\"A\"}";

        var result = _parser.ParseMeetingNotes(raw);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("{\"structuredSummary\":123,\"keyFacts\":[],\"risks\":[],\"recommendedNextAction\":\"A\"}")]
    [InlineData("{\"structuredSummary\":\"S\",\"keyFacts\":[],\"risks\":[],\"recommendedNextAction\":123}")]
    [InlineData("{\"structuredSummary\":false,\"keyFacts\":[],\"risks\":[],\"recommendedNextAction\":\"A\"}")]
    public void ParseMeetingNotes_NonStringTextFields_Fail(string raw)
    {
        var result = _parser.ParseMeetingNotes(raw);

        Assert.False(result.Success);
    }

    [Fact]
    public void ParseMeetingNotes_OverlongFieldsOrArrays_Fail()
    {
        var longSummary = new string('x', AiResponseParser.MaxSummaryLength + 1);
        var longAction = new string('x', AiResponseParser.MaxRecommendedNextActionLength + 1);
        var longItem = new string('x', AiResponseParser.MaxArrayItemLength + 1);
        var tooManyFacts = string.Join(",", Enumerable.Range(0, AiResponseParser.MaxArrayItems + 1).Select(i => $"\"fact {i}\""));

        var summaryResult = _parser.ParseMeetingNotes(
            $"{{\"structuredSummary\":\"{longSummary}\",\"keyFacts\":[],\"risks\":[],\"recommendedNextAction\":\"A\"}}");
        var actionResult = _parser.ParseMeetingNotes(
            $"{{\"structuredSummary\":\"S\",\"keyFacts\":[],\"risks\":[],\"recommendedNextAction\":\"{longAction}\"}}");
        var itemResult = _parser.ParseMeetingNotes(
            $"{{\"structuredSummary\":\"S\",\"keyFacts\":[\"{longItem}\"],\"risks\":[],\"recommendedNextAction\":\"A\"}}");
        var countResult = _parser.ParseMeetingNotes(
            $"{{\"structuredSummary\":\"S\",\"keyFacts\":[{tooManyFacts}],\"risks\":[],\"recommendedNextAction\":\"A\"}}");

        Assert.False(summaryResult.Success);
        Assert.False(actionResult.Success);
        Assert.False(itemResult.Success);
        Assert.False(countResult.Success);
    }
}
