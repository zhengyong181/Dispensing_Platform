namespace DispensingPlatform.Hal.Beckhoff.Connection;

/// <summary>
/// ADS 客户端工厂。
/// </summary>
/// <remarks>
/// 工厂负责创建 ADS 客户端实例。这样 HAL 主入口不用知道客户端的具体构造细节，
/// 测试时也可以替换为 Fake 工厂。
/// </remarks>
internal interface IAdsSymbolClientFactory
{
    /// <summary>
    /// 创建 ADS 符号客户端。
    /// </summary>
    IAdsSymbolClient Create();
}
