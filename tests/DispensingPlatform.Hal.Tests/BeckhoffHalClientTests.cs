using DispensingPlatform.Hal.Beckhoff;
using DispensingPlatform.Hal.Beckhoff.Ads;
using DispensingPlatform.Hal.Beckhoff.Models;
using UnitsNet;
using Xunit;

namespace DispensingPlatform.Hal.Tests;

/// <summary>
/// Beckhoff HAL 行为测试。
/// </summary>
/// <remarks>
/// 测试目标：
/// <list type="bullet">
/// <item><description>验证轴类型约束（仅 3 轴旋转）。</description></item>
/// <item><description>验证运动命令写入的 PLC 符号路径和触发顺序。</description></item>
/// <item><description>验证旋转轴状态读取时使用模轴位置字段。</description></item>
/// </list>
/// 所有测试均通过 Fake ADS 执行，不依赖真实 PLC 或现场网络。
/// </remarks>
public sealed class BeckhoffHalClientTests
{
    /// <summary>
    /// 验证轴目录中的旋转轴规则与业务约束一致。
    /// </summary>
    [Fact]
    public void AxisCatalog_UsesExpectedRotaryRules()
    {
        Assert.Equal(BeckhoffAxisType.Rotary, BeckhoffAxisCatalog.Get(3).AxisType);
        Assert.Equal(BeckhoffAxisType.Linear, BeckhoffAxisCatalog.Get(7).AxisType);
        Assert.Equal(BeckhoffAxisType.Linear, BeckhoffAxisCatalog.Get(8).AxisType);
        Assert.Equal(BeckhoffAxisType.Linear, BeckhoffAxisCatalog.Get(9).AxisType);
    }

    /// <summary>
    /// 验证 7 轴按直线轴执行定位时，会写入目标位置并产生启动脉冲。
    /// </summary>
    [Fact]
    public async Task MoveLinearAxisAsync_Axis7_WritesPositionAndTrigger()
    {
        var fakeAds = new FakeAdsSymbolClient();
        var hal = CreateHal(fakeAds);

        await hal.ConnectAsync();
        await hal.MoveLinearAxisAsync(
            axisNo: 7,
            command: new BeckhoffLinearMoveCommand(
                Target: Length.FromMillimeters(12.5),
                Velocity: Speed.FromMillimetersPerSecond(60),
                Acceleration: Acceleration.FromMillimetersPerSecondSquared(500),
                Deceleration: Acceleration.FromMillimetersPerSecondSquared(400),
                Jerk: Jerk.FromMillimetersPerSecondCubed(2000),
                Relative: false));

        Assert.Contains(fakeAds.Writes, x => x.SymbolPath == "Com_GVLS.arstAxis[7].Pos.Par.rPos" && Equals(x.Value, 12.5f));
        Assert.Contains(fakeAds.Writes, x => x.SymbolPath == "Com_GVLS.arstAxis[7].Pos.Ctrl.bGo_A" && Equals(x.Value, true));
        Assert.Contains(fakeAds.Writes, x => x.SymbolPath == "Com_GVLS.arstAxis[7].Pos.Ctrl.bGo_A" && Equals(x.Value, false));
    }

    /// <summary>
    /// 验证旋转运动接口的轴类型保护逻辑：
    /// 7 轴应拒绝旋转命令，3 轴应允许旋转命令。
    /// </summary>
    [Fact]
    public async Task MoveRotaryAxisAsync_AxisTypeGuard_IsEnforced()
    {
        var fakeAds = new FakeAdsSymbolClient();
        var hal = CreateHal(fakeAds);
        await hal.ConnectAsync();

        var command = new BeckhoffRotaryMoveCommand(
            Target: Angle.FromDegrees(90),
            Velocity: RotationalSpeed.FromDegreesPerSecond(45),
            Acceleration: RotationalAcceleration.FromDegreesPerSecondSquared(200),
            Deceleration: RotationalAcceleration.FromDegreesPerSecondSquared(150),
            JerkDegreesPerSecondCubed: 2000,
            Relative: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => hal.MoveRotaryAxisAsync(7, command));

        await hal.MoveRotaryAxisAsync(3, command);
        Assert.Contains(fakeAds.Writes, x => x.SymbolPath == "Com_GVLS.arstAxis[3].Pos.Par.rPos" && Equals(x.Value, 90f));
    }

