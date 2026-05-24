using DispensingPlatform.Hal.Beckhoff.Connection;
using DispensingPlatform.Hal.Beckhoff.PlcVariables;
using DispensingPlatform.Hal.Contracts.Axes;
using UnitsNet;

namespace DispensingPlatform.Hal.Beckhoff.Axes;

/// <summary>
/// Beckhoff 轴模块。
/// </summary>
/// <remarks>
/// 本模块只负责轴相关 PLC 读写：使能、回零、停止、复位、定位、状态读取。
/// 轴 id 到 PLC 数组序号的映射由 <see cref="BeckhoffAxisCatalog"/> 统一维护。
/// </remarks>
internal sealed class BeckhoffAxisHal
{
    private readonly BeckhoffPlcAccessor _plc;

    /// <summary>
    /// 创建轴模块。
    /// </summary>
    public BeckhoffAxisHal(BeckhoffPlcAccessor plc)
    {
        _plc = plc ?? throw new ArgumentNullException(nameof(plc));
    }

    /// <summary>
    /// 当前平台全部轴定义。
    /// </summary>
    public IReadOnlyList<AxisDescriptor> Axes => BeckhoffAxisCatalog.All;

    /// <summary>
    /// 读取单轴状态。
    /// </summary>
    public async Task<AxisStatus> ReadStatusAsync(string axisId, CancellationToken ct)
    {
        var axis = BeckhoffAxisCatalog.GetByAxisId(axisId);
        var axisNo = GetAxisNo(axis);

        // 并行读取状态字段，减少一次状态刷新耗时。
        var enabledTask = _plc.ReadAsync<bool>(AxisVariableMap.State(axisNo, "bPowerOK"), ct);
        var homedTask = _plc.ReadAsync<bool>(AxisVariableMap.State(axisNo, "bHome_OK"), ct);
        var movingTask = _plc.ReadAsync<bool>(AxisVariableMap.State(axisNo, "bMoving"), ct);
        var hasJobTask = _plc.ReadAsync<bool>(AxisVariableMap.State(axisNo, "bHasJob"), ct);
        var hasErrorTask = _plc.ReadAsync<bool>(AxisVariableMap.State(axisNo, "bError"), ct);
        var hasWarningTask = _plc.ReadAsync<bool>(AxisVariableMap.State(axisNo, "bWarning"), ct);
        var ncErrorTask = _plc.ReadAsync<uint>(AxisVariableMap.State(axisNo, "udiNCErrorID"), ct);
        var driveErrorTask = _plc.ReadAsync<ushort>(AxisVariableMap.State(axisNo, "uiDriveErrorCode"), ct);
        var rawPositionTask = _plc.ReadAsync<float>(AxisVariableMap.PositionFeedback(axis), ct);
        var rawVelocityTask = _plc.ReadAsync<float>(AxisVariableMap.State(axisNo, "rActVel"), ct);

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

        // 旋转轴和直线轴使用不同单位模型，避免上层误用单位。
        if (axis.Kind == AxisKind.Rotary)
        {
            rotaryPos = Angle.FromDegrees(rawPosition);
            rotaryVel = RotationalSpeed.FromDegreesPerSecond(rawVelocity);
        }
        else
        {
            linearPos = Length.FromMillimeters(rawPosition);
            linearVel = Speed.FromMillimetersPerSecond(rawVelocity);
        }

        return new AxisStatus(
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

    /// <summary>
    /// 设置轴使能。
    /// </summary>
    public async Task SetPowerAsync(string axisId, bool enabled, CancellationToken ct)
    {
        var axisNo = GetAxisNo(BeckhoffAxisCatalog.GetByAxisId(axisId));
        await _plc.WriteAsync(AxisVariableMap.Control(axisNo, "bPowerOn"), enabled, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 触发轴自动回零。
    /// </summary>
    public async Task HomeAsync(string axisId, CancellationToken ct)
    {
        var axisNo = GetAxisNo(BeckhoffAxisCatalog.GetByAxisId(axisId));
        await _plc.PulseAsync(AxisVariableMap.Control(axisNo, "bHome_A"), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 触发轴自动停止。
    /// </summary>
    public async Task StopAsync(string axisId, CancellationToken ct)
    {
        var axisNo = GetAxisNo(BeckhoffAxisCatalog.GetByAxisId(axisId));
        await _plc.PulseAsync(AxisVariableMap.Control(axisNo, "bStop_A"), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 触发轴自动复位。
    /// </summary>
    public async Task ResetAsync(string axisId, CancellationToken ct)
    {
        var axisNo = GetAxisNo(BeckhoffAxisCatalog.GetByAxisId(axisId));
        await _plc.PulseAsync(AxisVariableMap.Control(axisNo, "bReset_A"), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 执行直线轴定位。
    /// </summary>
    public async Task MoveLinearAsync(string axisId, LinearAxisMoveCommand command, CancellationToken ct)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var axis = BeckhoffAxisCatalog.GetByAxisId(axisId);
        if (axis.Kind != AxisKind.Linear)
        {
            throw new InvalidOperationException($"轴 {axis.AxisId} 不是直线轴，不能执行直线轴定位命令。");
        }

        var axisNo = GetAxisNo(axis);
        ValidateLinearCommand(command);

        // 先写完整定位参数，再触发启动位，避免 PLC 读取到半套参数。
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "bRelEnable"), command.Relative, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rPos"), (float)command.Target.Millimeters, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rVel"), (float)command.Velocity.MillimetersPerSecond, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rAcc"), (float)command.Acceleration.MillimetersPerSecondSquared, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rDec"), (float)command.Deceleration.MillimetersPerSecondSquared, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rJerk"), (float)command.Jerk.MillimetersPerSecondCubed, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionControl(axisNo, "bGo_Con"), true, ct).ConfigureAwait(false);
        await _plc.PulseAsync(AxisVariableMap.PositionControl(axisNo, "bGo_A"), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 执行旋转轴定位。
    /// </summary>
    public async Task MoveRotaryAsync(string axisId, RotaryAxisMoveCommand command, CancellationToken ct)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var axis = BeckhoffAxisCatalog.GetByAxisId(axisId);
        if (axis.Kind != AxisKind.Rotary)
        {
            throw new InvalidOperationException($"轴 {axis.AxisId} 不是旋转轴，不能执行旋转轴定位命令。");
        }

        var axisNo = GetAxisNo(axis);
        ValidateRotaryCommand(command);

        // 旋转轴统一写入“度系”参数，与 PLC REAL 字段语义保持一致。
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "bRelEnable"), command.Relative, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rPos"), (float)command.Target.Degrees, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rVel"), (float)command.Velocity.DegreesPerSecond, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rAcc"), (float)command.Acceleration.DegreesPerSecondSquared, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rDec"), (float)command.Deceleration.DegreesPerSecondSquared, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionParameter(axisNo, "rJerk"), (float)command.JerkDegreesPerSecondCubed, ct).ConfigureAwait(false);
        await _plc.WriteAsync(AxisVariableMap.PositionControl(axisNo, "bGo_Con"), true, ct).ConfigureAwait(false);
        await _plc.PulseAsync(AxisVariableMap.PositionControl(axisNo, "bGo_A"), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取轴对应的 PLC 序号。
    /// </summary>
    private static int GetAxisNo(AxisDescriptor axis)
    {
        if (axis.ControllerIndex is null)
        {
            throw new InvalidOperationException($"轴 {axis.AxisId} 未配置 PLC 序号映射。");
        }

        return axis.ControllerIndex.Value;
    }

    /// <summary>
    /// 校验直线运动参数。
    /// </summary>
    private static void ValidateLinearCommand(LinearAxisMoveCommand command)
    {
        if (command.Velocity <= Speed.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "直线轴速度必须大于 0。");
        }

        if (command.Acceleration <= Acceleration.Zero || command.Deceleration <= Acceleration.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "直线轴加速度和减速度必须大于 0。");
        }

        if (command.Jerk <= Jerk.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "直线轴跃度必须大于 0。");
        }
    }

    /// <summary>
    /// 校验旋转运动参数。
    /// </summary>
    private static void ValidateRotaryCommand(RotaryAxisMoveCommand command)
    {
        if (command.Velocity <= RotationalSpeed.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "旋转轴速度必须大于 0。");
        }

        if (command.Acceleration <= RotationalAcceleration.Zero || command.Deceleration <= RotationalAcceleration.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "旋转轴加速度和减速度必须大于 0。");
        }

        if (command.JerkDegreesPerSecondCubed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "旋转轴跃度必须大于 0。");
        }
    }
}
