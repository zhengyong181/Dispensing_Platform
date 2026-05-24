# 文档 3 — 核心契约与接口骨架（Core-Contracts.md）

> 版本：v0.2 · 最后更新：2026-05-25

本文定义所有跨模块协作的"语言"：接口、数据模型、事件、错误码。这是项目里最稳定的部分，一旦定下来变更要慎重，因为任何修改都会影响所有上层。

本文不展开实现细节（实现散落在各项目），只定义"形状"。代码示例使用 C# 12 / .NET 8 语法。

---

## 1. 设计原则

### 1.1 接口先行

所有跨模块协作必须通过接口。新增功能时先定义接口，再实现。当前阶段接口放在对应分层的 `Contracts` 逻辑目录/命名空间里；只有该分层需要独立发布、强隔离或复用时，才按文档 2 拆成 `*.Contracts` 项目。

### 1.2 不可变值对象

数据模型默认使用 `record`（不可变）：

```csharp
public sealed record AxisPosition(string AxisId, Length Value, DateTimeOffset Timestamp);
```

理由：
- 跨线程传递安全（不需要锁）
- 序列化友好
- 表达"事件"和"快照"语义自然

可变状态只在服务实现内部封闭，不暴露到接口。

### 1.3 异步默认

所有 IO 操作（硬件、数据库、网络、文件）必须异步，命名以 `Async` 结尾，签名带 `CancellationToken`：

```csharp
Task<MotionResult> MoveAbsoluteAsync(AxisCommand cmd, CancellationToken ct = default);
```

CPU 密集计算（几何运算、编译）保持同步，由调用方决定是否包到 `Task.Run`。

### 1.4 取消令牌

所有长时间操作支持 `CancellationToken`。事件订阅返回 `IDisposable` 用于解订。

### 1.5 接口隔离

接口按使用场景拆分，不堆"上帝接口"。例如：

```csharp
// ❌ 不要
public interface IAxis {
    Task MoveAsync(...); Task HomeAsync(...); Task ConfigureAsync(...); ...
}

// ✅ 拆分
public interface IAxis : IAxisMotion, IAxisHoming, IAxisStatus { }
public interface IAxisMotion { Task MoveAsync(...); }
public interface IAxisHoming { Task HomeAsync(...); }
public interface IAxisStatus { AxisStatus Status { get; } IObservable<AxisStatus> Changes { get; } }
```

### 1.6 能力标志

不同硬件能力差异大，用 Capability Flags 声明而非接口区分：

```csharp
[Flags]
public enum MotionCapabilities {
    None            = 0,
    PositionSync    = 1 << 0,   // PSO
    OnTheFly        = 1 << 1,   // 飞行点胶
    BufferedMotion  = 1 << 2,   // 轨迹缓冲
    ElectronicCam   = 1 << 3,   // 电子凸轮
    Lookahead       = 1 << 4,   // 前瞻
    AbsoluteEncoder = 1 << 5,
}

public interface IControllerCapabilities {
    MotionCapabilities Motion { get; }
    bool Has(MotionCapabilities cap);
}
```

### 1.7 量纲类型

所有有物理量纲的值使用 UnitsNet 强类型，禁止用裸 `double` 表达长度、速度、压力、温度等：

```csharp
// ❌ 不要
public Task MoveAsync(double position, double velocity);

// ✅ 应该
public Task MoveAsync(Length position, Speed velocity);
```

例外：无量纲值或厂商原始量可以使用 `double`，但字段名或注释必须说明语义，例如相机增益 `gain`、视觉质量分数 `quality`、模拟量原始值 `rawValue`。已经完成量纲转换的模拟量应优先建模为具体 UnitsNet 类型或带单位描述的 reading。

---

## 2. HAL 接口

定义在 `DispensingPlatform.Hal.Contracts` 逻辑命名空间。当前它不要求必须是独立 `.csproj`；后续 HAL 契约稳定并需要独立发布时，可按文档 2 拆成候选项目。

### 2.1 IAxis（单轴）

```csharp
public interface IAxis {
    string Id { get; }
    AxisDescriptor Descriptor { get; }                  // 名称、单位、行程范围
    AxisStatus Status { get; }                          // 当前状态
    IObservable<AxisStatus> StatusChanges { get; }      // 状态流（Rx）
    IControllerCapabilities Capabilities { get; }

    Task EnableAsync(CancellationToken ct = default);
    Task DisableAsync(CancellationToken ct = default);
    Task HomeAsync(HomingMode mode, CancellationToken ct = default);
    Task<MotionResult> MoveAbsoluteAsync(Length target, MotionParameters p, CancellationToken ct = default);
    Task<MotionResult> MoveRelativeAsync(Length offset, MotionParameters p, CancellationToken ct = default);
    Task StopAsync(StopMode mode, CancellationToken ct = default);
    Task ResetAsync(CancellationToken ct = default);
}

public sealed record AxisStatus(
    Length Position,
    Speed Velocity,
    bool IsEnabled,
    bool IsMoving,
    bool IsHomed,
    bool HasError,
    int? ErrorCode,
    DateTimeOffset Timestamp);

public sealed record MotionParameters(
    Speed MaxVelocity,
    Acceleration MaxAcceleration,
    Acceleration MaxDeceleration,
    Jerk? MaxJerk = null);
```

