# Process 层规则

Process 负责 IR、编译、仿真、运动规划、控制器程序生成和执行适配。它描述工艺如何变成可验证、可执行、可追溯的产物。

## 允许

- 定义不可变 IR、编译诊断、仿真 Trace、运动规划结果和控制器程序派生产物。
- 保留控制器方言差异，但必须通过 Emitter / DirectExecutor 边界表达。
- DirectExecutor / 执行适配可以接触 HAL 契约，用于把受控命令交给设备层。
- 使用 hash 关联源图纸、IR、仿真、控制器程序和执行回采。

## 禁止

- Compiler、Simulation、Planner、Emitter 不得依赖 HAL、PLC SDK 或厂家 SDK。
- 不得把 Beckhoff ADS、ACSPL+、PMAC 等具体协议写成通用 IR 语义。
- 不得让可编辑控制器程序绕过 IR 校验成为受控生产下发依据。
- 不得直接依赖 UI 或 Prism。

## 交互方式

- Drafting 生成或更新工艺语义后，通过 Process 编译为 IR / MotionPlan。
- Application 调用 Process 能力，并决定是否允许执行。
- TraceHub 负责高频数据广播，避免多个消费者竞争同一个入口 Channel。
- IR 或同步语义变更必须同步 `docs/5 Sync-Mechanism.md`。
