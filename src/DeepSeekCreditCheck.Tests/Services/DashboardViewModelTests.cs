using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.Core.Services;
using DeepSeekCreditCheck.UI.ViewModels;
using Moq;
using Xunit;

namespace DeepSeekCreditCheck.Tests.Services;

public class DashboardViewModelTests
{
    private readonly Mock<IPollingService> _pollingMock;
    private readonly Mock<IBalanceRepository> _balanceRepoMock;
    private readonly Mock<PredictionEngine> _predictionEngineMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly Mock<IDeepSeekPlatformClient> _platformClientMock;
    private readonly Mock<IAppSettingsService> _settingsMock;
    private readonly Mock<IUsageRepository> _usageRepoMock;

    public DashboardViewModelTests()
    {
        _pollingMock = new Mock<IPollingService>();
        _balanceRepoMock = new Mock<IBalanceRepository>();
        _predictionEngineMock = new Mock<PredictionEngine>();
        _updateServiceMock = new Mock<IUpdateService>();
        _platformClientMock = new Mock<IDeepSeekPlatformClient>();
        _settingsMock = new Mock<IAppSettingsService>();
        _usageRepoMock = new Mock<IUsageRepository>();
        
        // Default settings setups
        _settingsMock.Setup(s => s.GetSessionTokenAsync()).ReturnsAsync("mock-session-token");
    }

    [Fact]
    public async Task MonthNavigation_PreviousMonthCommand_DecrementsSelectedMonth()
    {
        // Arrange
        var today = DateTime.Today;
        var prevMonth = today.AddMonths(-1);

        _platformClientMock.Setup(c => c.GetUsageAmountAsync(It.IsAny<string>(), prevMonth.Year, prevMonth.Month))
            .ReturnsAsync(new JsonObject { ["code"] = 0 });
        _platformClientMock.Setup(c => c.GetUsageCostAsync(It.IsAny<string>(), prevMonth.Year, prevMonth.Month))
            .ReturnsAsync(new JsonObject { ["code"] = 0 });

        var vm = new DashboardViewModel(
            _pollingMock.Object,
            _balanceRepoMock.Object,
            _predictionEngineMock.Object,
            _updateServiceMock.Object,
            _platformClientMock.Object,
            _settingsMock.Object,
            _usageRepoMock.Object
        );

        // Act
        vm.PreviousMonthCommand.Execute(null);

        // Give a tiny moment for async execution of command if needed, though Wpf Command executes synchronously or starts a Task
        // Wait, the command is: PreviousMonthCommand = new RelayCommand(async _ => await GoToPreviousMonthAsync());
        // Since it is async void under the hood when executed via ICommand, let's wait/verify.
        // Let's call the VM method directly or use a brief wait, or just let it finish.
        // Actually, we can call vm.PreviousMonthCommand.Execute(null) and since it has IsLoading = true/false we can wait till IsLoading is false
        int retry = 0;
        while (vm.IsLoading && retry < 100)
        {
            await Task.Delay(10);
            retry++;
        }

        // Assert
        Assert.Equal($"{prevMonth.Year}-{prevMonth.Month:D2}", vm.PlatformHeading);
        _platformClientMock.Verify(c => c.GetUsageAmountAsync("mock-session-token", prevMonth.Year, prevMonth.Month), Times.Once);
        _platformClientMock.Verify(c => c.GetUsageCostAsync("mock-session-token", prevMonth.Year, prevMonth.Month), Times.Once);
    }

    [Fact]
    public void MonthNavigation_NextMonthCommand_DisabledOnCurrentMonth()
    {
        // Arrange
        var vm = new DashboardViewModel(
            _pollingMock.Object,
            _balanceRepoMock.Object,
            _predictionEngineMock.Object,
            _updateServiceMock.Object,
            _platformClientMock.Object,
            _settingsMock.Object,
            _usageRepoMock.Object
        );

        // Act & Assert
        Assert.False(vm.NextMonthCommand.CanExecute(null));
    }

