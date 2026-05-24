namespace DispensingPlatform.Hal.Contracts.Io;

/// <summary>
/// 模拟量 IO 控制契约。
/// </summary>
/// <remarks>
/// 当前接口使用原始值，便于兼容不同 PLC/模块的量程配置。
/// 工程量换算应在设备服务层统一处理，避免同一通道出现多处换算逻辑。
/// </remarks>
public interface IAnalogIoController
{
    /// <summary>
    /// 读取模拟量输入原始值。
    /// </summary>
    /// <param name="channel">通道号。</param>
    /// <param name="ct">取消令牌。</param>
    Task<double> ReadAnalogInputRawAsync(int channel, CancellationToken ct = default);

    /// <summary>
    /// 写入模拟量输出原始值。
    /// </summary>
    /// <param name="channel">通道号。</param>
    /// <param name="rawValue">原始值。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteAnalogOutputRawAsync(int channel, double rawValue, CancellationToken ct = default);
}
