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
