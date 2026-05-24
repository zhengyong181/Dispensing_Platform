namespace DispensingPlatform.Hal.Contracts.Dispense;

/// <summary>
/// 点胶触发状态。
/// </summary>
/// <param name="Valve">阀触发状态。</param>
/// <param name="HighVoltage">高压触发状态。</param>
public sealed record DispenseTriggerState(
    bool Valve,
    bool HighVoltage);
