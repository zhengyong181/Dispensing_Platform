# ADR 目录

本目录用于记录重大架构决策（Architecture Decision Record）。普通实现细节不需要写 ADR；会影响公共契约、依赖边界、技术栈、数据格式、上下位机协议、部署方式或长期演进方向的变化，需要新增 ADR。

## 命名

```text
ADR-0001-short-title.md
ADR-0002-short-title.md
```

编号递增，不复用。标题使用英文短横线，便于跨平台路径处理。

## 模板

```markdown
# ADR-0000: <决策标题>

- 状态：Proposed / Accepted / Deprecated / Superseded
- 日期：YYYY-MM-DD

## 背景

为什么需要做这个决策。

## 选项

列出考虑过的主要方案。

## 决策

说明最终选择，以及边界条件。

## 后果

写清收益、代价、风险和后续动作。
```

