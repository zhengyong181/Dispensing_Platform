namespace DispensingPlatform.Hal.Contracts.Connection;

/// <summary>
/// 硬件连接契约。
/// </summary>
/// <remarks>
/// 连接细节（如 ADS、TCP、串口、现场总线）属于具体实现内部，不应泄露到公共契约。
/// 上层只关心“何时建立连接”以及连接失败时的异常反馈。
/// </remarks>
public interface IHardwareConnection
{
    /// <summary>
    /// 建立与控制器的通信连接。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    Task ConnectAsync(CancellationToken ct = default);
}
