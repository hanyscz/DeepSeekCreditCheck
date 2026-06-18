using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DeepSeekCreditCheck.Core.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace DeepSeekCreditCheck.Tests.Services;

public class DeepSeekPlatformClientTests
{
    [Fact]
    public async Task GetUserSummary_ValidResponse_ReturnsJsonNode()
    {
        var json = @"{
            ""code"": 0,
            ""msg"": ""success"",
            ""data"": {
                ""user_id"": 123456,
                ""username"": ""testuser""
            }
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

        var client = new DeepSeekPlatformClient(new HttpClient(handler.Object));
        var result = await client.GetUserSummaryAsync("test-token");

        Assert.NotNull(result);
        Assert.Equal(0, result["code"]?.GetValue<int>());
        Assert.Equal("success", result["msg"]?.GetValue<string>());
        Assert.Equal("testuser", result["data"]?["username"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetUsageAmount_ValidResponse_ReturnsJsonNode()
    {
        var json = @"{
            ""code"": 0,
            ""data"": {
                ""usage_amount"": [
                    {
                        ""model"": ""deepseek-chat"",
                        ""prompt_cache_hit_tokens"": 1000,
                        ""prompt_cache_miss_tokens"": 2000,
                        ""response_tokens"": 500
                    }
                ]
            }
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

        var client = new DeepSeekPlatformClient(new HttpClient(handler.Object));
        var result = await client.GetUsageAmountAsync("test-token", 2026, 6);

        Assert.NotNull(result);
        var firstItem = result["data"]?["usage_amount"]?[0];
        Assert.NotNull(firstItem);
        Assert.Equal("deepseek-chat", firstItem["model"]?.GetValue<string>());
        Assert.Equal(1000, firstItem["prompt_cache_hit_tokens"]?.GetValue<long>());
    }

    [Fact]
    public async Task GetUsageCost_ValidResponse_ReturnsJsonNode()
    {
        var json = @"{
            ""code"": 0,
            ""data"": {
                ""usage_cost"": [
                    {
                        ""model"": ""deepseek-chat"",
                        ""cost"": ""0.1234""
                    }
                ]
            }
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

        var client = new DeepSeekPlatformClient(new HttpClient(handler.Object));
        var result = await client.GetUsageCostAsync("test-token", 2026, 6);

        Assert.NotNull(result);
        var firstItem = result["data"]?["usage_cost"]?[0];
        Assert.NotNull(firstItem);
        Assert.Equal("0.1234", firstItem["cost"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetJson_Unauthorized_ThrowsHttpRequestException()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized access")
            });

        var client = new DeepSeekPlatformClient(new HttpClient(handler.Object));
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetUserSummaryAsync("bad-token"));
    }
}