#### 2.1.1 Beckhoff 当前落地约束（2026-05-25）

- 已落地项目：`src/Hal/DispensingPlatform.Hal.Beckhoff`（程序集 `DispensingPlatform.Hal.Beckhoff`）。
- 轴类型约束：仅 `Axis3` 为旋转轴；`Axis1/2/4/5/6/7/8/9/10` 全部按直线轴建模。
- 单位约束：直线轴使用 `Length/Speed/Acceleration/Jerk`，旋转轴使用 `Angle/RotationalSpeed/RotationalAcceleration`。
- PLC 符号映射基线：`Com_GVLS.arstAxis[*]`（运动轴）、`Com_GVLS.stSystem`（系统状态）、`Com_GVLS.stIJP`（NCI/点胶触发）、`Com_GVLS.stAIO`（模拟量）、`GVL_IO`（数字 IO）。
- PLC 变量清单：`configs/beckhoff/plc-symbol-map.yaml` 是现场调试、PLC 维护和 HAL 开发共同使用的变量索引；任何符号变动都必须同步该清单和 `src/Hal/DispensingPlatform.Hal.Beckhoff/PlcVariables/`。
- 实现拆分约束：`Connection` 只隔离 ADS 连接和 SDK，`PlcVariables` 只维护符号路径，`Axes` / `Machine` / `Io` / `Nci` / `Dispense` 只承载各自设备职责，`BeckhoffHal.cs` 只作为总入口转发调用。

### 2.2 IMotionGroup（多轴协调）

```csharp
public interface IMotionGroup {
    string Id { get; }
    IReadOnlyList<IAxis> Axes { get; }
    IControllerCapabilities Capabilities { get; }

    Task<MotionResult> MoveLinearAsync(Pose target, MotionParameters p, CancellationToken ct = default);
    Task<MotionResult> MoveArcAsync(ArcCommand cmd, MotionParameters p, CancellationToken ct = default);
    Task<MotionResult> ExecutePathAsync(IReadOnlyList<PathSegment> path, CancellationToken ct = default);
    Task StopAsync(StopMode mode, CancellationToken ct = default);
}
```

### 2.3 IMotionScript（预编译运动脚本下发）

```csharp
public interface IMotionScript {
    Task<ScriptHandle> LoadAsync(byte[] program, CancellationToken ct = default);
    Task ExecuteAsync(ScriptHandle handle, CancellationToken ct = default);
    Task<ScriptStatus> GetStatusAsync(ScriptHandle handle, CancellationToken ct = default);
    Task UnloadAsync(ScriptHandle handle, CancellationToken ct = default);
}
```

### 2.4 IDispenser（点胶阀）

```csharp
public interface IDispenser {
    string Id { get; }
    DispenserCapabilities Capabilities { get; }

    Task OpenAsync(DispenseParameters p, CancellationToken ct = default);
    Task CloseAsync(CancellationToken ct = default);
    Task PulseAsync(TimeSpan duration, DispenseParameters p, CancellationToken ct = default);
    Task PurgeAsync(TimeSpan duration, CancellationToken ct = default);
    Task SetPressureAsync(Pressure p, CancellationToken ct = default);
    Task SetTemperatureAsync(Temperature t, CancellationToken ct = default);

    DispenserStatus Status { get; }
    IObservable<DispenserStatus> StatusChanges { get; }
}

public sealed record DispenseParameters(
    Pressure Pressure,
    TimeSpan OpenDelay,
    TimeSpan CloseDelay,
    Temperature? Temperature = null);
```

### 2.5 ICamera（工业相机）

```csharp
public interface ICamera {
    string Id { get; }
    CameraDescriptor Descriptor { get; }
    CameraStatus Status { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<IFrame> GrabSingleAsync(CancellationToken ct = default);
    IDisposable StartContinuous(IObserver<IFrame> observer, GrabSettings settings);
    Task SetExposureAsync(TimeSpan exposure, CancellationToken ct = default);
    Task SetGainAsync(double gain, CancellationToken ct = default);
    Task TriggerSoftwareAsync(CancellationToken ct = default);
}

public interface IFrame : IDisposable {
    int Width { get; }
    int Height { get; }
    PixelFormat Format { get; }
    DateTimeOffset Timestamp { get; }
    long FrameId { get; }
    ReadOnlyMemory<byte> Pixels { get; }
    // 可转换为 OpenCvSharp Mat / SkiaSharp SKBitmap
}
```

### 2.6 IIoModule / 数字与模拟 IO

```csharp
public interface IIoModule {
    string Id { get; }
    IReadOnlyList<IDigitalInput> DigitalInputs { get; }
    IReadOnlyList<IDigitalOutput> DigitalOutputs { get; }
    IReadOnlyList<IAnalogInput> AnalogInputs { get; }
    IReadOnlyList<IAnalogOutput> AnalogOutputs { get; }
}

public interface IDigitalInput {
    string Id { get; }
    bool Value { get; }
    IObservable<bool> Changes { get; }
}

public interface IDigitalOutput {
    string Id { get; }
    bool Value { get; }
    Task SetAsync(bool value, CancellationToken ct = default);
    Task PulseAsync(TimeSpan duration, CancellationToken ct = default);
}

public interface IAnalogInput {
    string Id { get; }
    double RawValue { get; }                           // 厂商 SDK / PLC 暴露的原始值
    string EngineeringUnit { get; }                    // 如 V / mA / kPa；需要强量纲时由上层转换为具体 Reading
    IObservable<double> RawChanges { get; }
}
```

