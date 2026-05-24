namespace DispensingPlatform.Hal.Beckhoff.Connection;

/// <summary>
/// TwinCAT ADS 客户端工厂。
/// </summary>
/// <remarks>
/// 生产环境默认使用该工厂创建 Beckhoff 官方 SDK 客户端。
/// </remarks>
internal sealed class TwinCatAdsClientFactory : IAdsSymbolClientFactory
{
    /// <inheritdoc />
    public IAdsSymbolClient Create() => new TwinCatAdsClient();
}
