namespace DispensingPlatform.Hal.Beckhoff.Models;

/// <summary>
/// 系统级状态快照。
/// </summary>
/// <remarks>
/// 该状态对象来源于 <c>Com_GVLS.stSystem</c>，用于上层状态机与报警逻辑进行只读判断。
/// </remarks>
public sealed record BeckhoffSystemStatus(
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