### 2.7 ISensor（高度 / 线扫激光 / 光谱共聚焦）

```csharp
public interface ISensor {
    string Id { get; }
    SensorDescriptor Descriptor { get; }
}

public interface ISensor<TReading> : ISensor where TReading : ISensorReading {
    Task<TReading> ReadAsync(CancellationToken ct = default);
    IDisposable Subscribe(IObserver<TReading> observer, SensorSubscription sub);
}

public interface ISensorReading {
    DateTimeOffset Timestamp { get; }
    bool IsValid { get; }
}

public sealed record HeightReading(Length Height, double Quality, DateTimeOffset Timestamp, bool IsValid) : ISensorReading;
public sealed record LineProfileReading(IReadOnlyList<Length> Profile, Length Pitch, DateTimeOffset Timestamp, bool IsValid) : ISensorReading;
```

### 2.8 IHeater / IPressureRegulator

```csharp
public interface IHeater {
    string Id { get; }
    Temperature SetPoint { get; }
    Temperature Current { get; }
    Task SetSetPointAsync(Temperature target, CancellationToken ct = default);
    Task EnableAsync(CancellationToken ct = default);
    Task DisableAsync(CancellationToken ct = default);
    IObservable<Temperature> Readings { get; }
}

public interface IPressureRegulator {
    string Id { get; }
    Pressure SetPoint { get; }
    Pressure Current { get; }
    Task SetSetPointAsync(Pressure target, CancellationToken ct = default);
    IObservable<Pressure> Readings { get; }
}
```

### 2.9 ISafetyController

```csharp
public interface ISafetyController {
    SafetyStatus Status { get; }
    IObservable<SafetyEvent> Events { get; }
    Task ResetAsync(CancellationToken ct = default);
}

public sealed record SafetyStatus(
    bool EmergencyStop,
    bool SafetyDoorClosed,
    bool LightCurtainOk,
    bool AirPressureOk);

public sealed record SafetyEvent(SafetyEventType Type, string Source, DateTimeOffset Timestamp);
```

### 2.10 能力查询

```csharp
public interface IControllerCapabilities {
    MotionCapabilities Motion { get; }
    bool Has(MotionCapabilities cap);
}
```

---

## 3. Device 聚合层

把多个 HAL 组合成"逻辑设备"。注意：凡是直接暴露 `IDispenser`、`IHeater`、`IMotionGroup` 等 HAL 接口的聚合接口，归属实现侧的 Device / Service Abstractions，不放入纯 `Core.Contracts`。`Core.Contracts` 只暴露给 UI 和跨模块使用的设备快照、设备 id、能力描述，避免核心契约被硬件接口牵引。

### 3.1 IDispensingHead

```csharp
public interface IDispensingHead {
    string Id { get; }
    IDispenser Valve { get; }
    IHeater? Heater { get; }
    IPressureRegulator? Pressure { get; }
    ISensor<HeightReading>? HeightSensor { get; }
    ToolDescriptor CurrentTool { get; }                 // 当前针头

    Task ChangeToolAsync(ToolDescriptor tool, CancellationToken ct = default);
    Task PurgeAsync(PurgeStrategy strategy, CancellationToken ct = default);
}
```

### 3.2 IStation（工位）

```csharp
public interface IStation {
    string Id { get; }
    StationKind Kind { get; }                           // Effector / Substrate
    IMotionGroup Motion { get; }
    IDispensingHead? Head { get; }                      // Effector 工位才有
    ICamera? Camera { get; }
    IReadOnlyList<ISensor> Sensors { get; }
    StationState State { get; }                         // 工位级状态机当前状态
    IObservable<StationState> StateChanges { get; }
}

public enum StationKind { Effector, Substrate }
```

### 3.3 IWorkspaceArbiter

```csharp
public interface IWorkspaceArbiter {
    Task<IRegionLease> AcquireAsync(string stationId, Region region, CancellationToken ct = default);
    bool TryAcquire(string stationId, Region region, out IRegionLease? lease);
}

public interface IRegionLease : IAsyncDisposable {
    string StationId { get; }
    Region Region { get; }
}
```

通用资源仲裁泛化为：

```csharp
public interface IResourceArbiter<TResource> where TResource : notnull {
    Task<IResourceLease<TResource>> AcquireAsync(string holder, TResource resource, CancellationToken ct = default);
}

public interface IResourceLease<out TResource> : IAsyncDisposable where TResource : notnull {
    string Holder { get; }
    TResource Resource { get; }
    DateTimeOffset AcquiredAt { get; }
}
```

---

## 4. 服务接口

定义在 `Core.Contracts` 逻辑命名空间，由 `Core.Services` 逻辑区域实现。是否拆成独立项目，以文档 2 的按需项目化规则为准。

### 4.1 IMotionService

