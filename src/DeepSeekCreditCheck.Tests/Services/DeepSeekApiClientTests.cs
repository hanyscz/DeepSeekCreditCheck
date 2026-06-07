using System.Net;
using Moq;
using Moq.Protected;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class DeepSeekApiClientTests
{
    [Fact]
    public async Task GetBalance_ValidResponse_ReturnsSnapshot()
    {
        var json = @"{
            ""is_available"": true,
            ""balance_infos"": [{
                ""currency"": ""USD"",
                ""total_balance"": ""103.50"",
                ""granted_balance"": ""14.50"",
                ""topped_up_balance"": ""89.00""
            }]
        }";

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        var client = new DeepSeekApiClient(new HttpClient(handler.Object));
        var result = await client.GetBalanceAsync("test-key");

        Assert.True(result.IsAvailable);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("103.50", result.TotalBalance);
        Assert.Equal("14.50", result.GrantedBalance);
        Assert.Equal("89.00", result.ToppedUpBalance);
    }

    [Fact]
    public async Task GetBalance_Unauthorized_Throws()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized
            });

        var client = new DeepSeekApiClient(new HttpClient(handler.Object));
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetBalanceAsync("bad-key"));
    }

    [Fact]
    public async Task GetUsage_NonSuccessStatusCode_ReturnsNull()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/v1/usage")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var client = new DeepSeekApiClient(new HttpClient(handler.Object));
        var result = await client.GetUsageAsync("test-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUsage_ValidResponse_ReturnsUsageRecord()
    {
        var json = @"{
            ""total_usage"": {
                ""total_tokens"": 15000,
                ""input_tokens"": 10000,
                ""output_tokens"": 5000,
                ""cached_tokens"": 2000
            }
        }";

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("/v1/usage")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        var client = new DeepSeekApiClient(new HttpClient(handler.Object));
        var result = await client.GetUsageAsync("test-key");

        Assert.NotNull(result);
        Assert.Equal(15000, result!.TotalTokens);
        Assert.Equal(10000, result.InputTokens);
        Assert.Equal(5000, result.OutputTokens);
        Assert.Equal(2000, result.CachedTokens);
    }
}
