using System.Text.RegularExpressions;
using DispensingPlatform.Hal.Beckhoff.Ads;
using DispensingPlatform.Hal.Beckhoff.Models;
using UnitsNet;

namespace DispensingPlatform.Hal.Beckhoff;

/// <summary>
/// Beckhoff HAL 入口实现。
/// </summary>
/// <remarks>
/// 本实现遵循“SDK 隔离 + 符号集中映射 + 单位强类型 + 可取消异步 IO”原则：
/// <list type="bullet">
/// <item><description>所有 PLC 交互都经由 <see cref="IAdsSymbolClient"/>，避免上层直接依赖 SDK。</description></item>
/// <item><description>所有符号路径通过 <see cref="BeckhoffPlcSymbols"/> 构造，避免散落硬编码。</description></item>
/// <item><description>直线与旋转轴使用不同单位类型，防止量纲误用。</description></item>
/// <item><description>每次读写都叠加超时与取消令牌，避免控制线程长时间阻塞。</description></item>
/// </list>
/// </remarks>
public sealed class BeckhoffHalClient : IBeckhoffHal
{
    // 输入点/输出点命名约束：与 PLC 中 GVL_IO 的 Xxxxxxx/Yxxxxxx 变量保持一致。
    private static readonly Regex DigitalInputNamePattern = new("^X\\d{6}$", RegexOptions.Compiled);
    private static readonly Regex DigitalOutputNamePattern = new("^Y\\d{6}$", RegexOptions.Compiled);

    private readonly BeckhoffConnectionOptions _options;
    private readonly IAdsSymbolClient _ads;
    private readonly Action<string>? _log;

    /// <summary>
    /// 创建 HAL 客户端（生产环境默认入口）。
    /// </summary>
    /// <param name="options">连接参数，空值时使用默认参数。</param>
    /// <param name="log">可选日志回调，用于输出读写轨迹。</param>
    public BeckhoffHalClient(
        BeckhoffConnectionOptions? options = null,
        Action<string>? log = null)
        : this(
            options ?? BeckhoffConnectionOptions.Default,
            new TwinCatAdsSymbolClientFactory(),
            log)
    {
    }

    /// <summary>
    /// 创建 HAL 客户端（测试/注入专用入口）。
    /// </summary>
    /// <param name="options">连接参数。</param>
    /// <param name="adsFactory">ADS 客户端工厂，可注入 fake 实现。</param>
    /// <param name="log">可选日志回调。</param>
    internal BeckhoffHalClient(
        BeckhoffConnectionOptions options,
        IAdsSymbolClientFactory adsFactory,
        Action<string>? log = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ads = (adsFactory ?? throw new ArgumentNullException(nameof(adsFactory))).Create();
        _log = log;
    }

    /// <inheritdoc />
    public BeckhoffConnectionOptions Connection => _options;

