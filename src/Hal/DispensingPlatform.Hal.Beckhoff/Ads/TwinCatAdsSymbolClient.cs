using TwinCAT.Ads;

namespace DispensingPlatform.Hal.Beckhoff.Ads;

/// <summary>
/// 基于 Beckhoff 官方 SDK 的 ADS 符号客户端实现。
/// </summary>
/// <remarks>
/// 该类仅负责“连接/读写/异常抛出”三件事，业务语义由上层 HAL 负责。
/// </remarks>
internal sealed class TwinCatAdsSymbolClient : IAdsSymbolClient
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
        return result.Value ?? throw new InvalidOperationException($"ADS symbol '{symbolPath}' returned null.");
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
