# Application 层规则

Application 负责编排用例、状态机、调度、资源仲裁和安全流程。它表达“什么时候允许做什么”，不表达厂家 SDK 细节。

## 允许

- 编排状态机、任务调度、资源锁、生产流程、恢复流程和权限检查。
- 通过接口调用 Core、Process、Service / Device 抽象。
- 将 UI 命令转化为受控的应用用例。
- 记录关键状态变化、报警、审计和可恢复上下文。

## 禁止

- 不得引用 WPF、Prism 或 UI 控件库。
- 不得引用具体 HAL 实现或厂家 SDK。
- 不得绕过状态机直接执行运动、PLC、IO 或点胶动作。
- 不得把客户专属分支写入主线流程。

## 交互方式

- UI 只能通过 Application 暴露的命令、查询或门面进入业务流程。
- Application 通过契约调用 Process 和设备服务，不直接 new 具体实现。
- 涉及机器状态、报警、恢复的变更必须同步 `docs/6 StateMachine-Design.md`。
