using DispensingPlatform.Hal.Contracts.Axes;
using DispensingPlatform.Hal.Contracts.Connection;
using DispensingPlatform.Hal.Contracts.Dispense;
using DispensingPlatform.Hal.Contracts.Io;
using DispensingPlatform.Hal.Contracts.Machine;
using DispensingPlatform.Hal.Contracts.Program;

namespace DispensingPlatform.Hal.Contracts;

/// <summary>
/// PLC 控制器统一契约。
/// </summary>
/// <remarks>
/// 该接口把当前平台可用的 PLC 能力聚合为一个入口，便于上层注入和调度。
/// 具体厂家实现可在内部拆分模块，但对外应通过这些稳定子接口暴露能力。
/// </remarks>
public interface IPlcController :
    IAsyncDisposable,
    IHardwareConnection,
    IAxisController,
    IMachineStatusReader,
    IDigitalIoController,
    IAnalogIoController,
    INativeProgramController,
    IDispenseTriggerController;
