namespace DispensingPlatform.Hal.Beckhoff;

/// <summary>
/// Beckhoff ADS 连接参数。
/// </summary>
/// <remarks>
/// 该配置用于控制 HAL 与 TwinCAT PLC 运行时的连接方式。现场部署时通常只需要确认
/// <see cref="AmsNetId"/> 和 <see cref="AmsPort"/> 是否与目标工控机/控制器一致。
/// </remarks>
public sealed class BeckhoffConnectionOptions
{
    /// <summary>
    /// 目标控制器的 AMS NetId。
    /// </summary>
    /// <remarks>
    /// 当前默认值来自 PLC 工程中的 Beckhoff 目标配置。客户现场如果更换控制器或网卡，
    /// 这里通常是第一处需要检查的连接参数。
    /// </remarks>
    public string AmsNetId { get; init; } = "192.168.1.50.1.1";

    /// <summary>
    /// 目标 PLC 运行时端口。
    /// </summary>
    /// <remarks>
    /// TwinCAT PLC Runtime 1 的常用端口是 851；如果 PLC 程序部署到其它 Runtime，
    /// 需要同步修改该端口。
    /// </remarks>
    public int AmsPort { get; init; } = 851;

    /// <summary>
    /// 单次 ADS 读写命令的超时时间。
    /// </summary>
    /// <remarks>
    /// 所有硬件 IO 都会叠加这个超时，避免 PLC 链路异常时上位机任务一直挂起。
    /// </remarks>
    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// 默认连接参数。
    /// </summary>
    public static BeckhoffConnectionOptions Default { get; } = new();
}
