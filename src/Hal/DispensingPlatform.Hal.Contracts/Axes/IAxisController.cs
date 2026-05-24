namespace DispensingPlatform.Hal.Contracts.Axes;

/// <summary>
/// 轴控制契约。
/// </summary>
/// <remarks>
/// 该接口仅描述“单轴级”控制能力，不覆盖多轴插补、轨迹规划或控制器脚本调度。
/// 多轴和流程编排应由上层调度器或执行适配层负责。
/// </remarks>
public interface IAxisController
{
    /// <summary>
    /// 可用轴描述集合。
    /// </summary>
    IReadOnlyList<AxisDescriptor> Axes { get; }

    /// <summary>
    /// 读取单轴状态快照。
    /// </summary>
    /// <param name="axisId">轴标识。</param>
    /// <param name="ct">取消令牌。</param>
    Task<AxisStatus> ReadAxisStatusAsync(string axisId, CancellationToken ct = default);

    /// <summary>
    /// 设置轴使能状态。
    /// </summary>
    /// <param name="axisId">轴标识。</param>
    /// <param name="enabled">目标使能状态。</param>
    /// <param name="ct">取消令牌。</param>
    Task SetAxisPowerAsync(string axisId, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// 触发单轴回零。
    /// </summary>
    /// <param name="axisId">轴标识。</param>
    /// <param name="ct">取消令牌。</param>
    Task HomeAxisAsync(string axisId, CancellationToken ct = default);

    /// <summary>
    /// 触发单轴停止。
    /// </summary>
    /// <param name="axisId">轴标识。</param>
    /// <param name="ct">取消令牌。</param>
    Task StopAxisAsync(string axisId, CancellationToken ct = default);

    /// <summary>
    /// 触发单轴复位。
    /// </summary>
    /// <param name="axisId">轴标识。</param>
    /// <param name="ct">取消令牌。</param>
    Task ResetAxisAsync(string axisId, CancellationToken ct = default);

    /// <summary>
    /// 执行直线轴定位。
    /// </summary>
    /// <param name="axisId">轴标识。</param>
    /// <param name="command">直线定位命令。</param>
    /// <param name="ct">取消令牌。</param>
    Task MoveLinearAxisAsync(string axisId, LinearAxisMoveCommand command, CancellationToken ct = default);

    /// <summary>
    /// 执行旋转轴定位。
    /// </summary>
    /// <param name="axisId">轴标识。</param>
    /// <param name="command">旋转定位命令。</param>
    /// <param name="ct">取消令牌。</param>
    Task MoveRotaryAxisAsync(string axisId, RotaryAxisMoveCommand command, CancellationToken ct = default);
}
