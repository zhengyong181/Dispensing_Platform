# 工业超精密点胶平台 — 架构文档集

本目录是项目的架构与开发基线文档集，包含十份主体架构文档，按"地基 → 核心难点 → 工业品质 → 商业与体验 → 工程纪律"逐层展开。

## 文档索引

| 编号 | 文件 | 状态 | 定位 |
|------|------|------|------|
| 1 | [1 Architecture.md](./1%20Architecture.md) | ✅ 已产出 | 项目地图：理念、技术栈、分层、数据流 |
| 2 | [2 Solution-Structure.md](./2%20Solution-Structure.md) | ✅ 已产出 | 解决方案目录与项目依赖骨架 |
| 3 | [3 Core-Contracts.md](./3%20Core-Contracts.md) | ✅ 已产出 | 跨模块的接口与数据模型契约 |
| 4 | [4 Drafting-Subsystem.md](./4%20Drafting-Subsystem.md) | ✅ 已产出 | 编辑器子系统（对标 AutoCAD 2D） |
| 5 | [5 Sync-Mechanism.md](./5%20Sync-Mechanism.md) | ✅ 已产出 | 画布 / IR / 仿真 / 控制器程序 / 真机回采同步 |
| 6 | [6 StateMachine-Design.md](./6%20StateMachine-Design.md) | ✅ 已产出 | 分层状态机与恢复机制 |
| 7 | [7 Data-Persistence.md](./7%20Data-Persistence.md) | ✅ 已产出 | SQLite 分库 + Parquet 落盘 |
| 8 | [8 Design-System.md](./8%20Design-System.md) | ✅ 已产出 | UI 设计 Token 与主题系统 |
| 9 | [9 Config-Multitenancy.md](./9%20Config-Multitenancy.md) | ✅ 已产出 | 多客户多机型配置管理 |
| 10 | [10 DevOps.md](./10%20DevOps.md) | ✅ 已产出 | 开发与发布流程 |

## 阅读建议

- **第一次阅读**：按 1 → 2 → 3 顺序通读，建立整体认识
- **准备开始开发**：先读根目录 `AGENTS.md`，再按任务回查对应主体文档
- **执行某个阶段**：参考 文档 2 的项目结构策略、根目录 `AGENTS.md` 的工作流程，以及相关专题文档
- **验收首条闭环**：按当前阶段目标在任务说明中定义输入、过程、结果和未覆盖项；架构边界以 `AGENTS.md` 与文档 1-10 为准
- **新增硬件**：直接看 文档 3 的 HAL 接口章节 + 文档 2 的扩展点章节
- **新增 UI 模块**：直接看 文档 2 的扩展点章节 + 文档 8
- **现场调试**：参考 文档 6 的状态机与恢复章节
- **新客户接入**：参考 文档 9 的配置层级章节

## 版本

- 文档版本：v0.3（保留 1-10 架构文档，执行规则迁入 AGENTS.md）
- 最后更新：2026-05-24
- 维护者：单人开发，主线维护

## 贡献约定

- 任何架构决策的变更，先更新对应文档，再改代码
- 重大决策另起一份 ADR（Architecture Decision Record），放在 `docs/adr/` 下
- 文档使用 Markdown，图表优先使用 Mermaid（便于版本管理）
