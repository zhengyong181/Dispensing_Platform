# 文档 2 — 解决方案与项目结构（Solution-Structure.md）

> 版本：v0.3 · 最后更新：2026-05-24

本文是项目的工程地图。当前阶段采用**最小项目骨架 + 按需项目化**策略：只创建当前真正要开发的项目，其它层先保留空目录占位。后续开发到哪一层、哪一个子系统，再根据实际需要创建对应 `.csproj`。

当前计划从 `Shell` 层开始，因此现阶段只需要 `DispensingPlatform.Shell` 是真实项目；`Core`、`Application`、`Hal`、`Drafting`、`Process`、`Modules`、`DesignSystem` 暂时只作为目录存在。

---

## 1. 当前目录树

```text
DispensingPlatform.sln
├─ Directory.Build.props
├─ Directory.Packages.props
├─ NuGet.Config
├─ .editorconfig
├─ .gitignore
├─ global.json
│
├─ src/
│   ├─ Shell/
│   │   └─ DispensingPlatform.Shell/
│   │       └─ DispensingPlatform.Shell.csproj
│   ├─ Core/
│   │   └─ README.md
│   ├─ Application/
│   │   └─ README.md
│   ├─ Hal/
│   │   └─ README.md
│   ├─ Drafting/
│   │   └─ README.md
│   ├─ Process/
│   │   └─ README.md
│   ├─ Modules/
│   │   └─ README.md
│   └─ DesignSystem/
│       └─ README.md
│
├─ tests/
│   └─ README.md
│
├─ configs/
├─ docs/
├─ samples/
└─ tools/
```

说明：

- 空目录用 `README.md` 占位，避免 Git 不跟踪。
- 当前只创建 `Shell` 项目，因为现阶段准备从 Shell 层开始搭建。
- 其它层不提前创建 `.csproj`，避免一开始出现大量空项目和错误边界。
- 后续新增项目前，先更新本文，再创建项目骨架。

---

## 2. 当前项目

### 2.1 `DispensingPlatform.Shell`

定位：WPF 启动入口和 UI 主壳。

当前职责：

- 应用入口
- 主窗口骨架
- 启动 / 关闭流程入口
- 配置、日志、DI 的装配入口
- Prism 只用于 UI 组织：Shell、Region、导航、对话适配

当前不做：

- 不承载业务逻辑
- 不直接调用 HAL
- 不实现 Drafting / Process / Application 细节
- 不让 Prism 类型进入非 UI 层

如果希望 `Shell` 项目在没有任何源码时也能保留为占位项目，可以先只创建 `.csproj`；如果要求它可编译运行，则需要允许最小 `App.xaml` / 入口代码，这属于下一步实现任务，不属于“纯骨架”。

---

## 3. 暂不项目化的层

这些目录现在只保留占位，等开发到对应功能时再创建项目：

| 目录 | 未来职责 | 何时创建项目 |
|------|----------|--------------|
| `src/Core/` | 公共契约、错误码、配置、日志、事件总线、核心服务 | 开始定义公共契约或横切基础设施时 |
| `src/Application/` | 状态机、调度、资源仲裁、恢复流程 | 开始做业务编排或状态机时 |
| `src/Hal/` | 硬件抽象、仿真器、厂商适配 | 开始做 Simulator 或真实硬件接入时 |
| `src/Drafting/` | 编辑器文档模型、几何、命令、渲染、交互 | 开始做编辑器时 |
| `src/Process/` | IR、编译、仿真、发码、执行适配 | 开始做工艺编译或执行链路时 |
| `src/Modules/` | UI 功能模块集合 | 开始做具体功能页时 |
| `src/DesignSystem/` | Token、主题、控件、图标 | 开始抽设计系统时 |

---

## 4. 按需创建项目的规则

新增项目必须满足至少一个条件：

- 当前任务需要实际开发该层功能
- 该层已经有明确职责和近期实现内容
- 需要通过项目引用表达依赖边界
- 需要单独测试、单独发布或运行时插件化
- 继续放在现有项目里会造成依赖污染

不建议因为“未来可能会用到”提前创建项目。目录可以先存在，项目等需要时再建。

---

## 5. 后续可能的演进

### 5.1 第一阶段：按大层创建项目

当对应层真正开始开发时，可以逐步变成：

