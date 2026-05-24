namespace DispensingPlatform.Hal.Contracts.Machine;

/// <summary>
/// 整机状态读取契约。
/// </summary>
public interface IMachineStatusReader
{
    /// <summary>
    /// 读取整机状态快照。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    Task<MachineStatus> ReadMachineStatusAsync(CancellationToken ct = default);
}
