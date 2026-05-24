using UnitsNet;

namespace DispensingPlatform.Hal.Contracts.Axes;

/// <summary>
/// 直线轴定位命令。
/// </summary>
/// <param name="Target">目标位置。</param>
/// <param name="Velocity">目标速度。</param>
/// <param name="Acceleration">目标加速度。</param>
/// <param name="Deceleration">目标减速度。</param>
/// <param name="Jerk">目标跃度。</param>
/// <param name="Relative">是否相对定位；<c>false</c> 表示绝对定位。</param>
public sealed record LinearAxisMoveCommand(
    Length Target,
    Speed Velocity,
    Acceleration Acceleration,
    Acceleration Deceleration,
    Jerk Jerk,
    bool Relative = false);
