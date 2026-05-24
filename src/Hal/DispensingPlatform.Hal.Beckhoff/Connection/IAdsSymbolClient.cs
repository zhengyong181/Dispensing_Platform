namespace DispensingPlatform.Hal.Beckhoff.Connection;

/// <summary>
/// ADS 符号读写客户端抽象。
/// </summary>
/// <remarks>
/// 该接口是 Beckhoff 官方 SDK 与本项目 HAL 之间的隔离层。生产环境使用真实 ADS 客户端，
/// 单元测试使用内存假实现，因此默认测试不需要连接真实 PLC。
/// </remarks>
internal interface IAdsSymbolClient : IAsyncDisposable
{
    /// <summary>
    /// 当前 ADS 连接是否已经建立。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接到指定 AMS 地址和端口。
    /// </summary>
    /// <param name="amsNetId">目标控制器 AMS NetId。</param>
    /// <param name="amsPort">目标 PLC Runtime 端口。</param>
    /// <param name="ct">取消令牌，用于外部停止或超时停止。</param>
    Task ConnectAsync(string amsNetId, int amsPort, CancellationToken ct);

    /// <summary>
    /// 读取指定 PLC 符号的值。
    /// </summary>
    /// <typeparam name="T">期望读取到的 .NET 类型。</typeparam>
    /// <param name="symbolPath">PLC 符号完整路径。</param>
    /// <param name="ct">取消令牌。</param>
    Task<T> ReadValueAsync<T>(string symbolPath, CancellationToken ct)
        where T : notnull;

    /// <summary>
    /// 写入指定 PLC 符号的值。
    /// </summary>
    /// <typeparam name="T">写入值的 .NET 类型。</typeparam>
    /// <param name="symbolPath">PLC 符号完整路径。</param>
    /// <param name="value">待写入值。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteValueAsync<T>(string symbolPath, T value, CancellationToken ct)
        where T : notnull;
}