```text
src/
├─ Shell/DispensingPlatform.Shell/
├─ Core/DispensingPlatform.Core/
├─ Application/DispensingPlatform.Application/
├─ Hal/DispensingPlatform.Hal/
├─ Drafting/DispensingPlatform.Drafting/
├─ Process/DispensingPlatform.Process/
├─ Modules/DispensingPlatform.Modules/
└─ DesignSystem/DispensingPlatform.DesignSystem/
```

### 5.2 第二阶段：内部文件夹组织

大层项目内部先用文件夹和命名空间组织，例如：

```text
src/Drafting/DispensingPlatform.Drafting/
├─ Core/
├─ Geometry/
├─ Commands/
├─ Rendering/
├─ Interaction/
└─ IO/
```

这些仍然只是一个 `DispensingPlatform.Drafting` 项目里的内部结构。

### 5.3 第三阶段：必要时拆细项目

只有当内部模块稳定、变大、需要独立发布或强制隔离时，才拆成独立项目。例如：

| 内部区域 | 未来可能拆成 | 触发条件 |
|----------|--------------|----------|
| `Drafting/Geometry` | `DispensingPlatform.Drafting.Geometry` | 几何算法需要独立复用或测试 |
| `Process/Ir` | `DispensingPlatform.Process.Ir` | IR schema 稳定且需要强版本控制 |
| `Hal/Simulator` | `DispensingPlatform.Hal.Simulator` | 仿真器成为测试基础设施 |
| `Hal/Vendors/Beckhoff` | `DispensingPlatform.Hal.Motion.Beckhoff` | 接入 Beckhoff SDK 并需要独立部署 |
| `Modules/Drafting` | `DispensingPlatform.Modules.Drafting` | UI 模块需要独立加载或客户裁剪 |

拆分前必须先更新本文；如影响公共契约、部署方式或技术栈，需要补 ADR。

---

## 6. 依赖边界原则

即使当前很多层还只是目录，占位阶段也要遵守这些边界：

- `Shell` 是顶端入口。
- Prism 只允许出现在 Shell、Modules、DesignSystem 的 UI 适配代码中。
- Prism 不作为跨层业务事件总线。
- 会改变设备状态的 UI 命令必须进入 Application，不由 UI 直接调用 HAL。
- Drafting / Compiler / Simulation 不直接依赖 HAL。
- 只有执行适配层和 Service / Device 实现层接触 HAL 契约。
- HAL 厂商实现之间不能互相依赖。

---

## 7. 测试项目策略

当前可以只保留 `tests/README.md`。当某个层开始开发时，再创建对应测试项目：

```text
tests/
├─ DispensingPlatform.Shell.Tests/
├─ DispensingPlatform.Core.Tests/
├─ DispensingPlatform.Hal.Tests/
├─ DispensingPlatform.Drafting.Tests/
├─ DispensingPlatform.Process.Tests/
├─ DispensingPlatform.Application.Tests/
├─ DispensingPlatform.Modules.Tests/
├─ DispensingPlatform.Integration.Tests/
└─ DispensingPlatform.Architecture.Tests/
```

测试项目同样按需创建，不提前铺满。

---

## 8. 配置、工具与样例目录

```text
configs/
├─ _shared/
├─ schemas/
└─ customer-XYZ/
    └─ model-A1/

tools/
├─ Scripts/
├─ Templates/
└─ Analyzers/

samples/
└─ SampleRecipes/
```

这些目录可以先只有 README 占位。真实配置、脚本、模板和样例只在当前阶段用到时再补。

---

## 9. 相关文档

- 文档 1：[1 Architecture.md](./1%20Architecture.md) — 总体架构与边界
- 文档 3：[3 Core-Contracts.md](./3%20Core-Contracts.md) — 公共契约方向
- 文档 4：[4 Drafting-Subsystem.md](./4%20Drafting-Subsystem.md) — Drafting 内部职责
- 文档 5：[5 Sync-Mechanism.md](./5%20Sync-Mechanism.md) — Process / IR / 同步语义
- 文档 6：[6 StateMachine-Design.md](./6%20StateMachine-Design.md) — Application 状态机
- 文档 8：[8 Design-System.md](./8%20Design-System.md) — DesignSystem 内部结构
- 文档 9：[9 Config-Multitenancy.md](./9%20Config-Multitenancy.md) — 配置目录
- 文档 10：[10 DevOps.md](./10%20DevOps.md) — 构建、CI、发布