```csharp
public interface IMotionService {
    IReadOnlyList<AxisDescriptor> Axes { get; }
    IReadOnlyList<MotionGroupDescriptor> Groups { get; }

    Task HomeAllAsync(CancellationToken ct = default);
    Task<MotionResult> JogAsync(string axisId, JogCommand cmd, CancellationToken ct = default);
    Task EmergencyStopAsync();

    SafetyZoneRegistry SafetyZones { get; }
    CoordinateFrameRegistry Frames { get; }
}
```

### 4.2 IVisionService

```csharp
public interface IVisionService {
    IReadOnlyList<ICamera> Cameras { get; }
    Task<LocateResult> LocateAsync(string cameraId, IVisionAlgorithm algo, IFrame? frame = null, CancellationToken ct = default);
    Task<InspectionResult> InspectAsync(string cameraId, IInspectionAlgorithm algo, CancellationToken ct = default);
}

public interface IVisionAlgorithm {
    string Name { get; }
    AlgorithmKind Kind { get; }                        // Mark / Edge / Circle / 自定义
}
```

### 4.3 ICalibrationService

```csharp
public interface ICalibrationService {
    Transform GetTransform(CoordinateFrame from, CoordinateFrame to);
    Task SaveCalibrationAsync(string name, CalibrationData data, CancellationToken ct = default);
    Task<CalibrationData?> LoadCalibrationAsync(string name, CancellationToken ct = default);
    IObservable<CalibrationChangedEvent> Changes { get; }
}

public sealed record Transform(Matrix4x4 Matrix, CoordinateFrame From, CoordinateFrame To, DateTimeOffset CalibratedAt);

public enum CoordinateFrame {
    Machine, Workpiece, Tool, Camera, Pixel, User1, User2, User3
}
```

### 4.4 IRecipeService

```csharp
public interface IRecipeService {
    Task<RecipeId> CreateAsync(Recipe recipe, CancellationToken ct = default);
    Task<Recipe> LoadAsync(RecipeId id, CancellationToken ct = default);
    Task<RecipeId> SaveNewVersionAsync(RecipeId id, Recipe recipe, string comment, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeMetadata>> ListAsync(RecipeFilter filter, CancellationToken ct = default);
    Task ImportAsync(Stream source, RecipeFormat format, CancellationToken ct = default);
    Task ExportAsync(RecipeId id, Stream target, RecipeFormat format, CancellationToken ct = default);
}
```

### 4.5 IProcessService

```csharp
public interface IProcessService {
    Task<JobHandle> StartJobAsync(JobRequest request, CancellationToken ct = default);
    Task PauseJobAsync(JobHandle handle, PauseMode mode, CancellationToken ct = default);
    Task ResumeJobAsync(JobHandle handle, CancellationToken ct = default);
    Task StopJobAsync(JobHandle handle, CancellationToken ct = default);
    JobStatus GetStatus(JobHandle handle);
    IObservable<JobEvent> Events { get; }
}
```

### 4.6 IAlarmService

```csharp
public interface IAlarmService {
    AlarmHandle Raise(AlarmDefinition definition, AlarmContext ctx);
    Task AcknowledgeAsync(AlarmHandle handle, string user, CancellationToken ct = default);
    Task ClearAsync(AlarmHandle handle, string user, CancellationToken ct = default);
    IReadOnlyList<ActiveAlarm> Active { get; }
    IObservable<AlarmEvent> Events { get; }
}

public sealed record AlarmDefinition(
    string Code,                                       // ALM-MOTION-0023
    AlarmSeverity Severity,                            // Fatal / Critical / Warning / Info
    AlarmCategory Category,
    string TitleKey,                                   // i18n key
    string DescriptionKey,
    AlarmRecoverability Recoverability);

public enum AlarmSeverity { Fatal, Critical, Warning, Info }
public enum AlarmRecoverability { SelfRecover, OperatorClear, EngineerClear, Unrecoverable }
```

### 4.7 IAuditLogger

```csharp
public interface IAuditLogger {
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<AuditEntry> QueryAsync(AuditQuery query, CancellationToken ct = default);
}

public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    string User,
    string Action,                                     // RecipeModified / ParameterChanged / ...
    string Target,                                     // 受影响的对象 ID
    JsonDocument? OldValue,
    JsonDocument? NewValue,
    string? Source,                                    // 发起来源（IP / 工位）
    string? Comment);
```

### 4.8 ITraceService（高频数据通道）

```csharp
public interface ITraceService {
    void RegisterChannel(TraceChannelDescriptor descriptor);
    IDisposable Subscribe<T>(string channelName, IObserver<T> observer) where T : ITracePoint;
    Task<IReadOnlyList<T>> QueryAsync<T>(TraceQuery query, CancellationToken ct = default) where T : ITracePoint;
}

public sealed record TraceChannelDescriptor(
    string Name,                                       // motion.axis_x.position
    Type PointType,
    Frequency NominalRate,
    PersistencePolicy Persistence);                    // 内存 / Parquet / 双写

public interface ITracePoint {
    DateTimeOffset Timestamp { get; }                  // PLC 周期时间戳，不是上位机收到时间
    long SequenceId { get; }                           // 用于丢包检测
}
```

`Subscribe` 是广播语义：每个订阅者都应收到自己的数据副本或独立读取通道。实现时可以用入口 `Channel<T>` 承接采集回调，但不能让多个消费者竞争读取同一个 `Channel<T>`。

### 4.9 IShutdownCoordinator

