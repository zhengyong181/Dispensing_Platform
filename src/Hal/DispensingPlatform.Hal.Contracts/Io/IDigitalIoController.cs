namespace DispensingPlatform.Hal.Contracts.Io;

/// <summary>
/// 数字 IO 控制契约。
/// </summary>
public interface IDigitalIoController
{
    /// <summary>
    /// 设置数字输出点。
    /// </summary>
    /// <param name="outputName">输出点名称。</param>
    /// <param name="value">目标值。</param>
    /// <param name="ct">取消令牌。</param>
    Task SetDigitalOutputAsync(string outputName, bool value, CancellationToken ct = default);

    /// <summary>
    /// 读取数字输入点。
    /// </summary>
    /// <param name="inputName">输入点名称。</param>
    /// <param name="ct">取消令牌。</param>
    Task<bool> ReadDigitalInputAsync(string inputName, CancellationToken ct = default);
}
