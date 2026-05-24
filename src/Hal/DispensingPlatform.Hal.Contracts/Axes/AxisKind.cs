namespace DispensingPlatform.Hal.Contracts.Axes;

/// <summary>
/// 轴类型。
/// </summary>
/// <remarks>
/// 该类型用于约束位置与速度单位语义，避免把角度轴当直线轴处理。
/// </remarks>
public enum AxisKind
{
    /// <summary>
    /// 直线轴，位置单位为长度。
    /// </summary>
    Linear = 0,

    /// <summary>
    /// 旋转轴，位置单位为角度。
    /// </summary>
    Rotary = 1
}
