# Modules 层规则

Modules 是业务 UI 功能区。模块可以使用 Prism 做 UI 组织，但不能把 Prism 变成业务协作总线。

## 允许

- 使用 WPF、Prism、DesignSystem、ViewModel 和 UI 适配代码。
- 通过 Application 门面触发命令型业务操作。
- 通过查询服务、状态订阅、项目自有事件总线展示数据。
- 按配置启用、禁用或后续独立插件化。

## 禁止

- 模块之间不得直接引用、直接调用或共享内部 ViewModel。
- 不得直接引用 HAL、厂家 SDK、Process 内部实现或数据库实现。
- 不得使用 Prism `EventAggregator` 作为跨层业务事件总线。
- 不得在模块里写客户专属 if-else 分支。

## 交互方式

- Module 到 Module 的业务协作通过 Core 契约、Application 门面或项目自有 `IEventBus`。
- Module 到设备的命令必须经过 Application。
- Module 只处理展示、输入、导航和用户意图表达。
- 新增 UI 模块时先确认是否只需要目录，还是确实需要独立项目。
