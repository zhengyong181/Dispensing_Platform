namespace DispensingPlatform.Hal.Contracts.Axes;

/// <summary>
/// 轴静态描述。
/// </summary>
/// <param name="AxisId">上位机内部轴标识，供业务层稳定引用。</param>
/// <param name="DisplayName">面向操作员或调试人员的显示名称。</param>
/// <param name="Kind">轴类型，决定运动命令和状态单位。</param>
/// <param name="ControllerIndex">控制器侧索引，供实现层映射 PLC 数组；上层不得依赖该值做业务判断。</param>
public sealed record AxisDescriptor(
    string AxisId,
    string DisplayName,
    AxisKind Kind,
    int? ControllerIndex = null);
