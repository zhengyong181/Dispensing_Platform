namespace DispensingPlatform.Hal.Beckhoff.Ads;

/// <summary>
/// ADS 符号读写客户端抽象。
/// </summary>
/// <remarks>
/// 该抽象用于隔离第三方 ADS SDK，便于单元测试注入假实现并避免测试依赖真实 PLC。
/// </remarks>
internal interface IAdsSymbolClient : IAsyncDisposable
{
    /// <summary>
    /// 当前连接状态。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 建立 ADS 连接。
    /// </summary>
    Task ConnectAsync(string amsNetId, int amsPort, CancellationToken ct);

    /// <summary>
    /// 读取指定符号并转换为目标类型。
    /// </summary>
    Task<T> ReadValueAsync<T>(string symbolPath, CancellationToken ct)
        where T : notnull;

    /// <summary>
    /// 向指定符号写入目标值。
    /// </summary>
    Task WriteValueAsync<T>(string symbolPath, T value, CancellationToken ct)
        where T : notnull;
}