```csharp
public interface IShutdownCoordinator {
    void Register(IShutdownParticipant participant);
    Task ShutdownAsync(ShutdownReason reason, CancellationToken ct = default);
}

public interface IShutdownParticipant {
    int Order { get; }                                 // 越小越先停
    string Name { get; }
    Task OnShutdownAsync(ShutdownReason reason, CancellationToken ct);
}
```

### 4.10 IPermissionService

```csharp
public interface IPermissionService {
    UserSession? CurrentUser { get; }
    bool HasPermission(Permission permission);
    Task<UserSession> SignInAsync(string username, string password, CancellationToken ct = default);
    Task SignOutAsync(CancellationToken ct = default);
    IObservable<PermissionChangedEvent> Changes { get; }
}

public enum UserRole { Operator, Engineer, Administrator, ServiceTechnician }
```

### 4.11 IThemeService

```csharp
public interface IThemeService {
    ThemeId Current { get; }
    IReadOnlyList<ThemeId> Available { get; }
    void Apply(ThemeId id);
    IObservable<ThemeChangedEvent> Changes { get; }
}
```

### 4.12 IDialogService（UI 层适配）

Core.Contracts 不直接依赖 Prism。UI 层可定义自己的对话服务接口，并由 Prism `IDialogService` 适配实现，避免核心契约泄漏具体 UI 框架：

```csharp
public interface IAppDialogService {
    Task<DialogResult> ConfirmAsync(ConfirmDialogRequest request, CancellationToken ct = default);
    Task<DialogResult<T?>> InputAsync<T>(InputDialogRequest<T> request, CancellationToken ct = default);
    Task ShowProgressAsync(ProgressDialogRequest request, CancellationToken ct = default);
}
```

---

## 5. 中间表示（IR）数据模型

定义在 `DispensingPlatform.Process.Ir` 逻辑命名空间。这是**系统里最稳定的数据模型**，画布、仿真、控制器程序、下发、回采五者共同的源头。当前不要求它必须是独立项目；当 IR schema 需要强版本控制时，再按文档 2 拆分。详细派生关系见文档 5《同步机制设计》。

### 5.1 顶层结构

```csharp
public sealed record MotionPlan(
    MotionPlanId Id,                                   // 含 hash 链
    MotionPlanHeader Header,
    IReadOnlyList<Segment> Segments,
    MotionPlanMetadata Metadata);

public sealed record MotionPlanId(
    Guid Value,
    string SourceHash,                                 // 源画布文档的 hash
    string IrHash,                                     // 此 IR 自身的 hash
    int SchemaVersion);

public sealed record MotionPlanHeader(
    LengthUnit LengthUnit,
    AngleUnit AngleUnit,
    CoordinateFrame Frame,                             // 输出坐标系
    string MachineModelId,
    string? RecipeName,
    string? RecipeVersion);

public sealed record MotionPlanMetadata(
    DateTimeOffset GeneratedAt,
    string GeneratedBy,                                // 用户名 / 系统名
    string CompilerVersion,
    string? Signature);                                // 预留电子签名
```

### 5.2 段（Segment）类型

所有段实现 `Segment` 抽象基础（用 record 继承）：

```csharp
public abstract record Segment(int Index, string? Tag);

// 运动段
public abstract record MoveSegment(
    int Index, string? Tag,
    Pose Start, Pose End,
    Speed Feedrate,
    Acceleration Acceleration,
    Acceleration Deceleration,
    Length? Tolerance,
    TransitionPolicy? TransitionIn,
    TransitionPolicy? TransitionOut) : Segment(Index, Tag);

// 下列派生 record 使用省略号表示字段与基类字段继承关系，不能直接复制编译；
// 落地代码必须写出完整主构造函数或改用 required init 属性。
public sealed record RapidMove(...) : MoveSegment(...);             // 快速定位 G0
public sealed record LinearMove(...) : MoveSegment(...);            // 直线插补 G1
public sealed record ArcMove(...) : MoveSegment(...) {              // 圆弧插补 G2/G3
    public required Point2D Center { get; init; }
    public required ArcDirection Direction { get; init; }
}
public sealed record SplineMove(...) : MoveSegment(...) {           // 样条
    public required IReadOnlyList<Pose> ControlPoints { get; init; }
    public required SplineKind Kind { get; init; }
}

// 工艺段
public abstract record ProcessSegment(int Index, string? Tag) : Segment(Index, Tag);

public sealed record DispenseOn(int Index, string? Tag, DispenseParameters Params) : ProcessSegment(Index, Tag);
public sealed record DispenseOff(int Index, string? Tag) : ProcessSegment(Index, Tag);
public sealed record Purge(int Index, string? Tag, TimeSpan Duration, DispenseParameters Params) : ProcessSegment(Index, Tag);
public sealed record Wait(int Index, string? Tag, TimeSpan Duration) : ProcessSegment(Index, Tag);
public sealed record SetPressure(int Index, string? Tag, Pressure Value) : ProcessSegment(Index, Tag);
public sealed record SetTemperature(int Index, string? Tag, Temperature Value) : ProcessSegment(Index, Tag);

// 辅助段
public sealed record ToolChange(int Index, string? Tag, ToolDescriptor Tool) : Segment(Index, Tag);
public sealed record VisionTrigger(int Index, string? Tag, string CameraId, string AlgorithmId) : Segment(Index, Tag);
public sealed record IoOperation(int Index, string? Tag, string IoId, IoOpKind Op, double? Value) : Segment(Index, Tag);

// 同步原语
public sealed record Barrier(int Index, string? Tag, string Reason) : Segment(Index, Tag);
public sealed record Trigger(int Index, string? Tag, TriggerCondition Condition, IReadOnlyList<Segment> Actions) : Segment(Index, Tag);
```

