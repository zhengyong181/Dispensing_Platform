using DispensingPlatform.Hal.Beckhoff.Connection;
using DispensingPlatform.Hal.Beckhoff.PlcVariables;

namespace DispensingPlatform.Hal.Beckhoff.Dispense;

/// <summary>
/// Beckhoff 点胶触发 HAL 模块。
/// </summary>
/// <remarks>
/// 当前 PLC 中阀触发和高压触发位位于 <c>Com_GVLS.stIJP</c>。虽然变量在 NCI 结构里，
/// 但从维护视角看它们属于点胶动作，因此单独拆到本模块。
/// </remarks>
internal sealed class BeckhoffDispenseHal
{
    private readonly BeckhoffPlcAccessor _plc;

    /// <summary>
    /// 创建点胶触发 HAL 模块。
    /// </summary>
    public BeckhoffDispenseHal(BeckhoffPlcAccessor plc)
    {
        _plc = plc ?? throw new ArgumentNullException(nameof(plc));
    }

    /// <summary>
    /// 设置阀触发和高压触发。
    /// </summary>
    /// <param name="valve">是否打开阀触发位。</param>
    /// <param name="highVoltage">是否打开高压触发位。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task SetTriggerAsync(bool valve, bool highVoltage, CancellationToken ct)
    {
        await _plc.WriteAsync(DispenseVariableMap.ValveTrigger, valve, ct).ConfigureAwait(false);
        await _plc.WriteAsync(DispenseVariableMap.HighVoltageTrigger, highVoltage, ct).ConfigureAwait(false);
    }
}
