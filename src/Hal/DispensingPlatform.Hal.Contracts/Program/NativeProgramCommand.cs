namespace DispensingPlatform.Hal.Contracts.Program;

/// <summary>
/// 控制器原生命令类型。
/// </summary>
/// <remarks>
/// 该枚举用于表达脚本/程序通道的基础控制意图，不直接绑定某家控制器命名。
/// 厂家实现应在内部完成枚举到具体 PLC 变量或 SDK 命令的映射。
/// </remarks>
public enum NativeProgramCommand
{
    /// <summary>
    /// 构建执行组。
    /// </summary>
    BuildGroup,

    /// <summary>
    /// 清理执行组。
    /// </summary>
    ClearGroup,

    /// <summary>
    /// 加载程序。
    /// </summary>
    LoadProgram,

    /// <summary>
    /// 启动程序。
    /// </summary>
    Start,

    /// <summary>
    /// 停止程序。
    /// </summary>
    Stop,

    /// <summary>
    /// 紧急停止程序。
    /// </summary>
    EmergencyStop,

    /// <summary>
    /// 紧急停止后继续执行。
    /// </summary>
    ContinueAfterEmergencyStop,

    /// <summary>
    /// 复位程序通道错误。
    /// </summary>
    Reset
}