### 5.3 拐角策略

```csharp
public sealed record TransitionPolicy(
    TransitionKind Kind,                               // Sharp / Rounded / Lookahead
    Length? RoundingRadius,
    Speed? CornerSpeed);

public enum TransitionKind { Sharp, Rounded, Lookahead }
```

### 5.4 触发条件（用于 PSO 飞行点胶）

```csharp
public abstract record TriggerCondition;
public sealed record PositionTrigger(string AxisId, Length AtPosition, TriggerEdge Edge) : TriggerCondition;
public sealed record TimeTrigger(TimeSpan AtElapsed) : TriggerCondition;
public sealed record IoTrigger(string IoId, bool RisingEdge) : TriggerCondition;
```

### 5.5 不可变性与 Hash 链

`MotionPlan` 与所有 Segment 都是不可变 record。`IrHash` 在创建时计算（基于规范化 JSON 的 SHA-256），用于验证完整性与追溯。

计算 `IrHash` 时必须把 `MotionPlanId.IrHash` 自身视为空值或排除在规范化 JSON 之外，避免"hash 包含自己"的循环。推荐流程：先构造 `IrHash = ""` 的 canonical JSON → 计算 SHA-256 → 回填 `IrHash`。

派生产物（仿真轨迹、G 代码、下发指令）必须携带 `IrHash`，UI 通过比对 hash 判断是否"过期"。

### 5.6 序列化

默认使用 `System.Text.Json`，自定义 `JsonConverter` 处理 record 继承与 UnitsNet 量纲。每个版本提供 schema：

```
schemas/motion-plan.v1.schema.json
```

升级 schema 走严格的版本流程，旧文档自动迁移。

---

## 6. 事件总线契约

### 6.1 命名约定

- 事件 record 用过去式：`AxisHomedEvent`、`AlarmRaisedEvent`、`RecipeSavedEvent`
- 集中放在 `Core.Contracts/Events/`，按子领域分文件夹
- 事件 payload 不可变（record）
- 事件**不携带可变引用**（不要传 IAxis，传 record 化的快照）

### 6.2 关键事件清单（V1）

```csharp
// 运动
public sealed record AxisHomedEvent(string AxisId, DateTimeOffset At);
public sealed record AxisMotionStartedEvent(string AxisId, MotionParameters Params, DateTimeOffset At);
public sealed record AxisMotionCompletedEvent(string AxisId, AxisStatus FinalStatus, DateTimeOffset At);

// 工艺
public sealed record JobStartedEvent(JobHandle Job, RecipeId Recipe, DateTimeOffset At);
public sealed record JobCompletedEvent(JobHandle Job, JobOutcome Outcome, DateTimeOffset At);
public sealed record StationStateChangedEvent(string StationId, StationState From, StationState To, DateTimeOffset At);
public sealed record SegmentExecutedEvent(string StationId, int SegmentIndex, ExecutionResult Result, DateTimeOffset At);

// 视觉
public sealed record VisionLocatedEvent(string CameraId, LocateResult Result, DateTimeOffset At);

// 报警与审计
public sealed record AlarmRaisedEvent(AlarmHandle Handle, AlarmDefinition Definition, AlarmContext Context);
public sealed record AlarmAcknowledgedEvent(AlarmHandle Handle, string User, DateTimeOffset At);
public sealed record AlarmClearedEvent(AlarmHandle Handle, string User, DateTimeOffset At);
public sealed record AuditLoggedEvent(AuditEntry Entry);

// 配方与配置
public sealed record RecipeSavedEvent(RecipeId Id, string Comment, DateTimeOffset At);
public sealed record RecipeActivatedEvent(RecipeId Id, DateTimeOffset At);
public sealed record CalibrationChangedEvent(string Name, DateTimeOffset At);

// 系统
public sealed record SystemStateChangedEvent(SystemState From, SystemState To, DateTimeOffset At);
public sealed record ShutdownStartedEvent(ShutdownReason Reason, DateTimeOffset At);
```

### 6.3 订阅与解订

事件总线对外暴露本项目自己的轻量包装，确保解订模式统一。Prism 只用于 UI 组织，不作为业务事件总线；Core.Contracts、Core.Infrastructure、Application、Process、HAL、Drafting 都不能暴露或依赖 Prism 类型：

```csharp
public enum EventDispatchThread {
    PublisherThread,
    BackgroundThread,
    UIThread
}

public interface IEventBus {
    IDisposable Subscribe<TEvent>(
        Action<TEvent> handler,
        EventDispatchThread thread = EventDispatchThread.UIThread);

    void Publish<TEvent>(TEvent ev);
}
```

订阅者必须 `Dispose` 或在 ViewModel 析构时解订。

---

## 7. 配置契约

定义在 `Core.Contracts/Configuration/`。

