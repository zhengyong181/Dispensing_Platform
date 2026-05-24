namespace DispensingPlatform.Hal.Beckhoff.PlcVariables;

/// <summary>
/// IO 与模拟量 PLC 变量名映射。
/// </summary>
/// <remarks>
/// 数字 IO 对应 PLC 的 <c>GVL_IO</c>，模拟量对应 <c>Com_GVLS.stAIO</c>。
/// 如果现场新增 IO 点或模拟量通道，优先更新本文件和 PLC 变量清单。
/// </remarks>
internal static class IoVariableMap
{
    /// <summary>
    /// 构造数字输入/输出变量路径。
    /// </summary>
    public static string Digital(string name)
        => $"GVL_IO.{name}";

    /// <summary>
    /// 构造模拟量输入变量路径。
    /// </summary>
    public static string AnalogInput(int channel)
        => $"Com_GVLS.stAIO.rAI{channel}";

    /// <summary>
    /// 构造模拟量输出变量路径。
    /// </summary>
    public static string AnalogOutput(int channel)
        => $"Com_GVLS.stAIO.rAO{channel}";
}
