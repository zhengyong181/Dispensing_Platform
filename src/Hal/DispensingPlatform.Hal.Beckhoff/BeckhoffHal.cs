using DispensingPlatform.Hal.Beckhoff.Axes;
using DispensingPlatform.Hal.Beckhoff.Connection;
using DispensingPlatform.Hal.Beckhoff.Dispense;
using DispensingPlatform.Hal.Beckhoff.Io;
using DispensingPlatform.Hal.Beckhoff.Machine;
using DispensingPlatform.Hal.Beckhoff.Nci;
using DispensingPlatform.Hal.Contracts;
using DispensingPlatform.Hal.Contracts.Axes;
using DispensingPlatform.Hal.Contracts.Dispense;
using DispensingPlatform.Hal.Contracts.Machine;
using DispensingPlatform.Hal.Contracts.Program;

namespace DispensingPlatform.Hal.Beckhoff;

/// <summary>
/// Beckhoff PLC 控制器实现。
/// </summary>
/// <remarks>
/// 该类是 Beckhoff HAL 的总入口，负责把通用 Contracts 接口调用分发到内部子模块。
/// 它不直接操作 PLC 符号，符号读写统一由各子模块通过 <see cref="BeckhoffPlcAccessor"/> 执行。
/// </remarks>
public sealed class BeckhoffHal : IPlcController
{
    private readonly BeckhoffPlcAccessor _plc;
    private readonly BeckhoffAxisHal _axes;
    private readonly BeckhoffSystemHal _system;
    private readonly BeckhoffIoHal _io;
    private readonly BeckhoffNciHal _nci;
    private readonly BeckhoffDispenseHal _dispense;

    /// <summary>
    /// 创建生产环境使用的 Beckhoff 控制器。
    /// </summary>
    /// <param name="options">ADS 连接参数；传空时使用默认参数。</param>
    /// <param name="log">可选日志回调，日志文本为简体中文。</param>
    public BeckhoffHal(
        BeckhoffConnectionOptions? options = null,
        Action<string>? log = null)
        : this(
            options ?? BeckhoffConnectionOptions.Default,
            new TwinCatAdsClientFactory(),
            log)
    {
    }

    /// <summary>
    /// 创建可测试的 Beckhoff 控制器。
    /// </summary>
    /// <param name="options">ADS 连接参数。</param>
    /// <param name="adsFactory">ADS 客户端工厂，测试时可注入假实现。</param>
    /// <param name="log">可选日志回调。</param>
    internal BeckhoffHal(
        BeckhoffConnectionOptions options,
        IAdsSymbolClientFactory adsFactory,
        Action<string>? log = null)
    {
        _plc = new BeckhoffPlcAccessor(options, adsFactory, log);
        _axes = new BeckhoffAxisHal(_plc);
        _system = new BeckhoffSystemHal(_plc);
        _io = new BeckhoffIoHal(_plc);
        _nci = new BeckhoffNciHal(_plc);
        _dispense = new BeckhoffDispenseHal(_plc);
    }

    /// <summary>
    /// 当前连接参数快照。
    /// </summary>
    public BeckhoffConnectionOptions Connection => _plc.Connection;

    /// <inheritdoc />
    public IReadOnlyList<AxisDescriptor> Axes => _axes.Axes;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken ct = default)
        => _plc.EnsureConnectedAsync(ct);

    /// <inheritdoc />
    public Task<MachineStatus> ReadMachineStatusAsync(CancellationToken ct = default)
        => _system.ReadStatusAsync(ct);

    /// <inheritdoc />
    public Task<AxisStatus> ReadAxisStatusAsync(string axisId, CancellationToken ct = default)
        => _axes.ReadStatusAsync(axisId, ct);

    /// <inheritdoc />
    public Task SetAxisPowerAsync(string axisId, bool enabled, CancellationToken ct = default)
        => _axes.SetPowerAsync(axisId, enabled, ct);

    /// <inheritdoc />
    public Task HomeAxisAsync(string axisId, CancellationToken ct = default)
        => _axes.HomeAsync(axisId, ct);

    /// <inheritdoc />
    public Task StopAxisAsync(string axisId, CancellationToken ct = default)
        => _axes.StopAsync(axisId, ct);

    /// <inheritdoc />
    public Task ResetAxisAsync(string axisId, CancellationToken ct = default)
        => _axes.ResetAsync(axisId, ct);

    /// <inheritdoc />
    public Task MoveLinearAxisAsync(string axisId, LinearAxisMoveCommand command, CancellationToken ct = default)
        => _axes.MoveLinearAsync(axisId, command, ct);

    /// <inheritdoc />
    public Task MoveRotaryAxisAsync(string axisId, RotaryAxisMoveCommand command, CancellationToken ct = default)
        => _axes.MoveRotaryAsync(axisId, command, ct);

    /// <inheritdoc />
    public Task SetNativeProgramCommandAsync(NativeProgramCommand command, bool value, CancellationToken ct = default)
        => _nci.SetCommandAsync(command, value, ct);

    /// <inheritdoc />
    public Task SetDigitalOutputAsync(string outputName, bool value, CancellationToken ct = default)
        => _io.SetDigitalOutputAsync(outputName, value, ct);

    /// <inheritdoc />
    public Task<bool> ReadDigitalInputAsync(string inputName, CancellationToken ct = default)
        => _io.ReadDigitalInputAsync(inputName, ct);

    /// <inheritdoc />
    public Task<double> ReadAnalogInputRawAsync(int channel, CancellationToken ct = default)
        => _io.ReadAnalogInputRawAsync(channel, ct);

    /// <inheritdoc />
    public Task WriteAnalogOutputRawAsync(int channel, double rawValue, CancellationToken ct = default)
        => _io.WriteAnalogOutputRawAsync(channel, rawValue, ct);

    /// <inheritdoc />
    public Task SetDispenseTriggerAsync(DispenseTriggerState state, CancellationToken ct = default)
        => _dispense.SetTriggerAsync(state.Valve, state.HighVoltage, ct);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _plc.DisposeAsync();
}
