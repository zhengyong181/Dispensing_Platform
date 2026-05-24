namespace DispensingPlatform.Hal.Contracts.Machine;

/// <summary>
/// 整机状态快照。
/// </summary>
/// <remarks>
/// 该模型表达“读取时刻的设备状态”，不表达动作意图。
/// 恢复与联锁判断应在 Application 或安全控制层结合历史与策略处理。
/// </remarks>
public sealed record MachineStatus(
    bool ManualMode,
    bool ResetRequested,
    bool StopRequested,
    bool EmergencyStop,
    bool SafetyDoorActive,
    bool AxesPowerOk,
    bool AxesHomeOk,
    bool HasError,
    bool HasWarning,
    DateTimeOffset Timestamp);
