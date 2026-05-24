# configs/schemas 目录规则

Schema 是配置兼容性的边界。任何 schema 变化都可能影响已有客户配置。

## 允许

- 为 machine、hardware、modules、persistence、theme、tenant 等配置定义 JSON Schema。
- 为新增字段提供默认值、说明和示例。
- 使用版本字段支持配置迁移。

## 禁止

- 不得无说明地删除字段、重命名字段或改变字段语义。
- 不得放宽安全相关字段的校验，例如限位、急停、互锁、报警等级和硬件类型。
- 不得让 schema 接受任意未定义对象来绕过校验。

## 交互方式

- Schema 变化必须同步 `docs/9 Config-Multitenancy.md`。
- 破坏兼容的 schema 变化需要迁移说明；重大变化需要 ADR。
- 如果新增硬件配置字段，应同时检查 HAL 契约和硬件配置示例。
