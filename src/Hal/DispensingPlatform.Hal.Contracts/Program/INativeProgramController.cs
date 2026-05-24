namespace DispensingPlatform.Hal.Contracts.Program;

/// <summary>
/// 控制器原生程序通道控制契约。
/// </summary>
public interface INativeProgramController
{
    /// <summary>
    /// 设置原生命令位。
    /// </summary>
    /// <param name="command">命令类型。</param>
    /// <param name="value">目标值。</param>
    /// <param name="ct">取消令牌。</param>
    Task SetNativeProgramCommandAsync(NativeProgramCommand command, bool value, CancellationToken ct = default);
}
