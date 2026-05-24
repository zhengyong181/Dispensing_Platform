using DispensingPlatform.Hal.Beckhoff;
using DispensingPlatform.Hal.Beckhoff.Axes;
using DispensingPlatform.Hal.Beckhoff.Connection;
using DispensingPlatform.Hal.Contracts.Axes;
using UnitsNet;
using Xunit;

namespace DispensingPlatform.Hal.Tests;

/// <summary>
/// Beckhoff HAL 行为测试。
/// </summary>
/// <remarks>
/// 测试目标：
/// <list type="bullet">
/// <item><description>验证轴类型约束，当前只有 Axis3 是旋转轴。</description></item>
/// <item><description>验证定位命令写入 PLC 符号路径与触发顺序。</description></item>
/// <item><description>验证旋转轴状态读取使用模轴位置字段并转换为角度单位。</description></item>
/// </list>
/// 所有测试均通过 ADS 假客户端执行，不依赖真实 PLC 或现场网络。
/// </remarks>
public sealed class BeckhoffHalTests
{
    /// <summary>
    /// 验证轴目录中的旋转轴规则与约束一致。
    /// </summary>
    [Fact]
    public void AxisCatalog_UsesExpectedRotaryRules()
    {
        Assert.Equal(AxisKind.Rotary, BeckhoffAxisCatalog.GetByAxisId("Axis3").Kind);
        Assert.Equal(AxisKind.Linear, BeckhoffAxisCatalog.GetByAxisId("Axis7").Kind);
        Assert.Equal(AxisKind.Linear, BeckhoffAxisCatalog.GetByAxisId("Axis8").Kind);
        Assert.Equal(AxisKind.Linear, BeckhoffAxisCatalog.GetByAxisId("Axis9").Kind);
    }

    /// <summary>
    /// 验证 Axis7 执行直线定位时，会写入目标位置并产生启动脉冲。
    /// </summary>
    [Fact]
    public async Task MoveLinearAxisAsync_Axis7_WritesPositionAndTrigger()
    {
        var fakeAds = new FakeAdsSymbolClient();
        var hal = CreateHal(fakeAds);

        await hal.ConnectAsync();
        await hal.MoveLinearAxisAsync(
            axisId: "Axis7",
            command: new LinearAxisMoveCommand(
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
    /// 验证旋转定位接口的轴类型保护逻辑：Axis7 拒绝旋转命令，Axis3 允许旋转命令。
    /// </summary>
    [Fact]
    public async Task MoveRotaryAxisAsync_AxisTypeGuard_IsEnforced()
    {
        var fakeAds = new FakeAdsSymbolClient();
        var hal = CreateHal(fakeAds);
        await hal.ConnectAsync();

        var command = new RotaryAxisMoveCommand(
            Target: Angle.FromDegrees(90),
            Velocity: RotationalSpeed.FromDegreesPerSecond(45),
            Acceleration: RotationalAcceleration.FromDegreesPerSecondSquared(200),
            Deceleration: RotationalAcceleration.FromDegreesPerSecondSquared(150),
            JerkDegreesPerSecondCubed: 2000,
            Relative: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => hal.MoveRotaryAxisAsync("Axis7", command));

        await hal.MoveRotaryAxisAsync("Axis3", command);
        Assert.Contains(fakeAds.Writes, x => x.SymbolPath == "Com_GVLS.arstAxis[3].Pos.Par.rPos" && Equals(x.Value, 90f));
    }

    /// <summary>
    /// 验证旋转轴状态读取会使用 <c>rActModuloPos</c> 字段，并转换为角度单位。
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
        var status = await hal.ReadAxisStatusAsync("Axis3");

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
    /// 通过注入 ADS 假客户端工厂实现完全离线测试，避免 SDK 或网络影响单元测试稳定性。
    /// </remarks>
    private static BeckhoffHal CreateHal(FakeAdsSymbolClient fakeAds)
    {
        var options = new BeckhoffConnectionOptions
        {
            AmsNetId = "127.0.0.1.1.1",
            AmsPort = 851,
            CommandTimeout = TimeSpan.FromSeconds(1)
        };

        return new BeckhoffHal(options, new FakeAdsSymbolClientFactory(fakeAds));
    }

    /// <summary>
    /// 记录一次写入行为快照。
    /// </summary>
    private sealed record WriteEntry(string SymbolPath, object? Value);

    /// <summary>
    /// ADS 假客户端。
    /// </summary>
    /// <remarks>
    /// 使用内存字典模拟符号读取，使用列表记录符号写入，用于断言 HAL 读写行为。
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
                throw new KeyNotFoundException($"测试未设置 PLC 变量“{symbolPath}”的读取值。");
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
        /// 释放假客户端资源；当前无实际资源占用。
        /// </summary>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// ADS 假客户端工厂，始终返回同一个实例。
    /// </summary>
    private sealed class FakeAdsSymbolClientFactory : IAdsSymbolClientFactory
    {
        private readonly IAdsSymbolClient _client;

        /// <summary>
        /// 创建假客户端工厂。
        /// </summary>
        public FakeAdsSymbolClientFactory(IAdsSymbolClient client)
        {
            _client = client;
        }

        /// <summary>
        /// 返回注入的假客户端。
        /// </summary>
        public IAdsSymbolClient Create() => _client;
    }
}
