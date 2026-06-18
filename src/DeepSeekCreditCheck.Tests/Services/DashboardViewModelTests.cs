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
}
