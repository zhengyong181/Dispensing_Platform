# DesignSystem 层规则

DesignSystem 承载 UI Token、主题、样式、图标和通用控件。它只解决视觉和交互一致性，不承载业务。

## 允许

- 定义颜色、间距、字体、圆角、阴影、动画时长等 Token。
- 定义主题 ResourceDictionary、通用控件样式、图标资源和设计系统示例。
- 使用 WPF 和选定 UI 控件库做视觉适配。

## 禁止

- 不得引用 Application、Process、HAL、Drafting 或业务 Module。
- 不得包含设备状态机、生产流程、报警处理、配置加载等业务逻辑。
- 不得在页面或控件中硬编码颜色、间距和状态样式，优先使用 Token。
- 不得为单一客户写专属控件分支；客户品牌差异走配置和主题覆盖。

## 交互方式

- 业务页面消费 DesignSystem 资源，DesignSystem 不反向知道业务页面。
- 通用控件通过依赖属性、命令和事件暴露 UI 行为，不依赖业务服务。
- Token 或主题规则变化必须同步 `docs/8 Design-System.md`。