    /// <summary>
    /// 验证旋转轴状态读取会使用 <c>rActModuloPos</c> 字段并转换为角度单位。
    /// </summary>
    [Fact]
    public async Task ReadAxisStatusAsync_RotaryAxis_UsesModuloPosition()
    {
        var fakeAds = new FakeAdsSymbolClient();
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.bPowerOK"] = true;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.bHome_OK"] = true;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.bMoving"] = false;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.bHasJob"] = false;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.bError"] = false;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.bWarning"] = false;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.udiNCErrorID"] = 0u;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.uiDriveErrorCode"] = (ushort)0;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.rActModuloPos"] = 90f;
        fakeAds.Reads["Com_GVLS.arstAxis[3].State.rActVel"] = 30f;

        var hal = CreateHal(fakeAds);
        await hal.ConnectAsync();
        var status = await hal.ReadAxisStatusAsync(3);

        Assert.NotNull(status.RotaryPosition);
        Assert.NotNull(status.RotaryVelocity);
        Assert.Null(status.LinearPosition);
        Assert.Null(status.LinearVelocity);
        Assert.Equal(90, status.RotaryPosition!.Value.Degrees, 6);
        Assert.Equal(30, status.RotaryVelocity!.Value.DegreesPerSecond, 6);
    }

    /// <summary>
    /// 构造可测试 HAL 实例。
    /// </summary>
    /// <remarks>
    /// 通过注入 fake 工厂实现完全离线测试，避免 SDK 或网络对单测稳定性的影响。
    /// </remarks>
    private static BeckhoffHalClient CreateHal(FakeAdsSymbolClient fakeAds)
    {
        var options = new BeckhoffConnectionOptions
        {
            AmsNetId = "127.0.0.1.1.1",
            AmsPort = 851,
            CommandTimeout = TimeSpan.FromSeconds(1)
        };

        return new BeckhoffHalClient(options, new FakeAdsSymbolClientFactory(fakeAds));
    }

    /// <summary>
    /// 记录一次写入行为的快照。
    /// </summary>
    private sealed record WriteEntry(string SymbolPath, object? Value);

    /// <summary>
    /// Fake ADS 客户端。
    /// </summary>
    /// <remarks>
    /// 使用内存字典模拟“符号读取”，使用列表记录“符号写入”，用于断言 HAL 的读写行为。
    /// </remarks>
    private sealed class FakeAdsSymbolClient : IAdsSymbolClient
    {
        /// <summary>
        /// 读取数据源：键为符号路径，值为预设返回值。
        /// </summary>
        public Dictionary<string, object?> Reads { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// 写入日志：用于断言调用顺序与写入值。
        /// </summary>
        public List<WriteEntry> Writes { get; } = [];

        /// <summary>
        /// 当前连接状态。
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 模拟建立连接。
        /// </summary>
        public Task ConnectAsync(string amsNetId, int amsPort, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            IsConnected = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 按符号路径读取预设值，并尽量转换到目标类型。
        /// </summary>
        public Task<T> ReadValueAsync<T>(string symbolPath, CancellationToken ct)
            where T : notnull
        {
            ct.ThrowIfCancellationRequested();

            if (!Reads.TryGetValue(symbolPath, out var raw))
            {
                throw new KeyNotFoundException($"Missing fake read value for symbol: {symbolPath}");
            }

            if (raw is T typed)
            {
                return Task.FromResult(typed);
            }

            var converted = (T)Convert.ChangeType(raw!, typeof(T));
            return Task.FromResult(converted);
        }

        /// <summary>
        /// 记录写入操作。
        /// </summary>
        public Task WriteValueAsync<T>(string symbolPath, T value, CancellationToken ct)
            where T : notnull
        {
            ct.ThrowIfCancellationRequested();
            Writes.Add(new WriteEntry(symbolPath, value));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 释放 fake 资源（无实际资源占用）。
        /// </summary>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Fake 工厂：始终返回同一个 fake 客户端实例。
    /// </summary>
    private sealed class FakeAdsSymbolClientFactory : IAdsSymbolClientFactory
    {
        private readonly IAdsSymbolClient _client;

        /// <summary>
        /// 创建 fake 工厂。
        /// </summary>
        public FakeAdsSymbolClientFactory(IAdsSymbolClient client)
        {
            _client = client;
        }

        /// <summary>
        /// 返回注入的 fake 客户端。
        /// </summary>
        public IAdsSymbolClient Create() => _client;
    }
}
