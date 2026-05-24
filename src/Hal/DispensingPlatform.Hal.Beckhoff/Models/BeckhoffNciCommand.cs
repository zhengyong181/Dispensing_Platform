namespace DispensingPlatform.Hal.Beckhoff.Models;

/// <summary>
/// NCI 通道控制命令枚举。
/// </summary>
/// <remarks>
/// 每个命令最终都会映射到 <c>Com_GVLS.stIJP</c> 下对应的布尔触发位。
/// </remarks>
public enum BeckhoffNciCommand
{
    /// <summary>构建 NCI 轴组。</summary>
    BuildGroup,
    /// <summary>清理 NCI 轴组。</summary>
    ClearGroup,
    /// <summary>装载 G 代码程序。</summary>
    LoadProgram,
    /// <summary>启动程序执行。</summary>
    Start,
    /// <summary>停止程序执行。</summary>
    Stop,
    /// <summary>NCI 紧急停止。</summary>
    EmergencyStop,
    /// <summary>紧急停止后继续执行。</summary>
    ContinueAfterEmergencyStop,
    /// <summary>复位 NCI 错误。</summary>
    Reset
}
