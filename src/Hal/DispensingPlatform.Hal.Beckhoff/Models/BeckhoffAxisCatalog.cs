namespace DispensingPlatform.Hal.Beckhoff.Models;

/// <summary>
/// Beckhoff 轴目录。
/// </summary>
/// <remarks>
/// 该目录固化了当前机型轴配置，是 HAL 进行轴类型判断、符号映射与运行时参数校验的基础。
/// </remarks>
public static class BeckhoffAxisCatalog
{
    // 轴定义按 PLC 序号顺序排列，便于通过 axisNo-1 快速索引。
    private static readonly IReadOnlyList<BeckhoffAxisDefinition> Definitions =
    [
        new(1, "Axis1", "喷涂X轴", BeckhoffAxisType.Linear),
        new(2, "Axis2", "喷涂Y轴", BeckhoffAxisType.Linear),
        new(3, "Axis3", "喷涂R轴", BeckhoffAxisType.Rotary),
        new(4, "Axis4", "喷涂Z1轴", BeckhoffAxisType.Linear),
        new(5, "Axis5", "喷涂Z2轴", BeckhoffAxisType.Linear),
        new(6, "Axis6", "顶pin轴", BeckhoffAxisType.Linear),
        new(7, "Axis7", "R1轴", BeckhoffAxisType.Linear),
        new(8, "Axis8", "R2轴", BeckhoffAxisType.Linear),
        new(9, "Axis9", "R3轴", BeckhoffAxisType.Linear),
        new(10, "Axis10", "Bump轴", BeckhoffAxisType.Linear)
    ];

    /// <summary>
    /// 获取全部轴定义。
    /// </summary>
    public static IReadOnlyList<BeckhoffAxisDefinition> All => Definitions;

    /// <summary>
    /// 根据轴序号获取轴定义。
    /// </summary>
    /// <param name="axisNo">轴序号，合法范围 1..10。</param>
    /// <returns>对应的轴定义。</returns>
    /// <exception cref="ArgumentOutOfRangeException">轴序号超出范围时抛出。</exception>
    public static BeckhoffAxisDefinition Get(int axisNo)
    {
        if (axisNo is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(axisNo), axisNo, "Axis number must be in [1,10].");
        }

        return Definitions[axisNo - 1];
    }
}
