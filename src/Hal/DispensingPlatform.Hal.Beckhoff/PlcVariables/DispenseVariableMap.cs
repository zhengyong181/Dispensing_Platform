namespace DispensingPlatform.Hal.Beckhoff.PlcVariables;

/// <summary>
/// 点胶触发 PLC 变量名映射。
/// </summary>
/// <remarks>
/// 当前 PLC 将阀触发和高压触发字段放在 <c>Com_GVLS.stIJP</c> 中。这里单独建文件，
/// 是为了让维护者从文件名就能找到点胶相关变量。
/// </remarks>
internal static class DispenseVariableMap
{
    /// <summary>
    /// 阀触发输出变量。
    /// </summary>
    public static string ValveTrigger => NciVariableMap.Field("bEx_Trig");

    /// <summary>
    /// 高压触发输出变量。
    /// </summary>
    public static string HighVoltageTrigger => NciVariableMap.Field("bEx_TrigH");
}
