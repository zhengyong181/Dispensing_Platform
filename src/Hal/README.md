# HAL

硬件抽象层项目目录。这里承载控制器、IO、点胶、视觉等硬件能力的契约与实现。

## 已存在项目

- `DispensingPlatform.Hal.Contracts`：HAL 公共契约项目，只包含接口和通用数据模型。
- `DispensingPlatform.Hal.Beckhoff`：Beckhoff 控制器实现项目，内部通过 ADS 读写 PLC 变量并实现 Contracts 接口。