    [Fact]
    public async Task MonthNavigation_NextMonthCommand_EnabledOnPreviousMonth_AndIncrementsSelectedMonth()
    {
        // Arrange
        var today = DateTime.Today;
        var prevMonth = today.AddMonths(-1);

        _platformClientMock.Setup(c => c.GetUsageAmountAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new JsonObject { ["code"] = 0 });
        _platformClientMock.Setup(c => c.GetUsageCostAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new JsonObject { ["code"] = 0 });

        var vm = new DashboardViewModel(
            _pollingMock.Object,
            _balanceRepoMock.Object,
            _predictionEngineMock.Object,
            _updateServiceMock.Object,
            _platformClientMock.Object,
            _settingsMock.Object,
            _usageRepoMock.Object
        );

        // Navigate back first
        vm.PreviousMonthCommand.Execute(null);
        int retry = 0;
        while (vm.IsLoading && retry < 100)
        {
            await Task.Delay(10);
            retry++;
        }

        // Verify next command is now enabled
        Assert.True(vm.NextMonthCommand.CanExecute(null));

        // Act
        vm.NextMonthCommand.Execute(null);
        retry = 0;
        while (vm.IsLoading && retry < 100)
        {
            await Task.Delay(10);
            retry++;
        }

        // Assert
        Assert.Equal($"{today.Year}-{today.Month:D2}", vm.PlatformHeading);
        Assert.False(vm.NextMonthCommand.CanExecute(null)); // Should be disabled again on current month
    }

