using DispensingPlatform.Hal.Contracts.Program;

namespace DispensingPlatform.Hal.Beckhoff.PlcVariables;

/// <summary>
/// 原生程序通道 PLC 变量名映射。
/// </summary>
/// <remarks>
/// 当前 Beckhoff 实现映射到 <c>Com_GVLS.stIJP</c> 结构。
/// </remarks>
internal static class NciVariableMap
{
    /// <summary>
    /// 构造原生程序字段路径。
    /// </summary>
    public static string Field(string field)
        => $"Com_GVLS.stIJP.{field}";

    /// <summary>
    /// 将通用原生命令映射到 Beckhoff PLC 命令位。
    /// </summary>
    public static string Command(NativeProgramCommand command)
        => command switch
        {
            NativeProgramCommand.BuildGroup => Field("bBuildGroupExe"),
            NativeProgramCommand.ClearGroup => Field("bClearGroupExe"),
            NativeProgramCommand.LoadProgram => Field("bLoadPGMExe"),
            NativeProgramCommand.Start => Field("bStartExe"),
            NativeProgramCommand.Stop => Field("bStopExe"),
            NativeProgramCommand.EmergencyStop => Field("bEstopExe"),
            NativeProgramCommand.ContinueAfterEmergencyStop => Field("bAfterEstopExe"),
            NativeProgramCommand.Reset => Field("bResetExe"),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "未知的原生命令。")
        };
}
