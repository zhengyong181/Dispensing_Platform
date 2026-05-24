using DispensingPlatform.Hal.Beckhoff.Models;

namespace DispensingPlatform.Hal.Beckhoff;

/// <summary>
/// Beckhoff HAL 对外能力接口。
/// </summary>
/// <remarks>
/// 该接口只暴露硬件抽象能力，不泄露 TwinCAT SDK 细节，供上层 Application/Service 安全调用。
/// </remarks>
public interface IBeckhoffHal : IAsyncDisposable
{
    /// <summary>
    /// 当前连接参数快照。
    /// </summary>
    BeckhoffConnectionOptions Connection { get; }

    /// <summary>
    /// 可用轴定义集合。
    /// </summary>
    IReadOnlyList<BeckhoffAxisDefinition> Axes { get; }

    /// <summary>
    /// 建立 ADS 连接。
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// 读取系统状态快照。
    /// </summary>
    Task<BeckhoffSystemStatus> ReadSystemStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// 读取单轴状态快照。
    /// </summary>
    Task<BeckhoffAxisStatus> ReadAxisStatusAsync(int axisNo, CancellationToken ct = default);

    /// <summary>
    /// 设置轴使能状态。
    /// </summary>
    Task SetAxisPowerAsync(int axisNo, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// 触发轴回零。
    /// </summary>
    Task HomeAxisAsync(int axisNo, CancellationToken ct = default);

    /// <summary>
    /// 触发轴停止。
    /// </summary>
    Task StopAxisAsync(int axisNo, CancellationToken ct = default);

    /// <summary>
    /// 触发轴复位。
    /// </summary>
    Task ResetAxisAsync(int axisNo, CancellationToken ct = default);

    /// <summary>
    /// 执行直线轴定位运动。
    /// </summary>
    Task MoveLinearAxisAsync(int axisNo, BeckhoffLinearMoveCommand command, CancellationToken ct = default);

    /// <summary>
    /// 执行旋转轴定位运动。
    /// </summary>
    Task MoveRotaryAxisAsync(int axisNo, BeckhoffRotaryMoveCommand command, CancellationToken ct = default);

    /// <summary>
    /// 设置 NCI 控制命令位。
    /// </summary>
    Task SetNciCommandAsync(BeckhoffNciCommand command, bool value, CancellationToken ct = default);

    /// <summary>
    /// 写入数字输出点。
    /// </summary>
    Task SetDigitalOutputAsync(string outputName, bool value, CancellationToken ct = default);

    /// <summary>
    /// 读取数字输入点。
    /// </summary>
    Task<bool> ReadDigitalInputAsync(string inputName, CancellationToken ct = default);

    /// <summary>
    /// 读取模拟量输入原始值。
    /// </summary>
    Task<double> ReadAnalogInputRawAsync(int channel, CancellationToken ct = default);

    /// <summary>
    /// 写入模拟量输出原始值。
    /// </summary>
    Task WriteAnalogOutputRawAsync(int channel, double rawValue, CancellationToken ct = default);

    /// <summary>
    /// 设置点胶触发位（阀/高压）。
    /// </summary>
    Task SetDispenseTriggerAsync(bool valve, bool highVoltage, CancellationToken ct = default);
}