    /// <inheritdoc />
    public IReadOnlyList<BeckhoffAxisDefinition> Axes => BeckhoffAxisCatalog.All;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_ads.IsConnected)
        {
            return;
        }

        using var timeout = CreateTimeoutTokenSource(ct);
        await _ads.ConnectAsync(_options.AmsNetId, _options.AmsPort, timeout.Token).ConfigureAwait(false);
        _log?.Invoke($"Connected ADS: {_options.AmsNetId}:{_options.AmsPort}");
    }

    /// <inheritdoc />
    public async Task<BeckhoffSystemStatus> ReadSystemStatusAsync(CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct).ConfigureAwait(false);

        // 并行读取系统状态，降低轮询时延并减少单周期抖动。
        var manualModeTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bManual"), ct);
        var resetTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bReset"), ct);
        var stopTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bStop"), ct);
        var emgTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bEMG"), ct);
        var safetyDoorTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bSafetyDoor"), ct);
        var axesPowerOkTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bAxesPowerOK"), ct);
        var axesHomeOkTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bAxesHomeOK"), ct);
        var hasErrorTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bError"), ct);
        var hasWarningTask = ReadAsync<bool>(BeckhoffPlcSymbols.System("bWarning"), ct);

        await Task.WhenAll(
            manualModeTask,
            resetTask,
            stopTask,
            emgTask,
            safetyDoorTask,
            axesPowerOkTask,
            axesHomeOkTask,
            hasErrorTask,
            hasWarningTask).ConfigureAwait(false);

        // 返回统一时间戳快照，便于上层对多字段进行同周期判断。
        return new BeckhoffSystemStatus(
            ManualMode: manualModeTask.Result,
            ResetRequested: resetTask.Result,
            StopRequested: stopTask.Result,
            EmergencyStop: emgTask.Result,
            SafetyDoorActive: safetyDoorTask.Result,
            AxesPowerOk: axesPowerOkTask.Result,
            AxesHomeOk: axesHomeOkTask.Result,
            HasError: hasErrorTask.Result,
            HasWarning: hasWarningTask.Result,
            Timestamp: DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<BeckhoffAxisStatus> ReadAxisStatusAsync(int axisNo, CancellationToken ct = default)
    {
        var axis = BeckhoffAxisCatalog.Get(axisNo);
        await EnsureConnectedAsync(ct).ConfigureAwait(false);

        // 轴状态字段并行读取：包含使能、回零、运动、报警及当前位置速度。
        var enabledTask = ReadAsync<bool>(BeckhoffPlcSymbols.AxisState(axisNo, "bPowerOK"), ct);
        var homedTask = ReadAsync<bool>(BeckhoffPlcSymbols.AxisState(axisNo, "bHome_OK"), ct);
        var movingTask = ReadAsync<bool>(BeckhoffPlcSymbols.AxisState(axisNo, "bMoving"), ct);
        var hasJobTask = ReadAsync<bool>(BeckhoffPlcSymbols.AxisState(axisNo, "bHasJob"), ct);
        var hasErrorTask = ReadAsync<bool>(BeckhoffPlcSymbols.AxisState(axisNo, "bError"), ct);
        var hasWarningTask = ReadAsync<bool>(BeckhoffPlcSymbols.AxisState(axisNo, "bWarning"), ct);
        var ncErrorTask = ReadAsync<uint>(BeckhoffPlcSymbols.AxisState(axisNo, "udiNCErrorID"), ct);
        var driveErrorTask = ReadAsync<ushort>(BeckhoffPlcSymbols.AxisState(axisNo, "uiDriveErrorCode"), ct);
        var rawPositionTask = ReadAsync<float>(BeckhoffPlcSymbols.AxisPositionFeedbackSymbol(axis), ct);
        var rawVelocityTask = ReadAsync<float>(BeckhoffPlcSymbols.AxisState(axisNo, "rActVel"), ct);

        await Task.WhenAll(
            enabledTask,
            homedTask,
            movingTask,
            hasJobTask,
            hasErrorTask,
            hasWarningTask,
            ncErrorTask,
            driveErrorTask,
            rawPositionTask,
            rawVelocityTask).ConfigureAwait(false);

        var rawPosition = Convert.ToDouble(rawPositionTask.Result);
        var rawVelocity = Convert.ToDouble(rawVelocityTask.Result);

        Length? linearPos = null;
        Speed? linearVel = null;
        Angle? rotaryPos = null;
        RotationalSpeed? rotaryVel = null;

        // 根据轴类型进行单位转换，防止线性量与角度量混用。
        if (axis.AxisType == BeckhoffAxisType.Rotary)
        {
            rotaryPos = Angle.FromDegrees(rawPosition);
            rotaryVel = RotationalSpeed.FromDegreesPerSecond(rawVelocity);
        }
        else
        {
            linearPos = Length.FromMillimeters(rawPosition);
            linearVel = Speed.FromMillimetersPerSecond(rawVelocity);
        }

        return new BeckhoffAxisStatus(
            Axis: axis,
            IsEnabled: enabledTask.Result,
            IsHomed: homedTask.Result,
            IsMoving: movingTask.Result,
            HasJob: hasJobTask.Result,
            HasError: hasErrorTask.Result,
            HasWarning: hasWarningTask.Result,
            NcErrorCode: ncErrorTask.Result,
            DriveErrorCode: driveErrorTask.Result,
            LinearPosition: linearPos,
            LinearVelocity: linearVel,
            RotaryPosition: rotaryPos,
            RotaryVelocity: rotaryVel,
            Timestamp: DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task SetAxisPowerAsync(int axisNo, bool enabled, CancellationToken ct = default)
    {
        BeckhoffAxisCatalog.Get(axisNo);
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisControl(axisNo, "bPowerOn"), enabled, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task HomeAxisAsync(int axisNo, CancellationToken ct = default)
    {
        BeckhoffAxisCatalog.Get(axisNo);
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        await PulseAsync(BeckhoffPlcSymbols.AxisControl(axisNo, "bHome_A"), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StopAxisAsync(int axisNo, CancellationToken ct = default)
    {
        BeckhoffAxisCatalog.Get(axisNo);
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        await PulseAsync(BeckhoffPlcSymbols.AxisControl(axisNo, "bStop_A"), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetAxisAsync(int axisNo, CancellationToken ct = default)
    {
        BeckhoffAxisCatalog.Get(axisNo);
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        await PulseAsync(BeckhoffPlcSymbols.AxisControl(axisNo, "bReset_A"), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MoveLinearAxisAsync(int axisNo, BeckhoffLinearMoveCommand command, CancellationToken ct = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var axis = BeckhoffAxisCatalog.Get(axisNo);
        if (axis.AxisType != BeckhoffAxisType.Linear)
        {
            throw new InvalidOperationException($"Axis {axisNo} is not linear.");
        }

        ValidateLinearCommand(command);

        await EnsureConnectedAsync(ct).ConfigureAwait(false);

        // 先写定位参数，再触发启动位，确保 PLC 在同一周期拿到完整命令。
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "bRelEnable"), command.Relative, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rPos"), (float)command.Target.Millimeters, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rVel"), (float)command.Velocity.MillimetersPerSecond, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rAcc"), (float)command.Acceleration.MillimetersPerSecondSquared, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rDec"), (float)command.Deceleration.MillimetersPerSecondSquared, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rJerk"), (float)command.Jerk.MillimetersPerSecondCubed, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionControl(axisNo, "bGo_Con"), true, ct).ConfigureAwait(false);
        await PulseAsync(BeckhoffPlcSymbols.AxisPositionControl(axisNo, "bGo_A"), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MoveRotaryAxisAsync(int axisNo, BeckhoffRotaryMoveCommand command, CancellationToken ct = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var axis = BeckhoffAxisCatalog.Get(axisNo);
        if (axis.AxisType != BeckhoffAxisType.Rotary)
        {
            throw new InvalidOperationException($"Axis {axisNo} is not rotary.");
        }

        ValidateRotaryCommand(command);

        await EnsureConnectedAsync(ct).ConfigureAwait(false);

        // 旋转轴参数统一换算为“度系”，与 PLC 内部 REAL 角度参数保持一致。
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "bRelEnable"), command.Relative, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rPos"), (float)command.Target.Degrees, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rVel"), (float)command.Velocity.DegreesPerSecond, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rAcc"), (float)command.Acceleration.DegreesPerSecondSquared, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rDec"), (float)command.Deceleration.DegreesPerSecondSquared, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionParameter(axisNo, "rJerk"), (float)command.JerkDegreesPerSecondCubed, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AxisPositionControl(axisNo, "bGo_Con"), true, ct).ConfigureAwait(false);
        await PulseAsync(BeckhoffPlcSymbols.AxisPositionControl(axisNo, "bGo_A"), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetNciCommandAsync(BeckhoffNciCommand command, bool value, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.NciCommand(command), value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetDigitalOutputAsync(string outputName, bool value, CancellationToken ct = default)
    {
        if (!DigitalOutputNamePattern.IsMatch(outputName))
        {
            throw new ArgumentException("Output name must match pattern Yxxxxxx.", nameof(outputName));
        }

        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.DigitalIo(outputName), value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ReadDigitalInputAsync(string inputName, CancellationToken ct = default)
    {
        if (!DigitalInputNamePattern.IsMatch(inputName))
        {
            throw new ArgumentException("Input name must match pattern Xxxxxxx.", nameof(inputName));
        }

        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        return await ReadAsync<bool>(BeckhoffPlcSymbols.DigitalIo(inputName), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<double> ReadAnalogInputRawAsync(int channel, CancellationToken ct = default)
    {
        ValidateAnalogChannel(channel);
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        return await ReadAsync<float>(BeckhoffPlcSymbols.AnalogInput(channel), ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteAnalogOutputRawAsync(int channel, double rawValue, CancellationToken ct = default)
    {
        ValidateAnalogChannel(channel);
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.AnalogOutput(channel), (float)rawValue, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetDispenseTriggerAsync(bool valve, bool highVoltage, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.Nci("bEx_Trig"), valve, ct).ConfigureAwait(false);
        await WriteAsync(BeckhoffPlcSymbols.Nci("bEx_TrigH"), highVoltage, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _ads.DisposeAsync();

    /// <summary>
    /// 校验模拟量通道范围。
    /// </summary>
    /// <remarks>
    /// 当前 PLC 定义为 1..20，越界通常意味着上层配置错误。
    /// </remarks>
    private static void ValidateAnalogChannel(int channel)
    {
        if (channel is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "Analog channel must be in [1,20].");
        }
    }

    /// <summary>
    /// 校验直线运动命令参数。
    /// </summary>
    /// <remarks>
    /// 速度/加减速度/跃度必须为正值，避免向 PLC 下发无意义或危险参数。
    /// </remarks>
    private static void ValidateLinearCommand(BeckhoffLinearMoveCommand command)
    {
        if (command.Velocity <= Speed.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Velocity must be positive.");
        }

        if (command.Acceleration <= Acceleration.Zero || command.Deceleration <= Acceleration.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Acceleration and deceleration must be positive.");
        }

        if (command.Jerk <= Jerk.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Jerk must be positive.");
        }
    }

    /// <summary>
    /// 校验旋转运动命令参数。
    /// </summary>
    /// <remarks>
    /// 角速度/角加减速度/角跃度必须为正值，避免轴状态机进入不可预测分支。
    /// </remarks>
    private static void ValidateRotaryCommand(BeckhoffRotaryMoveCommand command)
    {
        if (command.Velocity <= RotationalSpeed.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Velocity must be positive.");
        }

        if (command.Acceleration <= RotationalAcceleration.Zero || command.Deceleration <= RotationalAcceleration.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Acceleration and deceleration must be positive.");
        }

        if (command.JerkDegreesPerSecondCubed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Jerk must be positive.");
        }
    }

    /// <summary>
    /// 确保 ADS 连接可用。
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (!_ads.IsConnected)
        {
            await ConnectAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 读取符号并输出可选日志。
    /// </summary>
    private async Task<T> ReadAsync<T>(string symbolPath, CancellationToken ct)
        where T : notnull
    {
        using var timeout = CreateTimeoutTokenSource(ct);
        var value = await _ads.ReadValueAsync<T>(symbolPath, timeout.Token).ConfigureAwait(false);
        _log?.Invoke($"READ {symbolPath} -> {value}");
        return value;
    }

    /// <summary>
    /// 写入符号并输出可选日志。
    /// </summary>
    private async Task WriteAsync<T>(string symbolPath, T value, CancellationToken ct)
        where T : notnull
    {
        using var timeout = CreateTimeoutTokenSource(ct);
        await _ads.WriteValueAsync(symbolPath, value, timeout.Token).ConfigureAwait(false);
        _log?.Invoke($"WRITE {symbolPath} <- {value}");
    }

    /// <summary>
    /// 执行布尔触发脉冲。
    /// </summary>
    /// <remarks>
    /// 先写 <c>true</c> 再延迟写 <c>false</c>，用于触发 PLC 的上升沿命令位。
    /// </remarks>
    private async Task PulseAsync(string symbolPath, CancellationToken ct)
    {
        await WriteAsync(symbolPath, true, ct).ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromMilliseconds(10), ct).ConfigureAwait(false);
        await WriteAsync(symbolPath, false, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 创建“外部取消 + 命令超时”的组合令牌。
    /// </summary>
    /// <remarks>
    /// 所有 IO 命令必须可取消且可超时，防止链路异常导致任务长期挂起。
    /// </remarks>
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
