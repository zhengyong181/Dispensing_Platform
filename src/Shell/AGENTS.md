# Shell 层规则

Shell 是 WPF 启动入口和 UI 主壳，只负责应用生命周期和界面装配。

## 允许

- 使用 WPF、Prism、DesignSystem 资源组织主窗口、Region、导航、对话框和启动流程。
- 装配 DI、日志、配置加载、模块清单和全局异常入口。
- 通过 Application 门面或查询服务发起用户命令、读取状态和展示报警。

## 禁止

- 不得承载业务流程、工艺算法、运动控制、PLC / IO 操作或持久化细节。
- 不得直接引用 HAL 实现、硬件 SDK、Process 内部实现或 Drafting 算法实现。
- 不得把 Prism 类型暴露到非 UI 层契约中。
- 不得让 Shell 知道具体客户专属模块的内部实现。

## 交互方式

- 命令型操作进入 Application。
- 只读状态通过 Service / Query facade、事件总线或状态订阅获得。
- 模块通过配置和 Region 注入，不通过 Shell 直接 new 具体页面。
