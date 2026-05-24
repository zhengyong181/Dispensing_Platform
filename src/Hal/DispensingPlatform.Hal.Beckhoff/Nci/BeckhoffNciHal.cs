using DispensingPlatform.Hal.Beckhoff.Connection;
using DispensingPlatform.Hal.Beckhoff.PlcVariables;
using DispensingPlatform.Hal.Contracts.Program;

namespace DispensingPlatform.Hal.Beckhoff.Nci;

/// <summary>
/// Beckhoff 原生程序通道模块。
/// </summary>
/// <remarks>
/// 本模块只负责命令位写入，程序生命周期状态机应放在上层执行适配或 Application 层。
/// </remarks>
internal sealed class BeckhoffNciHal
{
    private readonly BeckhoffPlcAccessor _plc;

    /// <summary>
    /// 创建原生程序通道模块。
    /// </summary>
    public BeckhoffNciHal(BeckhoffPlcAccessor plc)
    {
        _plc = plc ?? throw new ArgumentNullException(nameof(plc));
    }

    /// <summary>
    /// 设置原生命令位。
    /// </summary>
    public async Task SetCommandAsync(NativeProgramCommand command, bool value, CancellationToken ct)
    {
        await _plc.WriteAsync(NciVariableMap.Command(command), value, ct).ConfigureAwait(false);
    }
}