    [Fact]
    public async Task UpdatePlatformStats_ParsesTodayStats_WhenCurrentMonthIsSelected()
    {
        // Arrange
        var today = DateTime.Today;
        var todayStr = today.ToString("yyyy-MM-dd");

        // mock platform data pro usage amount
        var amountJson = new JsonObject
        {
            ["code"] = 0,
            ["data"] = new JsonObject
            {
                ["biz_data"] = new JsonObject
                {
                    ["total"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["model"] = "deepseek-chat",
                            ["usage"] = new JsonArray
                            {
                                new JsonObject { ["type"] = "PROMPT_CACHE_HIT_TOKEN", ["amount"] = 1000000 },
                                new JsonObject { ["type"] = "PROMPT_CACHE_MISS_TOKEN", ["amount"] = 200000 },
                                new JsonObject { ["type"] = "RESPONSE_TOKEN", ["amount"] = 30000 }
                            }
                        }
                    },
                    ["days"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["date"] = todayStr,
                            ["data"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["model"] = "deepseek-chat", // Pro model
                                    ["usage"] = new JsonArray
                                    {
                                        new JsonObject { ["type"] = "PROMPT_CACHE_HIT_TOKEN", ["amount"] = 10000 },
                                        new JsonObject { ["type"] = "PROMPT_CACHE_MISS_TOKEN", ["amount"] = 2000 },
                                        new JsonObject { ["type"] = "RESPONSE_TOKEN", ["amount"] = 300 }
                                    }
                                },
                                new JsonObject
                                {
                                    ["model"] = "deepseek-coder", // Pro model
                                    ["usage"] = new JsonArray
                                    {
                                        new JsonObject { ["type"] = "PROMPT_CACHE_HIT_TOKEN", ["amount"] = 5000 },
                                        new JsonObject { ["type"] = "PROMPT_CACHE_MISS_TOKEN", ["amount"] = 1000 },
                                        new JsonObject { ["type"] = "RESPONSE_TOKEN", ["amount"] = 100 }
                                    }
                                },
                                new JsonObject
                                {
                                    ["model"] = "deepseek-flash", // Flash model
                                    ["usage"] = new JsonArray
                                    {
                                        new JsonObject { ["type"] = "PROMPT_CACHE_HIT_TOKEN", ["amount"] = 20000 },
                                        new JsonObject { ["type"] = "PROMPT_CACHE_MISS_TOKEN", ["amount"] = 4000 },
                                        new JsonObject { ["type"] = "RESPONSE_TOKEN", ["amount"] = 400 }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // mock platform data pro usage cost
        var costJson = new JsonObject
        {
            ["code"] = 0,
            ["data"] = new JsonObject
            {
                ["biz_data"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["total"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["model"] = "deepseek-chat",
                                ["usage"] = new JsonArray
                                {
                                    new JsonObject { ["type"] = "PROMPT_CACHE_HIT_TOKEN", ["cost"] = 5.50 }
                                }
                            }
                        },
                        ["days"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["date"] = todayStr,
                                ["data"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["model"] = "deepseek-chat",
                                        ["usage"] = new JsonArray
                                        {
                                            new JsonObject { ["type"] = "PROMPT_CACHE_HIT_TOKEN", ["cost"] = 0.05 }
                                        }
                                    },
                                    new JsonObject
                                    {
                                        ["model"] = "deepseek-coder",
                                        ["usage"] = new JsonArray
                                        {
                                            new JsonObject { ["type"] = "PROMPT_CACHE_HIT_TOKEN", ["cost"] = 0.02 }
                                        }
                                    },
                                    new JsonObject
                                    {
                                        ["model"] = "deepseek-flash",
                                        ["usage"] = new JsonArray
                                        {
                                            new JsonObject { ["type"] = "PROMPT_CACHE_HIT_TOKEN", ["cost"] = 0.01 }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        _platformClientMock.Setup(c => c.GetUsageAmountAsync(It.IsAny<string>(), today.Year, today.Month))
            .ReturnsAsync(amountJson);
        _platformClientMock.Setup(c => c.GetUsageCostAsync(It.IsAny<string>(), today.Year, today.Month))
            .ReturnsAsync(costJson);

        var vm = new DashboardViewModel(
            _pollingMock.Object,
            _balanceRepoMock.Object,
            _predictionEngineMock.Object,
            _updateServiceMock.Object,
            _platformClientMock.Object,
            _settingsMock.Object,
            _usageRepoMock.Object
        );

        // Act
        await vm.LoadPlatformStatsAsync();

        // Assert
        var decSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var grpSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;

        Assert.True(vm.IsPlatformTodayVisible);
        Assert.Equal(todayStr, vm.PlatformTodayDateText);

        // Pro model:
        // Input: 2000 + 1000 = 3000
        // Cache: 10000 + 5000 = 15000
        // Output: 300 + 100 = 400
        // Total: 3000 + 15000 + 400 = 18400
        // Cost: 0.05 + 0.02 = 0.07 => $0.07
        Assert.Equal($"3{grpSep}000", vm.PlatformTodayProInput);
        Assert.Equal($"15{grpSep}000", vm.PlatformTodayProCache);
        Assert.Equal("400", vm.PlatformTodayProOutput);
        Assert.Equal($"18{grpSep}400", vm.PlatformTodayProTotal);
        Assert.Equal($"$0{decSep}07", vm.PlatformTodayProCost);

        // Flash model:
        // Input: 4000
        // Cache: 20000
        // Output: 400
        // Total: 24400
        // Cost: 0.01 => $0.01
        Assert.Equal($"4{grpSep}000", vm.PlatformTodayFlashInput);
        Assert.Equal($"20{grpSep}000", vm.PlatformTodayFlashCache);
        Assert.Equal("400", vm.PlatformTodayFlashOutput);
        Assert.Equal($"24{grpSep}400", vm.PlatformTodayFlashTotal);
        Assert.Equal($"$0{decSep}01", vm.PlatformTodayFlashCost);

        // Celkem:
        // Input: 6000 (Pro input 3000 + Flash input 4000 = 7000)
        // Cache: 35000
        // Output: 800
        // Total: 42800
        // Cost: 0.08 => $0.08
        Assert.Equal($"7{grpSep}000", vm.PlatformTodayTotalInput);
        Assert.Equal($"35{grpSep}000", vm.PlatformTodayTotalCache);
        Assert.Equal("800", vm.PlatformTodayTotalOutput);
        Assert.Equal($"42{grpSep}800", vm.PlatformTodayTotalTotal);
        Assert.Equal($"$0{decSep}08", vm.PlatformTodayTotalCost);
    }

    [Fact]
    public async Task UpdatePlatformStats_HidesTodayStats_WhenPreviousMonthIsSelected()
    {
        // Arrange
        var today = DateTime.Today;
        var prevMonth = today.AddMonths(-1);

        _platformClientMock.Setup(c => c.GetUsageAmountAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new JsonObject { ["code"] = 0 });
        _platformClientMock.Setup(c => c.GetUsageCostAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new JsonObject { ["code"] = 0 });

        var vm = new DashboardViewModel(
            _pollingMock.Object,
            _balanceRepoMock.Object,
            _predictionEngineMock.Object,
            _updateServiceMock.Object,
            _platformClientMock.Object,
            _settingsMock.Object,
            _usageRepoMock.Object
        );

        // Act - navigace na předchozí měsíc
        vm.PreviousMonthCommand.Execute(null);
        int retry = 0;
        while (vm.IsLoading && retry < 100)
        {
            await Task.Delay(10);
            retry++;
        }

        // Assert
        Assert.False(vm.IsPlatformTodayVisible);
        Assert.Equal("—", vm.PlatformTodayDateText);
    }
}

