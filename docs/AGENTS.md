# docs 目录规则

`docs` 是项目架构事实来源。代码、配置、测试和工具不能与文档长期分叉。

## 文档同步

- 项目结构变化更新 `docs/2 Solution-Structure.md`。
- 公共接口变化更新 `docs/3 Core-Contracts.md`。
- Drafting 数据结构变化更新 `docs/4 Drafting-Subsystem.md`。
- IR 或同步语义变化更新 `docs/5 Sync-Mechanism.md`。
- 状态语义变化更新 `docs/6 StateMachine-Design.md`。
- 数据表或归档策略变化更新 `docs/7 Data-Persistence.md`。
- UI 设计规则变化更新 `docs/8 Design-System.md`。
- 配置格式变化更新 `docs/9 Config-Multitenancy.md`。
- 构建发布变化更新 `docs/10 DevOps.md`。

## 约束

- 当前只维护文档 1-10 和 `docs/adr/`，不要重新新增 11、12、13 作为长期编号文档。
- AI / 编码协作规则放在根目录 `AGENTS.md` 和各层级 `AGENTS.md`。
- 文档中出现 `DispensingPlatform.*` 名称时，要说明它是当前项目、逻辑命名空间还是未来拆分候选，避免误导为必须立即项目化。
- 重大架构决策必须新增 ADR 到 `docs/adr/`。

## 注释语言与详细度（强制）

- 所有源代码注释必须使用简体中文，不得使用其它语言作为注释正文。
- 所有源代码必须包含详细注释，至少应说明：设计意图、输入输出语义、边界条件、失败处理与恢复策略（适用时）。
- 禁止空泛注释（如“赋值”“调用方法”这类无信息量注释）；注释应帮助维护者在不了解上下文时快速理解代码。
- 新增或修改代码时，如注释不满足以上规则，必须在同次提交中补齐。