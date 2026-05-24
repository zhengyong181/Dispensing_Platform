namespace DispensingPlatform.Hal.Beckhoff.Models;

/// <summary>
/// 轴静态定义信息。
/// </summary>
/// <param name="AxisNo">PLC 侧轴序号，范围为 1..10。</param>
/// <param name="AxisId">平台内部轴标识，用于跨层日志与追踪。</param>
/// <param name="DisplayName">面向操作界面的轴显示名称。</param>
/// <param name="AxisType">轴类型（直线或旋转）。</param>
public sealed record BeckhoffAxisDefinition(
    int AxisNo,
    string AxisId,
    string DisplayName,
    BeckhoffAxisType AxisType);
