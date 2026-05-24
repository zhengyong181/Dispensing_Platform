namespace DispensingPlatform.Hal.Beckhoff.Connection;

/// <summary>
/// PLC 访问器。
/// </summary>
/// <remarks>
/// 该类把“连接、读、写、脉冲、超时、日志”这些横切逻辑统一收口。各设备模块只需要关心
/// 自己要读写哪个 PLC 变量，不需要重复处理连接和超时。
/// </remarks>
internal sealed class BeckhoffPlcAccessor : IAsyncDisposable
{
    private readonly BeckhoffConnectionOptions _options;
    private readonly IAdsSymbolClient _ads;
    private readonly Action<string>? _log;

    /// <summary>
    /// 创建 PLC 访问器。
    /// </summary>
    /// <param name="options">ADS 连接参数。</param>
    /// <param name="adsFactory">ADS 客户端工厂。</param>
    /// <param name="log">可选日志回调。</param>
    public BeckhoffPlcAccessor(
        BeckhoffConnectionOptions options,
        IAdsSymbolClientFactory adsFactory,
        Action<string>? log)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ads = (adsFactory ?? throw new ArgumentNullException(nameof(adsFactory))).Create();
        _log = log;
    }

    /// <summary>
    /// 当前连接参数。
    /// </summary>
    public BeckhoffConnectionOptions Connection => _options;

    /// <summary>
    /// 确保 ADS 连接已经建立。
    /// </summary>
    public async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_ads.IsConnected)
        {
            return;
        }

        using var timeout = CreateTimeoutTokenSource(ct);
        await _ads.ConnectAsync(_options.AmsNetId, _options.AmsPort, timeout.Token).ConfigureAwait(false);
        _log?.Invoke($"已连接到 Beckhoff ADS：{_options.AmsNetId}:{_options.AmsPort}");
    }

    /// <summary>
    /// 读取 PLC 符号。
    /// </summary>
    public async Task<T> ReadAsync<T>(string symbolPath, CancellationToken ct)
        where T : notnull
    {
        await EnsureConnectedAsync(ct).ConfigureAwait(false);

        using var timeout = CreateTimeoutTokenSource(ct);
        var value = await _ads.ReadValueAsync<T>(symbolPath, timeout.Token).ConfigureAwait(false);
        _log?.Invoke($"读取 PLC 变量：{symbolPath} -> {value}");
        return value;
    }

    /// <summary>
    /// 写入 PLC 符号。
    /// </summary>
    public async Task WriteAsync<T>(string symbolPath, T value, CancellationToken ct)
        where T : notnull
    {
        await EnsureConnectedAsync(ct).ConfigureAwait(false);

        using var timeout = CreateTimeoutTokenSource(ct);
        await _ads.WriteValueAsync(symbolPath, value, timeout.Token).ConfigureAwait(false);
        _log?.Invoke($"写入 PLC 变量：{symbolPath} <- {value}");
    }

    /// <summary>
    /// 对布尔型 PLC 触发位执行上升沿脉冲。
    /// </summary>
    /// <remarks>
    /// 该方法先写入 <c>true</c>，短暂延迟后再写入 <c>false</c>。PLC 中的自动命令位通常
    /// 通过上升沿触发，因此所有回零、停止、复位、定位启动等动作都走这个统一入口。
    /// </remarks>
    public async Task PulseAsync(string symbolPath, CancellationToken ct)
    {
        await WriteAsync(symbolPath, true, ct).ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromMilliseconds(10), ct).ConfigureAwait(false);
        await WriteAsync(symbolPath, false, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _ads.DisposeAsync();

    /// <summary>
    /// 创建“外部取消 + 命令超时”的组合取消令牌。
    /// </summary>
    private CancellationTokenSource CreateTimeoutTokenSource(CancellationToken ct)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (_options.CommandTimeout > TimeSpan.Zero)
        {
            timeout.CancelAfter(_options.CommandTimeout);
        }

        return timeout;
    }
}
