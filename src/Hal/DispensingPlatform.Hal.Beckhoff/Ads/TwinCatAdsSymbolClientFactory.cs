namespace DispensingPlatform.Hal.Beckhoff.Ads;

/// <summary>
/// TwinCAT ADS 客户端工厂。
/// </summary>
/// <remarks>
/// 生产环境默认通过该工厂创建真实 ADS 通信对象。
/// </remarks>
internal sealed class TwinCatAdsSymbolClientFactory : IAdsSymbolClientFactory
{
    /// <inheritdoc />
    public IAdsSymbolClient Create() => new TwinCatAdsSymbolClient();
}
