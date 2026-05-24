using DispensingPlatform.Hal.Beckhoff.Models;

namespace DispensingPlatform.Hal.Beckhoff;

/// <summary>
/// PLC 符号路径构造器。
/// </summary>
/// <remarks>
/// 所有路径均来源于用户提供的 PLC 工程（<c>Com_GVLS</c>、<c>GVL_IO</c> 等），
/// 统一收口可以避免业务代码散落硬编码字符串。
/// </remarks>
internal static class BeckhoffPlcSymbols
{
    /// <summary>
    /// 构造轴控制字段路径。
    /// </summary>
    public static string AxisControl(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].Control.{field}";

    /// <summary>
    /// 构造轴参数字段路径。
    /// </summary>
    public static string AxisParameter(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].Parameter.{field}";

    /// <summary>
    /// 构造轴状态字段路径。
    /// </summary>
    public static string AxisState(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].State.{field}";

    /// <summary>
    /// 构造轴定位控制字段路径。
    /// </summary>
    public static string AxisPositionControl(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].Pos.Ctrl.{field}";

    /// <summary>
    /// 构造轴定位参数字段路径。
    /// </summary>
    public static string AxisPositionParameter(int axisNo, string field)
        => $"Com_GVLS.arstAxis[{axisNo}].Pos.Par.{field}";

    /// <summary>
    /// 构造系统状态字段路径。
    /// </summary>
    public static string System(string field)
        => $"Com_GVLS.stSystem.{field}";

    /// <summary>
    /// 构造 NCI 状态字段路径。
    /// </summary>
    public static string Nci(string field)
        => $"Com_GVLS.stIJP.{field}";

    /// <summary>
    /// 构造数字 IO 字段路径。
    /// </summary>
    public static string DigitalIo(string name)
        => $"GVL_IO.{name}";

    /// <summary>
    /// 构造模拟量输入字段路径。
    /// </summary>
    public static string AnalogInput(int channel)
        => $"Com_GVLS.stAIO.rAI{channel}";

    /// <summary>
    /// 构造模拟量输出字段路径。
    /// </summary>
    public static string AnalogOutput(int channel)
        => $"Com_GVLS.stAIO.rAO{channel}";

    /// <summary>
    /// 根据轴类型选择位置反馈字段。
    /// </summary>
    /// <remarks>
    /// 旋转轴读取 <c>rActModuloPos</c>，直线轴读取 <c>rActPos</c>。
    /// </remarks>
    public static string AxisPositionFeedbackSymbol(BeckhoffAxisDefinition axis)
        => axis.AxisType == BeckhoffAxisType.Rotary
            ? AxisState(axis.AxisNo, "rActModuloPos")
            : AxisState(axis.AxisNo, "rActPos");

    /// <summary>
    /// 将 NCI 命令映射到 PLC 布尔触发位。
    /// </summary>
    public static string NciCommand(BeckhoffNciCommand command)
        => command switch
        {
            BeckhoffNciCommand.BuildGroup => Nci("bBuildGroupExe"),
            BeckhoffNciCommand.ClearGroup => Nci("bClearGroupExe"),
            BeckhoffNciCommand.LoadProgram => Nci("bLoadPGMExe"),
            BeckhoffNciCommand.Start => Nci("bStartExe"),
            BeckhoffNciCommand.Stop => Nci("bStopExe"),
            BeckhoffNciCommand.EmergencyStop => Nci("bEstopExe"),
            BeckhoffNciCommand.ContinueAfterEmergencyStop => Nci("bAfterEstopExe"),
            BeckhoffNciCommand.Reset => Nci("bResetExe"),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
}
