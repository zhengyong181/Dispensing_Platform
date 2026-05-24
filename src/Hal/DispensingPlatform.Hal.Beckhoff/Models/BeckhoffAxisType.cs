namespace DispensingPlatform.Hal.Beckhoff.Models;

/// <summary>
/// 轴类型枚举。
/// </summary>
/// <remarks>
/// 当前设备约束：仅 3 轴是旋转轴，其余轴均按直线轴处理。
/// </remarks>
public enum BeckhoffAxisType
{
    /// <summary>
    /// 直线轴，位置单位通常为毫米。
    /// </summary>
    Linear = 0,

    /// <summary>
    /// 旋转轴，位置单位通常为角度。
    /// </summary>
    Rotary = 1
}
