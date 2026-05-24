using UnitsNet;

namespace DispensingPlatform.Hal.Contracts.Axes;

/// <summary>
/// 单轴状态快照。
/// </summary>
/// <remarks>
/// 直线轴只填充 <see cref="LinearPosition"/> / <see cref="LinearVelocity"/>；
/// 旋转轴只填充 <see cref="RotaryPosition"/> / <see cref="RotaryVelocity"/>。
/// 当控制器字段缺失或不适用时，对应可空字段应返回 <c>null</c>，避免伪造默认值掩盖异常。
/// </remarks>
public sealed record AxisStatus(
    AxisDescriptor Axis,
    bool IsEnabled,
    bool IsHomed,
    bool IsMoving,
    bool HasJob,
    bool HasError,
    bool HasWarning,
    uint NcErrorCode,
    ushort DriveErrorCode,
    Length? LinearPosition,
    Speed? LinearVelocity,
    Angle? RotaryPosition,
    RotationalSpeed? RotaryVelocity,
    DateTimeOffset Timestamp);
