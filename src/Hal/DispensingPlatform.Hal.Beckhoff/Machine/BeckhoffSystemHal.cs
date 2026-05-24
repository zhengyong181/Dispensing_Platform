using DispensingPlatform.Hal.Beckhoff.Connection;
using DispensingPlatform.Hal.Beckhoff.PlcVariables;
using DispensingPlatform.Hal.Contracts.Machine;

namespace DispensingPlatform.Hal.Beckhoff.Machine;

/// <summary>
/// Beckhoff 整机状态模块。
/// </summary>
/// <remarks>
/// 本模块只读取整机状态，不负责状态变更。
/// 会改变设备状态的业务流程应由 Application 状态机或设备服务编排。
/// </remarks>
internal sealed class BeckhoffSystemHal
{
    private readonly BeckhoffPlcAccessor _plc;

    /// <summary>
    /// 创建整机状态模块。
    /// </summary>
    public BeckhoffSystemHal(BeckhoffPlcAccessor plc)
    {
        _plc = plc ?? throw new ArgumentNullException(nameof(plc));
    }

    /// <summary>
    /// 读取整机状态快照。
    /// </summary>
    public async Task<MachineStatus> ReadStatusAsync(CancellationToken ct)
    {
        // 并行读取状态字段，降低轮询读取的整体延迟。
        var manualModeTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bManual"), ct);
        var resetTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bReset"), ct);
        var stopTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bStop"), ct);
        var emgTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bEMG"), ct);
        var safetyDoorTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bSafetyDoor"), ct);
        var axesPowerOkTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bAxesPowerOK"), ct);
        var axesHomeOkTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bAxesHomeOK"), ct);
        var hasErrorTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bError"), ct);
        var hasWarningTask = _plc.ReadAsync<bool>(SystemVariableMap.Field("bWarning"), ct);

        await Task.WhenAll(
            manualModeTask,
            resetTask,
            stopTask,
            emgTask,
            safetyDoorTask,
            axesPowerOkTask,
            axesHomeOkTask,
            hasErrorTask,
            hasWarningTask).ConfigureAwait(false);

        return new MachineStatus(
            ManualMode: manualModeTask.Result,
            ResetRequested: resetTask.Result,
            StopRequested: stopTask.Result,
            EmergencyStop: emgTask.Result,
            SafetyDoorActive: safetyDoorTask.Result,
            AxesPowerOk: axesPowerOkTask.Result,
            AxesHomeOk: axesHomeOkTask.Result,
            HasError: hasErrorTask.Result,
            HasWarning: hasWarningTask.Result,
            Timestamp: DateTimeOffset.UtcNow);
    }
}