### 7.1 IMachineConfiguration

```csharp
public interface IMachineConfiguration {
    string CustomerId { get; }
    string ModelId { get; }
    string SchemaVersion { get; }
    IHardwareDescriptor Hardware { get; }
    IModuleManifest Modules { get; }
    IUiLayoutDescriptor UiLayout { get; }
    IBrandingDescriptor Branding { get; }
}
```

### 7.2 IHardwareDescriptor

```csharp
public interface IHardwareDescriptor {
    IReadOnlyList<ControllerDescriptor> Controllers { get; }
    IReadOnlyList<CameraDescriptor> Cameras { get; }
    IReadOnlyList<DispenserDescriptor> Dispensers { get; }
    IReadOnlyList<SensorDescriptor> Sensors { get; }
    IReadOnlyList<IoModuleDescriptor> IoModules { get; }
}

public sealed record ControllerDescriptor(
    string Id,
    ControllerType Type,                               // Motion / Safety
    string Vendor,                                     // Beckhoff / ACS / Pmac
    string AssemblyName,                               // 用于动态加载
    JsonDocument ConnectionParameters,                 // 厂家专有
    IReadOnlyList<AxisDescriptor> Axes);
```

### 7.3 IModuleManifest

```csharp
public interface IModuleManifest {
    IReadOnlyList<ModuleEntry> Enabled { get; }
}

public sealed record ModuleEntry(
    string Name,                                       // Drafting / Recipe / ...
    string AssemblyName,
    int LoadOrder,
    JsonDocument? ConfigOverride);
```

### 7.4 配置校验

启动时通过 JSON Schema 校验：

```csharp
public interface IConfigurationValidator {
    ConfigurationValidationResult Validate(IMachineConfiguration config);
}
```

校验失败必须显示明确错误页，不允许进入主界面。

---

## 8. 错误处理契约

### 8.1 异常分类

```csharp
public abstract class DispensingException : Exception {
    public string ErrorCode { get; }                   // 如 ERR-MOTION-0042
    public ErrorSeverity Severity { get; }
    public ErrorRecoverability Recoverability { get; }
    public IReadOnlyDictionary<string, object?> Context { get; }
}

public class HardwareException : DispensingException { }       // 硬件层
public class ConfigurationException : DispensingException { }  // 配置层
public class CompilationException : DispensingException { }    // IR 编译失败
public class RecoverableJobException : DispensingException { } // 业务可恢复
public class FatalJobException : DispensingException { }       // 业务致命
public class UserCancelledException : DispensingException { }  // 用户取消

public enum ErrorSeverity { Info, Warning, Error, Fatal }
public enum ErrorRecoverability { Retry, Skip, Abort }
```

### 8.2 错误码规范

格式：`<分类>-<子系统>-<编号>`

- 分类：`ERR`（错误）/ `ALM`（报警）/ `WRN`（警告）
- 子系统：`MOTION` / `VISION` / `DISPENSER` / `RECIPE` / `STATE` / `IO` / `SYS` / `CONFIG`
- 编号：4 位整数

示例：`ALM-MOTION-0023`、`ERR-CONFIG-0001`、`WRN-VISION-0102`

错误码集中在 `Core.Contracts/Errors/ErrorCodes.cs`，每个码对应一个常量 + i18n key。

### 8.3 错误码到报警的映射

```csharp
public interface IAlarmCodeMapper {
    AlarmDefinition? Map(string errorCode);
}
```

底层抛错 → 中间层捕获 → `IAlarmCodeMapper` 找到对应 `AlarmDefinition` → `IAlarmService.Raise(...)`。

---

## 9. 单位与量纲

### 9.1 UnitsNet 使用约定

- 长度：`Length`（默认 mm）
- 速度：`Speed`（默认 mm/s）
- 加速度：`Acceleration`（默认 mm/s²）
- 加加速度：`Jerk`（默认 mm/s³）
- 角度：`Angle`（默认 deg，UI 显示）
- 压力：`Pressure`（默认 kPa）
- 温度：`Temperature`（默认 °C）
- 时间：`Duration`（默认 ms）
- 频率：`Frequency`（默认 Hz）

### 9.2 量纲在接口中的表达

接口签名一律使用 UnitsNet 类型表达有量纲的物理量，禁止裸 double。无量纲值或厂商原始值按 §1.7 的例外规则处理。

### 9.3 序列化与显示

- 序列化：JSON 中以 `{"value": 12.5, "unit": "mm"}` 形式
- 显示：UI 层根据用户偏好选择显示单位（mm / inch / um 等）
- 计算：内部统一使用 SI 单位

### 9.4 Pose 与 Coordinate

```csharp
public sealed record Point2D(Length X, Length Y);
public sealed record Point3D(Length X, Length Y, Length Z);

public sealed record Pose(
    Length X, Length Y, Length Z,
    Angle? RotZ = null,                                // 绕 Z 轴旋转，可选
    CoordinateFrame Frame = CoordinateFrame.Workpiece);

public sealed record Region(Point2D Min, Point2D Max);
```

---

## 10. 测试契约

### 10.1 仿真接口契约

`Hal.Simulator` 必须实现完整的 HAL 接口，并提供"测试编排"扩展接口：

