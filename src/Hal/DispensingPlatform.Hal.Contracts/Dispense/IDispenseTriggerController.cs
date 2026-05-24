namespace DispensingPlatform.Hal.Contracts.Dispense;

/// <summary>
/// 点胶触发控制契约。
/// </summary>
public interface IDispenseTriggerController
{
    /// <summary>
    /// 设置点胶触发状态。
    /// </summary>
    /// <param name="state">触发状态。</param>
    /// <param name="ct">取消令牌。</param>
    Task SetDispenseTriggerAsync(DispenseTriggerState state, CancellationToken ct = default);
}
