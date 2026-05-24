using DispensingPlatform.Hal.Contracts.Axes;

namespace DispensingPlatform.Hal.Beckhoff.Axes;

/// <summary>
/// 当前机型轴目录。
/// </summary>
/// <remarks>
/// 该文件是维护轴标识、显示名、PLC 序号和轴类型的唯一入口。
/// 如果 PLC 轴数量或轴语义发生变化，优先修改本文件并同步 PLC 变量清单。
/// </remarks>
public static class BeckhoffAxisCatalog
{
    // 轴定义按 PLC 序号排序，ControllerIndex 对应 PLC 的 arstAxis[index] 下标。
    private static readonly IReadOnlyList<AxisDescriptor> Definitions =
    [
        new("Axis1", "喷涂X轴", AxisKind.Linear, 1),
        new("Axis2", "喷涂Y轴", AxisKind.Linear, 2),
        new("Axis3", "喷涂R轴", AxisKind.Rotary, 3),
        new("Axis4", "喷涂Z1轴", AxisKind.Linear, 4),
        new("Axis5", "喷涂Z2轴", AxisKind.Linear, 5),
        new("Axis6", "顶pin轴", AxisKind.Linear, 6),
        new("Axis7", "R1轴", AxisKind.Linear, 7),
        new("Axis8", "R2轴", AxisKind.Linear, 8),
        new("Axis9", "R3轴", AxisKind.Linear, 9),
        new("Axis10", "顶升轴", AxisKind.Linear, 10)
    ];

    /// <summary>
    /// 全部轴定义。
    /// </summary>
    public static IReadOnlyList<AxisDescriptor> All => Definitions;

    /// <summary>
    /// 根据 PLC 序号获取轴定义。
    /// </summary>
    /// <param name="axisNo">PLC 序号，范围 1..10。</param>
    /// <returns>对应轴定义。</returns>
    public static AxisDescriptor GetByAxisNo(int axisNo)
    {
        if (axisNo is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(axisNo), axisNo, "轴序号必须在 1 到 10 之间。");
        }

        return Definitions[axisNo - 1];
    }

    /// <summary>
    /// 根据轴标识获取轴定义。
    /// </summary>
    /// <param name="axisId">上位机轴标识，例如 Axis3。</param>
    /// <returns>对应轴定义。</returns>
    public static AxisDescriptor GetByAxisId(string axisId)
    {
        if (string.IsNullOrWhiteSpace(axisId))
        {
            throw new ArgumentException("轴标识不能为空。", nameof(axisId));
        }

        var axis = Definitions.FirstOrDefault(
            x => string.Equals(x.AxisId, axisId, StringComparison.OrdinalIgnoreCase));

        if (axis is null)
        {
            throw new ArgumentOutOfRangeException(nameof(axisId), axisId, "未找到对应轴标识。");
        }

        return axis;
    }
}
