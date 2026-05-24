using DispensingPlatform.Hal.Contracts.Axes;

namespace DispensingPlatform.Hal.Beckhoff.PlcVariables;

/// <summary>
/// 轴相关 PLC 变量名映射。
/// </summary>
/// <remarks>
/// 如果 PLC 中 <c>Com_GVLS.arstAxis</c> 的结构或字段发生变化，优先修改本文件。
/// 该文件只负责拼接 PLC 符号路径，不处理业务流程。
/// </remarks>
internal static class AxisVariableMap
{
    /// <summary>
    /// 轴控制字段路径，例如使能、回零、停止、复位。
    /// </summary>
    public static string Control(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].Control.{field}";

    /// <summary>
    /// 轴参数字段路径。
    /// </summary>
    public static string Parameter(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].Parameter.{field}";

    /// <summary>
    /// 轴状态字段路径。
    /// </summary>
    public static string State(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].State.{field}";

    /// <summary>
    /// 轴定位控制字段路径。
    /// </summary>
    public static string PositionControl(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].Pos.Ctrl.{field}";

    /// <summary>
    /// 轴定位参数字段路径。
    /// </summary>
    public static string PositionParameter(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].Pos.Par.{field}";

    /// <summary>
    /// 根据轴类型选择实际位置反馈字段。
    /// </summary>
    /// <remarks>
    /// 旋转轴读取模轴位置 <c>rActModuloPos</c>，直线轴读取线性位置 <c>rActPos</c>。
    /// </remarks>
    public static string PositionFeedback(AxisDescriptor axis)
        => axis.Kind == AxisKind.Rotary
            ? State(GetAxisNo(axis), "rActModuloPos")
            : State(GetAxisNo(axis), "rActPos");

    private static int GetAxisNo(AxisDescriptor axis)
    {
        if (axis.ControllerIndex is null)
        {
            throw new InvalidOperationException($"轴 {axis.AxisId} 未配置 PLC 序号映射。");
        }

        return axis.ControllerIndex.Value;
    }
}
