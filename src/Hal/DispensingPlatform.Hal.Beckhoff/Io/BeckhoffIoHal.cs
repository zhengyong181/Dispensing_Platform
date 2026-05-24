using System.Text.RegularExpressions;
using DispensingPlatform.Hal.Beckhoff.Connection;
using DispensingPlatform.Hal.Beckhoff.PlcVariables;

namespace DispensingPlatform.Hal.Beckhoff.Io;

/// <summary>
/// Beckhoff IO HAL 模块。
/// </summary>
/// <remarks>
/// 本模块负责数字输入、数字输出、模拟量输入和模拟量输出。它只做 HAL 层读写，
/// 不决定某个 IO 点在业务流程中何时应该打开或关闭。
/// </remarks>
internal sealed class BeckhoffIoHal
{
    // GVL_IO 中的数字输入命名形如 X000001。
    private static readonly Regex DigitalInputNamePattern = new("^X\\d{6}$", RegexOptions.Compiled);

    // GVL_IO 中的数字输出命名形如 Y000001。
    private static readonly Regex DigitalOutputNamePattern = new("^Y\\d{6}$", RegexOptions.Compiled);

    private readonly BeckhoffPlcAccessor _plc;

    /// <summary>
    /// 创建 IO HAL 模块。
    /// </summary>
    public BeckhoffIoHal(BeckhoffPlcAccessor plc)
    {
        _plc = plc ?? throw new ArgumentNullException(nameof(plc));
    }

    /// <summary>
    /// 设置数字输出。
    /// </summary>
    public async Task SetDigitalOutputAsync(string outputName, bool value, CancellationToken ct)
    {
        if (!DigitalOutputNamePattern.IsMatch(outputName))
        {
            throw new ArgumentException("数字输出名称必须符合 Y000001 这样的格式。", nameof(outputName));
        }

        await _plc.WriteAsync(IoVariableMap.Digital(outputName), value, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 读取数字输入。
    /// </summary>
    public async Task<bool> ReadDigitalInputAsync(string inputName, CancellationToken ct)
    {
        if (!DigitalInputNamePattern.IsMatch(inputName))
        {
            throw new ArgumentException("数字输入名称必须符合 X000001 这样的格式。", nameof(inputName));
        }

        return await _plc.ReadAsync<bool>(IoVariableMap.Digital(inputName), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 读取模拟量输入原始值。
    /// </summary>
    public async Task<double> ReadAnalogInputRawAsync(int channel, CancellationToken ct)
    {
        ValidateAnalogChannel(channel);
        return await _plc.ReadAsync<float>(IoVariableMap.AnalogInput(channel), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 写入模拟量输出原始值。
    /// </summary>
    public async Task WriteAnalogOutputRawAsync(int channel, double rawValue, CancellationToken ct)
    {
        ValidateAnalogChannel(channel);
        await _plc.WriteAsync(IoVariableMap.AnalogOutput(channel), (float)rawValue, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 校验模拟量通道号。
    /// </summary>
    /// <remarks>
    /// 当前 PLC 结构定义了 rAI1..rAI20 和 rAO1..rAO16；HAL 暂按 1..20 做读取上限，
    /// 输出通道实际可用范围需要结合后续硬件清单进一步收紧。
    /// </remarks>
    private static void ValidateAnalogChannel(int channel)
    {
        if (channel is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "模拟量通道号必须在 1 到 20 之间。");
        }
    }
}
