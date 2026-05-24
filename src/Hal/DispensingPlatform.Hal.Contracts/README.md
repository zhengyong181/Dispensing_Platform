# DispensingPlatform.Hal.Contracts

本项目承载 HAL 公共契约，只定义接口与通用数据模型，不包含任何厂家 SDK 依赖和具体通信实现。

## 设计约束

- 仅保留硬件能力语义，不暴露 ADS、寄存器地址、PLC 结构体路径等实现细节。
- 物理量优先使用 UnitsNet 强类型，避免无单位裸数在跨层传递中产生歧义。
- 所有接口保持异步并支持 `CancellationToken`，确保上层可中断长时硬件调用。
