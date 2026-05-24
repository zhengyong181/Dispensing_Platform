# DispensingPlatform.Hal.Beckhoff

本项目是 Beckhoff 控制器的 HAL 实现。对外通过 `DispensingPlatform.Hal.Contracts` 暴露通用接口，对内通过 ADS 与 PLC 变量通信。

## 目录职责

```text
DispensingPlatform.Hal.Beckhoff/
├─ BeckhoffHal.cs                  # IPlcController 总入口实现
├─ Axes/                           # 轴目录、状态读取、定位与轴控制逻辑
├─ Connection/                     # ADS 参数、SDK 隔离接口和 PLC 访问封装
├─ Dispense/                       # 点胶阀与高压触发位写入
├─ Io/                             # 数字 IO 与模拟量原始通道读写
├─ Machine/                        # 整机状态快照读取
├─ Nci/                            # 原生程序命令位写入
├─ PlcVariables/                   # PLC 变量路径集中映射
└─ Properties/                     # 程序集设置（如测试可见性）
```

## 维护规则

- PLC 变量改名、移动、删除或新增时，优先更新 `PlcVariables/` 映射代码，再同步 `configs/beckhoff/plc-symbol-map.yaml`。
- 轴类型和 PLC 序号映射只在 `Axes/BeckhoffAxisCatalog.cs` 维护；当前只有 Axis3 是旋转轴。
- 会改变设备状态的写入方法必须保留 `CancellationToken`，并通过 `Connection/BeckhoffPlcAccessor.cs` 统一执行连接、超时、取消和日志处理。
