using TwinCAT.Ads;

namespace DispensingPlatform.Hal.Beckhoff.Connection;

/// <summary>
/// 基于 Beckhoff 官方 SDK 的 ADS 符号客户端。
/// </summary>
/// <remarks>
/// 该类只负责 SDK 级连接和符号读写，不包含任何设备业务含义。所有“哪个变量代表什么”
/// 的语义都放在 <c>PlcVariables</c> 目录下的变量映射文件中。
/// </remarks>
internal sealed class TwinCatAdsClient : IAdsSymbolClient
{
    private readonly AdsClient _client = new();

    /// <inheritdoc />
    public bool IsConnected => _client.IsConnected;

    /// <inheritdoc />
    public Task ConnectAsync(string amsNetId, int amsPort, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _client.Connect(amsNetId, amsPort);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<T> ReadValueAsync<T>(string symbolPath, CancellationToken ct)
        where T : notnull
    {
        var result = await _client.ReadValueAsync<T>(symbolPath, ct).ConfigureAwait(false);
        result.ThrowOnError();
        return result.Value ?? throw new InvalidOperationException($"PLC变量“{symbolPath}”读取结果为空。");
    }

    /// <inheritdoc />
    public async Task WriteValueAsync<T>(string symbolPath, T value, CancellationToken ct)
        where T : notnull
    {
        var result = await _client.WriteValueAsync(symbolPath, value, ct).ConfigureAwait(false);
        result.ThrowOnError();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
