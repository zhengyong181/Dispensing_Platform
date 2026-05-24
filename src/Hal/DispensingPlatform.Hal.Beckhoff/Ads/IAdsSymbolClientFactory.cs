namespace DispensingPlatform.Hal.Beckhoff.Ads;

/// <summary>
/// ADS 客户端工厂抽象。
/// </summary>
/// <remarks>
/// 通过工厂创建客户端可以避免 HAL 在构造阶段直接依赖具体 SDK 类型，
/// 同时便于测试环境替换为 Fake 客户端。
/// </remarks>
internal interface IAdsSymbolClientFactory
{
    /// <summary>
    /// 创建一个新的 ADS 客户端实例。
    /// </summary>
    IAdsSymbolClient Create();
}
