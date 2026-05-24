using UnitsNet;

namespace DispensingPlatform.Hal.Contracts.Axes;

/// <summary>
/// 旋转轴定位命令。
/// </summary>
/// <param name="Target">目标角度。</param>
/// <param name="Velocity">目标角速度。</param>
/// <param name="Acceleration">目标角加速度。</param>
/// <param name="Deceleration">目标角减速度。</param>
/// <param name="JerkDegreesPerSecondCubed">目标角跃度，单位固定为度每秒三次方。</param>
/// <param name="Relative">是否相对定位；<c>false</c> 表示绝对定位。</param>
public sealed record RotaryAxisMoveCommand(
    Angle Target,
    RotationalSpeed Velocity,
    RotationalAcceleration Acceleration,
    RotationalAcceleration Deceleration,
    double JerkDegreesPerSecondCubed,
    bool Relative = false);
