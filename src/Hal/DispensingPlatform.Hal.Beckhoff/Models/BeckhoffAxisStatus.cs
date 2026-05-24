using UnitsNet;

namespace DispensingPlatform.Hal.Beckhoff.Models;

/// <summary>
/// 轴状态快照。
/// </summary>
/// <remarks>
/// 为保证类型安全，直线轴与旋转轴的状态量分开承载：
/// <list type="bullet">
/// <item><description>直线轴填充 <see cref="LinearPosition"/> 与 <see cref="LinearVelocity"/>。</description></item>
/// <item><description>旋转轴填充 <see cref="RotaryPosition"/> 与 <see cref="RotaryVelocity"/>。</description></item>
/// </list>
/// 未使用的一组字段保持 <c>null</c>。
/// </remarks>
public sealed record BeckhoffAxisStatus(
    BeckhoffAxisDefinition Axis,
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