```csharp
public interface ISimulationControl {
    void InjectFault(string deviceId, FaultKind kind, TimeSpan duration);
    void SetSensorValue<T>(string sensorId, T value) where T : ISensorReading;
    void TriggerSafetyEvent(SafetyEventType type);
    void AdvanceTime(TimeSpan delta);                  // 时间扭曲，加速测试
    SimulationSnapshot Capture();
    void Restore(SimulationSnapshot snapshot);
}
```

### 10.2 Mock 友好

所有服务接口必须能被 Mock 框架（NSubstitute）轻松替换。具体规则：

- 接口纯方法签名，不依赖具体实现类
- 不在接口中暴露静态成员
- 异步签名优先（更易 stub）
- 事件流返回 `IObservable<T>`，方便用 `Subject<T>` 替换

### 10.3 HAL 一致性测试套

每个 HAL 实现必须通过 HAL 仿真一致性测试套；该测试套当前可放在已有测试项目中，后续 HAL 项目化后可独立为 `Hal.Simulator.Tests`。它要确保：

- 接口语义统一
- 状态转移合规
- 异常类型规范
- 事件流时序正确

新增 HAL 厂家实现 = 跑通这套测试 + 厂家专有测试。

### 10.4 端到端集成测试

`DispensingPlatform.Integration.Tests` 提供端到端用例：

- DXF 导入 → 编译 → 仿真 → 发码 → Simulator 执行 → 真机回采（仿真版）→ 比对
- 状态机断电恢复
- 报警闭环
- 配方版本回归

每个用例使用标准的"测试场景"对象，可重放。

### 10.5 架构测试

架构测试用 NetArchTest 强制依赖方向；测试项目按文档 2 的测试策略按需创建，候选名称为 `DispensingPlatform.Architecture.Tests`。

---

## 附录 A — 逻辑命名空间布局速查

以下名称是逻辑命名空间布局，不代表当前阶段必须存在同名 `.csproj`。

```
DispensingPlatform.Hal.Contracts
├─ Motion/                  # IAxis, IMotionGroup, IMotionScript
├─ Vision/                  # ICamera, IFrame
├─ Io/                      # IIoModule, IDigitalInput, ...
├─ Process/                 # IDispenser, IHeater, IPressureRegulator
├─ Sensors/                 # ISensor<T>, HeightReading, ...
├─ Safety/                  # ISafetyController
└─ Capabilities/            # IControllerCapabilities, MotionCapabilities

DispensingPlatform.Core.Contracts
├─ Devices/                 # IDispensingHead, IStation
├─ Services/                # IMotionService, IVisionService, ...
├─ Process/                 # IProcessService, JobHandle, ...
├─ Calibration/             # ICalibrationService, Transform, ...
├─ Recipes/                 # IRecipeService, Recipe, ...
├─ Alarms/                  # IAlarmService, AlarmDefinition, ...
├─ Audit/                   # IAuditLogger, AuditEntry
├─ Trace/                   # ITraceService, ITracePoint
├─ Permissions/             # IPermissionService, UserRole
├─ Theme/                   # IThemeService, ThemeId
├─ Lifecycle/               # IShutdownCoordinator, ...
├─ Configuration/           # IMachineConfiguration, ...
├─ Errors/                  # ErrorCodes, DispensingException, ...
└─ Events/                  # 所有跨模块事件 record

DispensingPlatform.Process.Ir
├─ MotionPlan.cs
├─ Segments/                # 所有 Segment record
├─ Triggers/                # TriggerCondition
├─ Transition/              # TransitionPolicy
└─ Serialization/           # JsonConverter, Schema
```

---

## 附录 B — 接口稳定性等级

| 等级 | 含义 | 示例 |
|------|------|------|
| 极高 | 变更影响全局，需 ADR + 所有实现协调升级 | `Hal.Contracts` 全部、`Process.Ir` 全部 |
| 高 | 变更影响多模块，需 ADR | `Core.Contracts` 服务接口、事件 |
| 中 | 变更影响相关模块 | 配置接口、错误码 |
| 低 | 变更影响有限 | 单 Module 内部接口（不在本文档） |

---

## 附录 C — 参考文档

- 文档 1：[1 Architecture.md](./1%20Architecture.md) — 接口在分层中的位置
- 文档 2：[2 Solution-Structure.md](./2%20Solution-Structure.md) — 接口所在项目的依赖矩阵
- 文档 4：[4 Drafting-Subsystem.md](./4%20Drafting-Subsystem.md) — 编辑器引用的 IRecipeService / ICalibrationService 等
- 文档 5：[5 Sync-Mechanism.md](./5%20Sync-Mechanism.md) — IR 数据模型的派生关系详解
- 文档 6：[6 StateMachine-Design.md](./6%20StateMachine-Design.md) — 状态机相关接口的语义
- 文档 7：[7 Data-Persistence.md](./7%20Data-Persistence.md) — IAuditLogger / ITraceService 的持久化实现
- 文档 8：[8 Design-System.md](./8%20Design-System.md) — IThemeService 的实现规约
- 文档 9：[9 Config-Multitenancy.md](./9%20Config-Multitenancy.md) — IMachineConfiguration 等配置接口的来源
- 文档 10：[10 DevOps.md](./10%20DevOps.md) — 接口稳定性等级如何影响变更流程
