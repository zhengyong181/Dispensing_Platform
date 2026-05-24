using UnitsNet;

namespace DispensingPlatform.Hal.Beckhoff.Models;

/// <summary>
/// 直线轴定位命令。
/// </summary>
/// <param name="Target">目标位置（毫米系）。</param>
/// <param name="Velocity">目标速度。</param>
/// <param name="Acceleration">目标加速度。</param>
/// <param name="Deceleration">目标减速度。</param>
/// <param name="Jerk">目标跃度。</param>
/// <param name="Relative">是否按相对位移执行；<c>false</c> 为绝对定位。</param>
public sealed record BeckhoffLinearMoveCommand(
    Length Target,
    Speed Velocity,
    Acceleration Acceleration,
    Acceleration Deceleration,
    Jerk Jerk,
    bool Relative = false);
