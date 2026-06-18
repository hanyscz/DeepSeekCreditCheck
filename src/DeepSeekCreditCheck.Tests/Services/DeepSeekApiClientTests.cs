using System.Net;
using System.Net.Http;
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

        Assert.Equal("USD", result.Currency);
        Assert.Equal("103.50", result.TotalBalance);
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

}
