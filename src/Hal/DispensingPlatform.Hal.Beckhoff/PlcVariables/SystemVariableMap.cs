namespace DispensingPlatform.Hal.Beckhoff.PlcVariables;

/// <summary>
/// 系统状态 PLC 变量名映射。
/// </summary>
/// <remarks>
/// 本文件对应 PLC 中的 <c>Com_GVLS.stSystem</c>。凡是整机急停、安全门、报警、手自动、
/// 轴组状态等系统级变量，都应从这里统一构造路径。
/// </remarks>
internal static class SystemVariableMap
{
    /// <summary>
    /// 构造系统状态字段路径。
    /// </summary>
    public static string Field(string field)
        => $"Com_GVLS.stSystem.{field}";
}
