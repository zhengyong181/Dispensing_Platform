namespace DispensingPlatform.Hal.Beckhoff;

/// <summary>
/// Beckhoff ADS 连接参数。
/// </summary>
/// <remarks>
/// 该配置用于控制 HAL 与 TwinCAT 运行时的连接行为，包括目标 AMS 地址、端口以及每次命令的超时策略。
/// </remarks>
public sealed class BeckhoffConnectionOptions
{
    /// <summary>
    /// 目标控制器的 AMS NetId。
    /// </summary>
    /// <remarks>
    /// 默认值与当前工控机部署约定保持一致，实际部署时可通过配置覆盖。
    /// </remarks>
    public string AmsNetId { get; init; } = "192.168.1.50.1.1";

    /// <summary>
    /// 目标 PLC 运行时端口。
    /// </summary>
    /// <remarks>
    /// TwinCAT PLC Runtime 1 的常用端口为 851。
    /// </remarks>
    public int AmsPort { get; init; } = 851;

    /// <summary>
    /// 单次读写命令的超时时间。
    /// </summary>
    /// <remarks>
    /// 所有 HAL 命令都会创建带超时的取消令牌，超时后会主动取消操作，避免线程长期阻塞。
    /// </remarks>
    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// 默认连接参数实例。
    /// </summary>
    public static BeckhoffConnectionOptions Default { get; } = new();
}
