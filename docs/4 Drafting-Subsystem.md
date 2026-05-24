# 文档 4 — 编辑器子系统设计（Drafting-Subsystem.md）

> 版本：v0.1 · 最后更新：2026-05-20

本文是项目里架构难度最高的子系统的专属设计文档。编辑器不是一个"画图控件"，而是一个完整的 CAD 子应用，目标体验对标 AutoCAD 2D，承担"路径规划 + 工艺规划 + 仿真预览 + G 代码视图"四合一的职责。

---

## 1. 子系统定位与目标

### 1.1 在整体架构中的位置

编辑器子系统位于 **Drafting/** 分层。当前阶段先按目录和命名空间组织，不预先拆成多个项目；后续只有几何、渲染、命令、IO 等区域变大、需要独立测试或复用时，才按文档 2 的按需项目化规则拆分。它**不直接依赖 HAL**，所有"动起来"的能力通过 Process 层（IR 编译 + 仿真 + 发码）间接获得。

### 1.2 核心目标

1. **CAD 体验对标 AutoCAD 2D**：图元、命令、吸附、坐标输入、图层、块、撤销重做
2. **承载工艺语义**：在几何之上挂载工艺标注（点胶段、Mark、禁打区、测高点等）
3. **支持 DXF 导入**：netDxf，覆盖常见图元，工艺信息通过图层映射
4. **路径与工艺规划**：直观可视，可批量套用工艺模板
5. **仿真与 G 代码集成**：与 IR 双向同步，三者一致
6. **真机回采叠加**：实际轨迹与设计轨迹同框对比
7. **性能**：万级图元下保持 60 fps 交互
8. **扩展友好**：新工艺图元、新命令、新吸附、新文件格式都可插件化

### 1.3 非目标（V1 不做）

- 3D 实体造型、工程图标注体系（仅基础尺寸标注）
- 参数化绘图引擎（仅预留接口）
- DWG 原生格式
- 装配体、约束求解
- 渲染照片级真实感
- 协同编辑（多用户）

### 1.4 设计原则

- **领域驱动**：文档模型先行，UI 是文档模型的视图
- **快照不可变 + 命令侧写**：图元、组件、IR 等可传递数据结构不可变；`DraftingDocument` 是受控可变的文档根对象，所有变更走命令栈和 `IDocumentMutator`
- **几何与工艺解耦**：几何层稳定，工艺层可演进
- **渲染与逻辑分离**：渲染层只读，不改文档
- **CPU 与 GPU 边界清晰**：几何运算 CPU，绘制 GPU（SkiaSharp）

---

## 2. 文档模型（Document Model）

### 2.1 DraftingDocument（一份图纸 / 一份配方设计）

```csharp
public sealed class DraftingDocument {
    public DocumentId Id { get; }
    public DocumentMetadata Metadata { get; }
    public LayerTable Layers { get; }
    public BlockTable Blocks { get; }
    public StyleTable Styles { get; }                  // 文字样式、尺寸样式、线型
    public EntityTable Entities { get; }
    public UcsTable UserCoordinateSystems { get; }
    public ParameterTable Parameters { get; }          // V2 参数化预留
    public ProcessAnnotations ProcessLayer { get; }    // 工艺图元集合（独立索引）
    public ViewState ViewState { get; }                // 当前视口、命名视图

    public bool IsModified { get; }
    public string ContentHash { get; }                 // 用于 IR 同步
    public event Action<DocumentChangedArgs>? Changed;
}
```

文档是**有状态对象**，但所有的修改必须走 `IDocumentMutator`（命令系统的内部接口），保证脏标记、变更事件、Undo 栈一致。

### 2.2 DocumentStore（多文档管理）

```csharp
public interface IDocumentStore {
    DraftingDocument? Active { get; }
    IReadOnlyList<DraftingDocument> Open { get; }

    Task<DraftingDocument> CreateAsync(DocumentTemplate template, CancellationToken ct);
    Task<DraftingDocument> OpenAsync(string path, CancellationToken ct);
    Task SaveAsync(DraftingDocument doc, string path, CancellationToken ct);
    Task<bool> CloseAsync(DraftingDocument doc, CloseMode mode, CancellationToken ct);

    void Activate(DocumentId id);
    event Action<DocumentId>? ActiveChanged;
}
```

允许同时打开多份文档（多 Tab），但只有一份是 Active。

### 2.3 持久化格式

**原生格式**：`.dpdoc`（DispensingPlatform Document），实质是 ZIP 容器，里面包含：

```
recipe.dpdoc/
├─ manifest.json          # 文档元信息 + schema 版本
├─ document.json          # DraftingDocument 主体（JSON）
├─ geometry.dxf           # 几何部分（DXF 嵌入，方便外部 CAD 打开）
├─ thumbnails/
│   └─ preview.png
├─ resources/             # 嵌入资源（参考底图等）
└─ signatures/            # 预留电子签名
```

JSON 是唯一权威源，DXF 是兼容导出副本：保存 `.dpdoc` 时可以把 JSON 中的几何同步导出到 `geometry.dxf`，方便外部 CAD 查看；打开时优先且默认只读取 JSON。DXF 仅用于兼容校验和人工检查，不参与恢复工艺语义，避免 DXF 无法表达的工艺信息反向覆盖 JSON。

### 2.4 自动保存与崩溃恢复

- 每 N 秒（默认 60s，可配置）自动保存到 `cache/autosave/<docId>.dpdoc`
- 自动保存只在文档已修改时触发
- 崩溃后启动检测 `autosave/`，提示用户恢复
- 崩溃恢复成功后，自动备份原文件防止覆盖

### 2.5 脏标记与变更事件

- `IsModified` 由命令栈驱动：每次执行非只读命令置 true，保存后清零
- `Changed` 事件按变更**批次**触发（一个用户操作可能触发多个底层修改，合并成一次事件），避免 UI 频繁刷新
- 事件 payload 包含变更的 entity id 集合 + 变更类型（Added / Modified / Removed），渲染层据此做局部重绘

---

## 3. 图元体系（Entity System）

### 3.1 ECS 思想（Entity-Component-System）

为了让"几何"和"工艺"解耦，采用轻量 ECS：

- **Entity**：只是一个 Id + 一组 Component
- **Component**：纯数据（record），按类型挂在 Entity 上
- **System**：操作 Entity（命令、渲染、编译都是 System）

实际实现可以更简单——用一个基类 `EntityBase` + `Properties` 字典，但接口上对齐 ECS 思想：

```csharp
public abstract record EntityBase {
    public required EntityId Id { get; init; }
    public required LayerId Layer { get; init; }
    public required AffineTransform Transform { get; init; }    // 局部坐标变换
    public ImmutableDictionary<Type, IEntityComponent> Components { get; init; }
        = ImmutableDictionary<Type, IEntityComponent>.Empty;

    public T? Get<T>() where T : class, IEntityComponent;
    public EntityBase With<T>(T component) where T : class, IEntityComponent;
}
```

### 3.2 几何图元清单（Drafting.Core / Geometry 模型）

| 类型 | 说明 | DXF 对应 |
|------|------|----------|
| `PointEntity` | 点 | POINT |
| `LineEntity` | 直线 | LINE |
| `PolylineEntity` | 多段线（含弧段） | LWPOLYLINE / POLYLINE |
| `ArcEntity` | 圆弧 | ARC |
| `CircleEntity` | 圆 | CIRCLE |
| `EllipseEntity` | 椭圆 / 椭圆弧 | ELLIPSE |
| `SplineEntity` | B 样条 / NURBS | SPLINE |
| `HatchEntity` | 填充图案（含边界） | HATCH |
| `TextEntity` | 单行文本 | TEXT |
| `MTextEntity` | 多行文本 | MTEXT |
| `DimensionEntity` | 尺寸标注 | DIMENSION |

### 3.3 结构图元

| 类型 | 说明 |
|------|------|
| `LayerEntry` | 图层定义（属于 LayerTable，不是普通图元） |
| `BlockDefinition` | 块定义（属于 BlockTable） |
| `BlockReferenceEntity` | 块引用（普通图元） |
| `GroupEntry` | 编组（按 id 列表组织） |

### 3.4 工艺图元（Drafting.Process）

工艺图元**独立于几何**，但通过 `GeometryRef` 挂在某个几何上（一对一或一对多），这样删除工艺标注不会影响几何，反之亦然。

```csharp
public abstract record ProcessEntityBase : EntityBase {
    public required GeometryRef Target { get; init; }   // 关联的几何 entity id（可空）
}

// 点点胶
public sealed record DispensePointEntity(...) : ProcessEntityBase {
    public required Pose Position { get; init; }
    public required DispenseRecipeRef Recipe { get; init; }  // 工艺模板引用
    public Length? DwellHeight { get; init; }
    public Duration? DwellTime { get; init; }
}

// 线/弧/样条点胶（挂在几何上）
public sealed record DispensePathEntity(...) : ProcessEntityBase {
    public required DispenseRecipeRef Recipe { get; init; }
    public required PathDirection Direction { get; init; }   // Forward / Reverse
    public TransitionPolicy CornerPolicy { get; init; }
    public Length? LiftHeight { get; init; }
    public StartEndOptions StartEnd { get; init; }
}

// 填充
public sealed record FillRegionEntity(...) : ProcessEntityBase {
    public required FillPattern Pattern { get; init; }       // Zigzag / Spiral / Contour
    public Length StepOver { get; init; }
    public Angle Direction { get; init; }
    public DispenseRecipeRef Recipe { get; init; }
}

// Mark / 视觉
public sealed record MarkPointEntity(...) : ProcessEntityBase {
    public required Point2D Nominal { get; init; }
    public string AlgorithmId { get; init; } = "";
    public Length SearchRadius { get; init; }
}

// 禁打区
public sealed record KeepOutZoneEntity(...) : ProcessEntityBase {
    public required IReadOnlyList<Point2D> Boundary { get; init; }
    public KeepOutPolicy Policy { get; init; }              // 跳过 / 抬笔 / 重新规划
}

// 换针点
public sealed record ToolChangePointEntity(...) : ProcessEntityBase { ... }

// 清针点
public sealed record PurgePointEntity(...) : ProcessEntityBase { ... }

// 测高点
public sealed record HeightProbePointEntity(...) : ProcessEntityBase { ... }
```

### 3.5 工艺图元的渲染语义

每个工艺图元有自己的渲染样式（颜色、图标、透明度），**默认与几何同位置叠加显示**。例如 `DispensePathEntity` 在其挂载的 Polyline 上覆盖一层箭头标识方向，颜色由工艺模板决定。

工艺图元只能在专门的"工艺图层"上创建，不能随意散落在普通图层。

---

## 4. 几何运算库（Drafting.Geometry）

这是一个**纯算法逻辑区域**，无 UI 依赖。命名空间 `DispensingPlatform.Drafting.Geometry`；未来需要被其他项目复用或多目标框架构建时，可再拆成独立库。

### 4.1 基础类型

```csharp
public readonly record struct Vector2(double X, double Y);
public readonly record struct Point2 (double X, double Y);
public readonly record struct AABB    (Point2 Min, Point2 Max);    // 轴对齐包围盒
public readonly record struct Segment2(Point2 A, Point2 B);
public readonly record struct Arc2    (Point2 Center, double Radius, double StartAngle, double SweepAngle);
public readonly record struct Bezier2 (Point2 P0, Point2 P1, Point2 P2, Point2 P3);

public sealed class Polygon2 {
    public IReadOnlyList<Point2> Vertices { get; }
    public bool IsClosed { get; }
    public AABB Bounds { get; }
    public double SignedArea { get; }
}
```

**注意**：几何层使用裸 `double`（性能 + 与算法库对齐），单位由调用层保证（mm）。这与上层 `Length` 量纲不冲突——上层入边界做转换。

### 4.2 基本运算

- 距离：点到点、点到线、点到弧、点到样条
- 投影：点投影到线 / 弧 / 样条
- 相交：线-线、线-弧、弧-弧、线-样条、Bezier 求交
- 切线、法线、曲率
- 弧长、参数化
- 包围盒、凸包

### 4.3 偏移算法（Offset）

CAD 偏移是核心能力，覆盖：

- 直线偏移（平移）
- 多段线偏移（处理凹角自相交、凸角圆角/尖角/斜角策略）
- 圆弧偏移（半径增减）
- 样条偏移（先离散再重拟合）
- 多边形向内/向外偏移（用于胶轨道路宽度补偿）

推荐底层用 **Clipper2** 做整数化的多边形偏移与布尔，自研一层包装暴露 `double` API。

### 4.4 布尔运算

- 多边形并集 / 交集 / 差集 / 异或
- 区域裁剪（KeepOutZone 应用到路径）

### 4.5 样条采样与简化

- B 样条 / NURBS → 折线离散（按弦高公差、按弧长、按等参数）
- 折线 → 样条拟合（Douglas-Peucker 简化 + 拟合）

### 4.6 路径优化（TSP 类）

- 最近邻启发式（快速）
- 2-opt 改进（中等质量）
- Lin-Kernighan（高质量，V2 引入）
- 起点固定 / 终点固定 / 起终点重合三种模式
- 支持"组内不打乱顺序、组间优化"

### 4.7 拐角处理

- 内拐角：尖角 / 圆角 / 斜角
- 外拐角：尖角 / 圆角
- 速度规划：拐角速度、前瞻减速段长度
- 与 Process 层的 `TransitionPolicy` 对齐

### 4.8 空间索引

- **R-Tree**：用于 AABB 范围查询、相交查询
- **Quadtree**：用于点查找、吸附
- 索引随文档变更增量维护（命令系统提供变更通知）
- 性能目标：10 万图元下点击查询 < 1 ms

### 4.9 数值稳健性

- 容差统一管理：`GeometryTolerance.Default = 1e-6 mm`
- 浮点比较一律 epsilon 比较，禁止裸 `==`
- 大坐标场景（如 1m × 1m 工件）的精度退化测试

---

## 5. 命令系统（Drafting.Commands）

### 5.1 命令架构

参考 AutoCAD：**命令是一等公民**，UI 工具按钮、命令行输入、脚本宏都通过命令系统执行。

```csharp
public interface ICommand {
    string Name { get; }                                // "LINE", "OFFSET"
    string[] Aliases { get; }                           // "L", "O"
    CommandKind Kind { get; }                           // Draw / Modify / Query / View / Process
    Permission RequiredPermission { get; }
    Task<CommandResult> ExecuteAsync(CommandContext ctx, CancellationToken ct);
    bool CanExecute(CommandContext ctx);
}

public sealed class CommandContext {
    public DraftingDocument Document { get; }
    public ICommandPrompter Prompter { get; }           // 与用户交互（指定点 / 选择对象 / 输入文本）
    public ISnapEngine Snap { get; }
    public ISelectionService Selection { get; }
    public ICommandHistory History { get; }
    public ImmutableDictionary<string, object?> Args { get; }
}
```

### 5.2 命令注册表

启动时通过 DI 收集所有 `ICommand` 实现，按 Name / Alias 索引。新增命令 = 实现接口 + 在 Module 中注册（无主程序改动）。

```csharp
public interface ICommandRegistry {
    void Register(ICommand command);
    ICommand? Find(string nameOrAlias);
    IReadOnlyList<ICommand> All { get; }
}
```

### 5.3 命令清单（V1 计划）

#### 5.3.1 绘制命令

| 命令 | 别名 | 说明 |
|------|------|------|
| LINE | L | 直线 |
| PLINE | PL | 多段线（含弧段切换） |
| RECTANG | REC | 矩形 |
| POLYGON | POL | 正多边形 |
| ARC | A | 圆弧（多种起点终点策略） |
| CIRCLE | C | 圆（圆心半径 / 三点 / 切切切） |
| ELLIPSE | EL | 椭圆 |
| SPLINE | SPL | 样条 |
| POINT | PO | 点 |
| TEXT / MTEXT | T / MT | 文本 |
| HATCH | H | 填充 |
| BHATCH | BH | 边界填充 |
| INSERT | I | 插入块 |

#### 5.3.2 修改命令

| 命令 | 别名 | 说明 |
|------|------|------|
| MOVE | M | 移动 |
| COPY | CO | 复制 |
| ROTATE | RO | 旋转 |
| SCALE | SC | 缩放 |
| MIRROR | MI | 镜像 |
| TRIM | TR | 修剪 |
| EXTEND | EX | 延伸 |
| FILLET | F | 圆角 |
| CHAMFER | CHA | 倒角 |
| OFFSET | O | 偏移 |
| BREAK | BR | 打断 |
| JOIN | J | 合并 |
| EXPLODE | X | 炸开 |
| ARRAY | AR | 阵列（矩形 / 环形 / 路径） |
| ALIGN | AL | 对齐 |
| STRETCH | S | 拉伸 |
| LENGTHEN | LEN | 改长度 |

#### 5.3.3 查询命令

| 命令 | 别名 | 说明 |
|------|------|------|
| LIST | LI | 列出图元属性 |
| DIST | DI | 测量距离 |
| AREA | AA | 测量面积 |
| ID | ID | 显示坐标 |
| MEASUREGEOM | MEA | 综合测量 |

#### 5.3.4 视图命令

| 命令 | 别名 | 说明 |
|------|------|------|
| ZOOM | Z | 缩放（窗口 / 范围 / 上一个 / 实时） |
| PAN | P | 平移 |
| REGEN | RE | 重生成显示 |
| VIEW | V | 命名视图管理 |

#### 5.3.5 图层命令

| 命令 | 别名 | 说明 |
|------|------|------|
| LAYER | LA | 图层管理对话框 |
| LAYERON / OFF | | 图层开关 |
| LAYERFREEZE / THAW | | 冻结 / 解冻 |
| LAYERLOCK / UNLOCK | | 锁定 / 解锁 |

#### 5.3.6 工艺命令（点胶专用）

| 命令 | 别名 | 说明 |
|------|------|------|
| DPATH | DP | 把选中几何标记为点胶轨迹 |
| DPOINT | DPT | 添加点点胶 |
| DFILL | DF | 添加填充区域 |
| MARK | MK | 设为 Mark 点 |
| KEEPOUT | KO | 设为禁打区 |
| TOOLCHG | TC | 添加换针点 |
| PURGE | PG | 添加清针点 |
| PROBE | PB | 添加测高点 |
| ASSIGNRECIPE | ASSR | 套用工艺模板 |
| OPTIMIZE | OPT | 路径顺序优化 |
| REVERSEPATH | REV | 反转路径方向 |
| LIFTPLAN | LP | 抬笔策略规划 |

> 说明：上面 `AR` 与 `ARRAY` 冲突——别名要在统一别名表里**唯一**，最终别名待最终命令清单冻结时调整。

### 5.4 命令交互模型

每个命令通过 `ICommandPrompter` 与用户交互，模仿 AutoCAD 的命令行：

```csharp
public interface ICommandPrompter {
    Task<Point2?> GetPointAsync(string prompt, PointOptions opts, CancellationToken ct);
    Task<IReadOnlyList<EntityId>> GetSelectionAsync(string prompt, SelectionOptions opts, CancellationToken ct);
    Task<double?> GetDistanceAsync(string prompt, DistanceOptions opts, CancellationToken ct);
    Task<double?> GetAngleAsync(string prompt, AngleOptions opts, CancellationToken ct);
    Task<string?> GetStringAsync(string prompt, StringOptions opts, CancellationToken ct);
    Task<int?> GetKeywordAsync(string prompt, IReadOnlyList<string> keywords, CancellationToken ct);
    void WriteMessage(string message, MessageLevel level = MessageLevel.Info);
}
```

命令执行过程中可被取消（ESC），命令实现必须正确响应 `CancellationToken`。

### 5.5 撤销 / 重做

每个命令产生一个 `CommandTransaction`，记录所有变更（Memento 模式）：

```csharp
public sealed class CommandTransaction {
    public string CommandName { get; }
    public DateTimeOffset At { get; }
    public IReadOnlyList<IReversibleChange> Changes { get; }
    public string? Description { get; }

    public void Undo(DraftingDocument doc);
    public void Redo(DraftingDocument doc);
}

public interface IReversibleChange {
    void Apply(DraftingDocument doc);
    void Revert(DraftingDocument doc);
}
```

实现要点：
- 命令开始 → 创建 transaction
- 中途 ESC / 异常 → transaction 回滚
- 成功完成 → 入栈
- 复合命令把多个底层 change 打包成一个用户级 Undo 单元
- 栈深度可配置（默认无限，仅受内存限制）
- 跨会话不持久化（保存时清空栈）
- 文档关闭后释放栈（防止内存泄漏）

### 5.6 命令的可宏化

命令支持脚本（V1 起步是简单顺序执行，V2 接 Lua）：

```
LINE 0,0 100,0
LINE 100,0 100,100
LINE 100,100 0,100
LINE 0,100 0,0
```

宏文件存为 `.dpscript`，可绑定到工具栏按钮。

---

## 6. 选择系统（Selection）

### 6.1 选择方式

| 方式 | 触发 | 说明 |
|------|------|------|
| 点选 | 单击图元 | 单选 |
| 加选 | Shift + 点选 | 增加选择 |
| 切换选 | Ctrl + 点选 | 已选则取消 |
| 窗选 | 从左上拖到右下 | 完全包含才选中（蓝色矩形） |
| 窗交 | 从右下拖到左上 | 相交即选中（绿色矩形） |
| 栅栏选 | F 关键字 | 画线相交 |
| 套索 | WP / CP 关键字 | 不规则区域 |
| 全选 | Ctrl+A | 当前文档可见可选图元 |
| 反选 | INVERTSEL | 反选 |
| 上一个 | Previous 关键字 | 上次选择集 |

### 6.2 过滤选择（QSELECT）

按属性过滤：

- 按图元类型
- 按图层
- 按颜色 / 线型 / 线宽
- 按工艺模板
- 按尺寸范围（长度 / 面积）
- 自定义谓词（脚本）

### 6.3 选择集对象

```csharp
public interface ISelectionSet {
    IReadOnlyList<EntityId> Ids { get; }
    int Count { get; }
    AABB Bounds { get; }
    bool Contains(EntityId id);
    void Add(EntityId id);
    void Remove(EntityId id);
    void Clear();
    event Action? Changed;
}

public interface ISelectionService {
    ISelectionSet Current { get; }
    IReadOnlyDictionary<string, ISelectionSet> NamedSets { get; }
    void SaveAs(string name);
    void Restore(string name);
}
```

### 6.4 命名选择集

用户可以保存常用选择集（"所有 Mark 点"、"外圈轨迹"），跨会话持久化到文档。

### 6.5 选择高亮

- 当前选中：高亮颜色 + 夹点（Grip）显示
- 鼠标悬停预览：浅色高亮
- 选中项的属性面板自动联动

### 6.6 性能

- 选择查询走空间索引，避免遍历所有图元
- 大选择集（>10k）UI 上分批渲染夹点，避免卡顿
- 可设"最大显示夹点数"，超过仅显示选中数量

---

## 7. 吸附系统（OSnap）

### 7.1 设计目标

精度的命门。对标 AutoCAD OSnap，覆盖所有常用捕捉，且性能稳定（10 万图元下查询 < 1 ms）。

### 7.2 吸附引擎

```csharp
public interface ISnapEngine {
    SnapSettings Settings { get; set; }
    SnapResult? FindSnap(Point2 cursor, ViewState view, IReadOnlyList<EntityId>? scope = null);
    void RegisterProvider(ISnapProvider provider);
    void UnregisterProvider(string id);
}

public sealed record SnapResult(
    Point2 SnapPoint,
    SnapKind Kind,                                     // EndPoint / MidPoint / Center / ...
    EntityId? SourceEntity,
    int? SubIndex,                                     // 多段线第几段
    double Confidence);                                // 0~1
```

### 7.3 吸附 Provider 清单

| Provider | 关键字 | 说明 |
|---------|--------|------|
| `EndpointSnap` | END | 端点（线段、弧段端点、多段线端点） |
| `MidpointSnap` | MID | 中点 |
| `CenterSnap` | CEN | 圆心 / 弧心 / 椭圆中心 |
| `NodeSnap` | NOD | 点图元、控制点 |
| `QuadrantSnap` | QUA | 圆弧的 0/90/180/270 象限点 |
| `IntersectionSnap` | INT | 真实交点 |
| `ApparentIntersectionSnap` | APP | 投影交点（视图层面） |
| `ExtensionSnap` | EXT | 沿端点延伸方向 |
| `InsertionSnap` | INS | 块 / 文本插入点 |
| `PerpendicularSnap` | PER | 垂足 |
| `TangentSnap` | TAN | 切点 |
| `NearestSnap` | NEA | 最近点（沿曲线） |
| `ParallelSnap` | PAR | 平行追踪 |
| `GridSnap` | GRID | 网格 |
| `PolarSnap` | POLAR | 极坐标追踪 |
| `ObjectTrackSnap` | OTRACK | 对象追踪（基于已捕捉点的水平/垂直辅助线） |

### 7.4 优先级与冲突

- 用户配置的"启用 Provider 集合"
- 同一光标位置可能多个 Provider 命中，按优先级排序：端点 > 中点 > 交点 > 圆心 > 象限 > 节点 > 切点 > 垂足 > 最近 > 网格
- 命中时显示对应图标 + 工具提示文字
- 用户可以临时按 Tab 在多个候选间循环

### 7.5 视觉反馈

- 不同 SnapKind 用不同图标（方块=端点、三角=中点、圆=圆心、X=交点、∥=平行、⊥=垂直）
- 颜色按主题 Token 走（`Color.Snap.Endpoint` 等）
- 鼠标进入吸附范围立即显示，离开立即消失（避免抖动）
- 吸附距离阈值与缩放等级联动（屏幕像素恒定）

### 7.6 性能

- 全部走空间索引（R-Tree 查询附近图元，再让 Provider 在小集合上算具体捕捉点）
- 每帧最多检查 N 个图元（默认 64），超过早返回
- 吸附结果缓存一帧（鼠标未动时不重算）
- 选择性吸附：选中状态下可只对选中图元做吸附

---

## 8. 坐标输入系统

### 8.1 输入语法

对标 AutoCAD 的"动态输入 + 命令行"双通道：

| 语法 | 示例 | 含义 |
|------|------|------|
| 绝对坐标 | `100,200` | 当前坐标系下绝对位置 |
| 相对坐标 | `@50,30` | 相对上一点 |
| 极坐标 | `@100<45` | 长度+角度 |
| 直接距离 | `100`（鼠标已指方向） | 沿当前方向走 100 |
| 数学表达式 | `@(50+25),30` | 解析后求值 |
| 用户参数 | `@$Pitch,0` | 引用文档参数表 |

### 8.2 表达式解析

- 内置数学函数（sin/cos/sqrt/abs/floor/ceil/round/min/max）
- 内置常量（PI、E、当前光标 X/Y）
- 引用文档参数（`$Name`）
- 推荐：用 **NCalc** 库解析

### 8.3 用户坐标系（UCS）

```csharp
public sealed record UserCoordinateSystem(
    string Name,
    Point2 Origin,
    Angle Rotation,
    UcsId Id);
```

可建立、命名、保存、激活。命令 `UCS` 进入 UCS 管理。

### 8.4 坐标系切换

状态栏可切换显示坐标的坐标系：

- 机械坐标（Machine）
- 工件坐标（Workpiece）
- UCS（当前用户坐标系）
- 像素坐标（用于视觉调试视图）

### 8.5 动态输入框

- 鼠标边显示浮动输入框，光标在哪显示在哪
- 实时显示当前坐标 + 距离 + 角度（从上一点算起）
- 命令进行中显示当前提示（"指定下一点："）
- Tab 切换字段，Enter 确认
- 可关闭（DYNMODE 命令）

---

## 9. 图层系统

### 9.1 图层属性

```csharp
public sealed class LayerEntry {
    public LayerId Id { get; }
    public string Name { get; }
    public Color Color { get; }
    public LineType LineType { get; }                  // Continuous / Dashed / Center / ...
    public LineWeight LineWeight { get; }              // 0.13 / 0.18 / 0.25 mm 等
    public double Transparency { get; }                // 0~1
    public bool IsVisible { get; }
    public bool IsFrozen { get; }
    public bool IsLocked { get; }
    public bool IsPlottable { get; }
    public LayerKind Kind { get; }                     // Geometry / Process / Reference / Construction
}
```

### 9.2 图层状态

- **可见 / 隐藏**：Visible 控制渲染，临时切换
- **冻结 / 解冻**：Frozen 状态下不参与计算（编辑、编译、空间索引），比 Hidden 更彻底
- **锁定 / 解锁**：Locked 防止误编辑（仍可见、仍参与计算）
- **可打印 / 不可打印**：影响导出 PDF / 图片

### 9.3 图层过滤器

按名称模式分组：`Process_*` / `Geom_*` / `Ref_*`，UI 上可分组显示。

### 9.4 图层状态管理

保存"某种图层显示组合"为命名状态，一键切换：

- "调试视图"：只显示几何 + Mark
- "工艺视图"：显示几何 + 全工艺
- "干净视图"：只显示当前编辑层

### 9.5 点胶项目预置图层

新建文档时按机型配置预置：

- `0`（默认层，AutoCAD 兼容）
- `Geom_Outline`：外轮廓
- `Geom_Pad`：焊盘等几何
- `Process_Path`：点胶轨迹
- `Process_Point`：点点胶
- `Process_Fill`：填充
- `Process_Mark`：Mark 点
- `Process_KeepOut`：禁打区
- `Process_Probe`：测高点
- `Ref_Pcb`：PCB 参考底图
- `Construction`：辅助构造线（不导出）

---

## 10. 块（Block）系统

### 10.1 块定义与引用

```csharp
public sealed class BlockDefinition {
    public BlockId Id { get; }
    public string Name { get; }
    public Point2 BasePoint { get; }
    public IReadOnlyList<EntityBase> Entities { get; }
    public IReadOnlyList<AttributeDefinition> Attributes { get; }
    public BlockKind Kind { get; }                     // Static / Dynamic
}

public sealed record BlockReferenceEntity(...) : EntityBase {
    public required BlockId BlockId { get; init; }
    public required Point2 InsertionPoint { get; init; }
    public required Vector2 Scale { get; init; }
    public required Angle Rotation { get; init; }
    public ImmutableDictionary<string, string> AttributeValues { get; init; }
}
```

### 10.2 嵌套块

支持块定义内引用其他块。深度限制（默认 16 层），防止循环引用。导入时检测循环并报错。

### 10.3 动态块（V2 预留）

第一版仅静态块。`BlockKind.Dynamic` 枚举值预留，相关接口位（参数、可变形态）暂不实现。

### 10.4 属性（Attribute）

块上的可变文本字段，例如 IC 块上挂 "RefDes"、"Value" 属性。每个引用可以填不同值。

### 10.5 块编辑器

进入块定义本身做修改：
- 命令 `BEDIT`
- 进入块编辑模式时背景变暗，仅显示当前块定义
- 退出时所有引用同步更新
- 修改影响的引用数量在退出前提示

### 10.6 在点胶项目里的应用

- 一个 IC 封装 = 一个块定义（含 N 个 DispensePoint）
- 整板 100 颗 IC = 100 次块引用
- 改一次封装定义，全板同步更新
- ARRAY 命令基于块引用工作

### 10.7 编译时块的展开

PathCompiler 编译为 IR 时，把所有块引用**展开为独立的 Segment 序列**（应用块的变换矩阵），IR 层面不存在"块"概念。这样下位机/G 代码端不需要理解块语义。

---

## 11. 视图系统

### 11.1 视口（Viewport）

V1 单视口，V2 支持多视口（Split）。

```csharp
public sealed record ViewState(
    Point2 Center,                                     // 视口中心（文档坐标）
    double Zoom,                                       // 单位：像素/mm
    Angle ViewRotation,                                // 视图旋转
    GridSettings Grid,
    AxisDisplay Axis);                                 // 显示坐标轴指示
```

### 11.2 命名视图

```csharp
public sealed record NamedView(string Name, ViewState State, DateTimeOffset SavedAt);
```

`VIEW` 命令管理。

### 11.3 缩放命令

| 子命令 | 含义 |
|--------|------|
| `ZOOM Window` | 框选缩放 |
| `ZOOM Extents` | 缩放到所有图元 |
| `ZOOM All` | 缩放到图限 |
| `ZOOM Previous` | 上一视图 |
| `ZOOM Realtime` | 滚轮 / 拖拽实时 |
| `ZOOM Scale` | 按比例 |
| `ZOOM Object` | 缩放到选中对象 |

### 11.4 平移

- 鼠标中键拖拽
- `PAN` 命令进入实时平移模式（ESC 退出）
- 滚轮 + Ctrl 横向平移

### 11.5 渲染层级（z-order）

从底到顶：

1. 背景色 / 主题底色
2. 参考底图（PCB 图片层，可半透明）
3. 网格
4. 已冻结图层（淡化）
5. 普通图元（按图层 + 实体顺序）
6. 工艺图元（叠在几何上方）
7. 选择高亮 / 夹点
8. 工具反馈（橡皮筋、构造线、吸附图标）
9. 仿真叠加（仿真模式下）
10. 实际轨迹叠加（真机回采）
11. 命令提示浮窗

### 11.6 视图属性

- 背景色（Token 控制，主题感知）
- 网格密度（主网格 + 次网格）
- 坐标轴指示（左下角小图）
- 原点标识
- 标尺（可开关）

### 11.7 LOD 与剔除

- **视锥剔除**：屏幕外图元跳过绘制
- **AABB 早判**：图元 AABB 完全在视口外直接跳过
- **LOD 简化**：缩放过低时小图元（< 1 像素）合并显示为点
- **样条降采样**：根据缩放级别动态调整离散精度
- **批渲染**：同图层同样式的图元合并 draw call

性能目标：10 万图元下平移 / 缩放 60 fps。

### 11.8 双向定位（与 G 代码 / 仿真互联）

- 点击画布图元 → G 代码视图高亮对应行
- 点击 G 代码行 → 画布对应段高亮 + 视图聚焦
- 仿真播放时 → 当前执行段在画布显示"光标"
- 真机执行时 → 画布显示"机器位置"光标

实现依赖 IR 层的 `SourceEntityRef`（每个 Segment 记录来源实体 id）。详见文档 5。

---

## 12. 渲染管线（基于 SkiaSharp）

### 12.1 总体结构

```
DraftingDocument + ViewState
        ↓
   SceneBuilder（脏数据驱动，构建 RenderScene）
        ↓
   RenderScene（不可变快照，含图层栈）
        ↓
   SkRenderer（基于 SkiaSharp）
        ↓
   SKCanvas（嵌在 WPF 的 SKElement / GLWpfControl）
```

### 12.2 RenderScene

```csharp
public sealed class RenderScene {
    public ViewState View { get; }
    public IReadOnlyList<RenderLayer> Layers { get; }   // 图层栈
    public IReadOnlyList<RenderOverlay> Overlays { get; } // 选择 / 仿真 / 回采叠加
}

public sealed class RenderLayer {
    public LayerId Id { get; }
    public IReadOnlyList<RenderItem> Items { get; }
    public LayerStyle Style { get; }
}

public abstract record RenderItem(...);                // Path / Text / Image / Glyph
```

### 12.3 增量更新

- 文档变更事件 → 收到变更的 entity ids → 仅重建受影响的 RenderItem
- ViewState 变更 → 不重建场景，只重新 transform
- 主题切换 → 重建样式映射，几何不变

### 12.4 主题响应

- 颜色全部走 Token，不写死
- 主题切换时，渲染层订阅 `IThemeService.Changes`，重建样式表
- 自定义图层颜色优先于主题（用户已显式指定）

### 12.5 字体渲染

- WPF 端通过 SkTypeface 加载系统字体
- 中文字体 fallback 链
- 大字体缓存（避免每帧重建 Glyph）

### 12.6 抗锯齿

- 几何线条：默认 AA
- 网格：可关 AA（性能）
- 文本：使用 Skia 的 LCD 子像素渲染（屏幕清晰度）

### 12.7 多分辨率

- 高 DPI 自适应（WPF 自动）
- 自定义控件按 DIP 计算尺寸，渲染时乘以 DPI 缩放

### 12.8 屏幕外渲染

- 缩略图、PDF 导出、PNG 导出走相同的 RenderScene 管线
- 离屏 SKSurface，分辨率独立

---

## 13. 编辑反馈

### 13.1 夹点（Grip）

选中后图元上显示蓝色小方块（端点 / 中点 / 控制点 / 中心）。

```csharp
public abstract record Grip(EntityId Owner, Point2 Position, GripKind Kind);

public enum GripKind {
    Endpoint, Midpoint, Center, Quadrant,
    ControlPoint, InsertionPoint, Custom
}
```

拖动夹点直接修改图元（无需进入命令）。Hover 时夹点变色，按 Ctrl 多选夹点联动编辑。

### 13.2 橡皮筋（Rubber Band）

绘制命令进行中显示从上一点到鼠标的辅助线。线型 / 颜色 token 化。

### 13.3 临时构造线

`XLINE` / `RAY` 命令绘制无限延伸的辅助线，可作为吸附目标。

### 13.4 测量浮窗

`DIST` / `AREA` 命令执行结果浮窗显示 + 自动复制到剪贴板（可关）。

### 13.5 命令提示

底部命令行 + 鼠标边小提示：

- "指定第一点："
- "指定下一点（[U]ndo / [E]nter to finish）："
- 浮窗显示当前模式（吸附启用、正交开关、动态输入开关）

### 13.6 Tooltip

鼠标悬停图元 1.5 秒显示：

- 类型（"直线" / "点胶轨迹"）
- 关键属性（长度 / 工艺模板）
- 图层、颜色

可在设置中关闭。

### 13.7 状态栏

底部状态栏显示：

- 当前坐标（按当前坐标系）
- 当前图层
- 吸附状态（哪些 Provider 启用）
- 正交（ORTHO）开关
- 极坐标追踪（POLAR）开关
- 对象追踪（OTRACK）开关
- 网格（GRID）开关
- 动态输入（DYN）开关
- 缩放比例

每个状态可点击切换。快捷键 F3=吸附、F8=正交、F10=极坐标、F7=网格、F12=动态输入（与 AutoCAD 对齐）。

---

## 14. 撤销 / 重做

### 14.1 命令栈结构

```csharp
public interface ICommandHistory {
    bool CanUndo { get; }
    bool CanRedo { get; }
    int UndoDepth { get; }
    int RedoDepth { get; }
    void Push(CommandTransaction tx);
    void Undo();
    void Redo();
    void Clear();
    event Action? Changed;
}
```

每个 `CommandTransaction` 是用户级的一步操作。底层多次 entity 修改合并成一个 transaction。

### 14.2 复合命令

复杂命令（如 ARRAY 一次产生 100 个块引用）打包为一个 transaction，Undo 一次回滚。

### 14.3 局部撤销（V2 预留）

针对单个图元的撤销链路（与全局 Undo 栈隔离），V1 不做。

### 14.4 跨会话策略

- 关闭文档时 / 保存时 → 清空 Undo 栈
- 自动保存不影响 Undo 栈
- 重新打开文档时 → Undo 栈空

### 14.5 内存管理

- 默认无限深度，但每个 transaction 应只存"差异"而非完整快照
- 每个 transaction 估算内存占用，达到上限（默认 200 MB）开始裁剪最旧
- 文档关闭释放栈

### 14.6 与外部变更的协同

- 视图变更（缩放、平移）**不进** Undo 栈
- 选择变更**不进** Undo 栈
- 图层属性修改**进**栈
- 工艺模板修改**进**栈
- 标定数据变更**不进**栈（属于全局，由 ICalibrationService 管）

### 14.7 撤销期间的事件抑制

Undo / Redo 触发的变更事件携带 `IsReplay = true` 标志，避免某些副作用（如自动保存触发、IR 重新编译）重复发生。

---

## 15. 参数化绘图（V1 接口预留）

第一版**不实现**，但接口与数据结构必须预留，避免后期推倒重来。

### 15.1 设计目标

让用户可以这样定义图形：

- "一行 N 个点，间距 S，N 和 S 改了图自动更新"
- "矩形宽高与文档参数 `BoardWidth` / `BoardHeight` 联动"
- "圆心约束在某条线的中点"

### 15.2 几何约束（V2 接口预留）

```csharp
public abstract record GeometricConstraint(ConstraintId Id);

public sealed record CoincidentConstraint(...) : GeometricConstraint;     // 重合
public sealed record ColinearConstraint(...) : GeometricConstraint;       // 共线
public sealed record ParallelConstraint(...) : GeometricConstraint;       // 平行
public sealed record PerpendicularConstraint(...) : GeometricConstraint;  // 垂直
public sealed record TangentConstraint(...) : GeometricConstraint;        // 相切
public sealed record EqualLengthConstraint(...) : GeometricConstraint;    // 等长
public sealed record SymmetricConstraint(...) : GeometricConstraint;      // 对称
public sealed record HorizontalConstraint(...) : GeometricConstraint;     // 水平
public sealed record VerticalConstraint(...) : GeometricConstraint;       // 垂直 (轴)
public sealed record FixedConstraint(...) : GeometricConstraint;          // 固定
```

### 15.3 尺寸约束

```csharp
public abstract record DimensionalConstraint(ConstraintId Id, string Expression);

public sealed record DistanceConstraint(...) : DimensionalConstraint;
public sealed record AngleConstraint(...) : DimensionalConstraint;
public sealed record RadiusConstraint(...) : DimensionalConstraint;
public sealed record DiameterConstraint(...) : DimensionalConstraint;
```

`Expression` 可引用文档参数表 `$Pitch`、`$Count` 等。

### 15.4 用户参数表

```csharp
public sealed class ParameterTable {
    public IReadOnlyDictionary<string, ParameterDefinition> Parameters { get; }
    public Task SetAsync(string name, string expression, CancellationToken ct);
    public double? Evaluate(string name);
}

public sealed record ParameterDefinition(
    string Name,
    string Expression,
    ParameterKind Kind,                                // Length / Angle / Count / Boolean
    string? Description,
    string? Group);
```

### 15.5 约束求解器接口（V1 提供 Null 实现）

```csharp
public interface IConstraintSolver {
    SolveResult Solve(DraftingDocument doc, IReadOnlyList<GeometricConstraint> geo,
                      IReadOnlyList<DimensionalConstraint> dim, ParameterTable parameters,
                      SolverOptions opts);
}

public sealed class NullConstraintSolver : IConstraintSolver {
    public SolveResult Solve(...) => SolveResult.NotImplemented;
}
```

V2 候选实现：自研牛顿迭代 / `Z3 .NET` / `PlaneGCS`（OpenCascade 子项目）。

### 15.6 参数化命令（V2 启用）

预留命令名：

- `PARAMETERS` / `PARAM`：打开参数表对话框
- `CONSTRAIN` / `CON`：添加几何约束
- `DIMCONSTRAIN` / `DCON`：添加尺寸约束
- `SHOWCONSTRAINTS`：显示所有约束图标
- `DELCONSTRAINT`：删除约束

V1 不在正式命令注册表中启用这些命令，只在文档和保留关键字表中占位。开发 / 调试构建可注册隐藏命令并返回"V2 启用"提示，避免操作员 UI 出现不可用命令。

---

## 16. 文件 I/O（Drafting.IO）

### 16.1 原生格式（.dpdoc）

参见 §2.3。读写器接口：

```csharp
public interface IDocumentSerializer {
    Task<DraftingDocument> ReadAsync(Stream src, DocumentReadOptions opts, CancellationToken ct);
    Task WriteAsync(DraftingDocument doc, Stream dst, DocumentWriteOptions opts, CancellationToken ct);
    string FormatId { get; }                           // "dpdoc" / "dxf" / "json"
    int SchemaVersion { get; }
}
```

### 16.2 DXF 导入（netDxf）

支持的图元：

- POINT / LINE / LWPOLYLINE / POLYLINE / ARC / CIRCLE / ELLIPSE / SPLINE
- INSERT（块引用）+ BLOCK 表
- LAYER 表
- HATCH（部分图案）
- TEXT / MTEXT
- DIMENSION（基础类型）

不支持或部分支持的：

- 3D 实体 / 网格（忽略）
- 区域（REGION） — V2
- 视图块、视口（VIEWPORT） — 忽略
- 多线（MLINE） — 转 LWPOLYLINE
- 自定义对象（XRecord、Custom Entity） — 保留为附加元数据，不渲染

### 16.3 DXF 导入向导

分步对话框，让用户确认：

1. **预览**：以默认参数渲染缩略图
2. **单位**：mm / inch / unitless（默认按 INSUNITS）
3. **原点**：保留原点 / 移到选定基准 / 重心居中
4. **图层映射**：把 DXF 图层映射到本系统图层（按颜色 / 名称模式）
   - 红色图层 → Process_Path
   - 蓝色图层 → Process_Mark
   - 绿色图层 → Process_KeepOut
   - 自定义规则可保存为预设
5. **采样精度**：样条 / 椭圆离散弦高公差（默认 0.001 mm）
6. **简化**：是否对短线段（< N μm）合并
7. **块处理**：保留块定义 / 全部炸开
8. **完成**：显示导入统计（图元数 / 跳过数 / 警告数）

### 16.4 DXF 导出

写出当前文档的几何部分。工艺图元**不写入 DXF**（DXF 不认识），只写入 `.dpdoc` 内的 JSON。但可以选择：

- 把 DispensePath 写为带特殊图层名的 LWPOLYLINE（外部 CAD 可见）
- 把 MarkPoint 写为 POINT
- 把 KeepOutZone 写为 LWPOLYLINE 闭合多边形

便于客户用 AutoCAD 校对。

### 16.5 PDF / 图片导出

- PDF：单页或多页，含图层、可缩放矢量
- PNG / JPG：可指定分辨率、是否含背景、是否含网格
- SVG：矢量导出，便于嵌入文档
- 离屏 SkSurface 渲染，与画布共用 RenderScene 管线

### 16.6 参考底图导入

导入位图（PNG / JPG / TIFF）作为参考底图：

- 放在 `Ref_*` 图层
- 支持位置 + 缩放 + 旋转
- 半透明显示
- 不参与几何运算
- 用于电路板照片对位、客户图纸扫描

### 16.7 Gerber / ODB++（V2 预留）

第一版不做，但接口预留：

```csharp
public interface IDocumentImporter {
    string FormatId { get; }
    bool CanImport(Stream src);
    Task<DraftingDocument> ImportAsync(Stream src, ImportOptions opts, CancellationToken ct);
}
```

V2 通过实现该接口扩展。

### 16.8 错误处理与日志

- 导入失败有明确错误（哪一行 / 哪个对象 / 什么原因）
- 部分成功允许（导入 95% + 警告 5% 跳过的对象）
- 所有导入产生 ImportReport，可展示给用户、可写入审计

---

## 17. 工艺规划工作流

### 17.1 从几何到工艺的链路

```
DXF / 用户绘制几何
        ↓
   选择几何（多种选择方式）
        ↓
   套用工艺命令（DPATH / DPOINT / DFILL / MARK / ...）
        ↓
   工艺实体附加到几何
        ↓
   用工艺模板批量赋值参数
        ↓
   路径顺序优化
        ↓
   抬笔策略规划
        ↓
   编译为 IR（PathCompiler）
        ↓
   仿真预览 / G 代码生成
```

### 17.2 工艺模板库

```csharp
public sealed record ProcessTemplate(
    ProcessTemplateId Id,
    string Name,
    string Description,
    DispenseRecipeData Recipe,                         // 速度 / 压力 / 温度 / 抬笔等
    DateTimeOffset CreatedAt,
    string CreatedBy,
    int Version);

public interface IProcessTemplateLibrary {
    IReadOnlyList<ProcessTemplate> All { get; }
    Task<ProcessTemplate> AddAsync(ProcessTemplate t, CancellationToken ct);
    Task UpdateAsync(ProcessTemplate t, CancellationToken ct);
    Task<ProcessTemplate?> GetAsync(ProcessTemplateId id, CancellationToken ct);
    IObservable<TemplateChangedEvent> Changes { get; }
}
```

工艺模板与配方独立管理（一个工艺模板可以跨多个配方共用）。

修改工艺模板会影响所有引用它的工艺图元（"Update on Save" 提示）。

### 17.3 路径顺序优化

`OPTIMIZE` 命令对当前选中的工艺图元做顺序优化：

- 模式：最近邻 / 2-opt / 用户指定起点
- 范围：全文档 / 当前图层 / 当前选择集
- 约束：保持组内顺序、跨工位独立优化、按工艺模板分组
- 结果：显示优化前后的总移动距离 / 估算节拍 / 可一键回滚

### 17.4 拐角与抬笔策略

每段工艺路径标注 `TransitionPolicy`，决定：

- 直角 / 圆角 / 斜角过渡
- 拐角速度
- 是否抬笔（Z 抬起 + 关阀）

抬笔策略规划命令 `LIFTPLAN`：

- 自动判定哪些段需要抬笔（夹角过大 / 距离过远 / 跨 KeepOutZone）
- 用户可以加白名单（必须抬笔的段）
- 用户可以加黑名单（即使长距离也不抬笔）

### 17.5 起止点策略

每段路径可定义起止点专属参数：

- **起点**：开阀延时、起点拖尾速度、起点抬笔高度
- **终点**：关阀提前量、终点拖尾、回吸时长

由 `StartEndOptions` 承载。

### 17.6 KeepOutZone 应用

KeepOutZone 在编译时影响路径：

- **跳过**：路径段进入禁打区时直接跳过该段
- **抬笔**：路径在边界抬笔，跨过禁打区后落笔继续
- **重新规划**：调用偏移 / 布尔运算重新生成绕行路径

策略由 `KeepOutPolicy` 决定。

### 17.7 多 Mark 点对位补偿

视觉对位完成后，计算实际位置与名义位置的偏差，得到一个 2D 仿射变换（或刚体变换 + 缩放）。

`PathCompiler` 在编译时把这个变换嵌入 IR Header 的 `Frame`，下位机执行时统一应用，不需要修改路径数据本身。

### 17.8 工艺校验

编译前后端到端校验：

- 路径速度是否在硬件能力范围
- 拐角速度是否合理
- 抬笔高度是否安全
- 工艺模板参数是否完整
- KeepOutZone 是否被正确处理
- 起止点参数是否冲突

校验失败 → 编辑器内提示 + 不允许下发。

---

## 18. G 代码视图集成

### 18.1 视图位置与切换

G 代码视图作为 `Modules.Drafting` 的一个 Tab（或可拆分面板），用户可显隐。

UI 布局：

```
┌─────────────────────────────────┬───────────────┐
│                                 │ ▣ G 代码      │
│        画布主区                  │ ▢ 属性        │
│       （SkiaSharp）               │ ▢ 图层        │
│                                 │ ▢ 工艺        │
│                                 │ ▢ 仿真        │
│                                 │               │
└─────────────────────────────────┴───────────────┘
```

### 18.2 G 代码视图功能

- 只读浏览（默认）
- 行号、语法高亮、折叠
- 搜索 / 替换（只读模式仅搜索）
- 当前行同步：仿真播放或真机执行时高亮当前段对应的 G 代码行

### 18.3 双向高亮联动

机制：所有 G 代码行写入时附 `irHash + segmentIndex` 注释（或独立映射表）：

```
; SEG=0042 IR=ab12cd34
G1 X10 Y20 F100
```

UI 选中：

- 点击画布图元 → 找到关联的 segmentIndex 集合 → 反查 G 代码行 → 高亮
- 点击 G 代码行 → 解析 segmentIndex → 反查 IR 中的源 entity id → 在画布高亮 + 居中

### 18.4 编辑模式（专家模式）

- 默认锁定，"开启编辑"按钮带二次确认
- 进入编辑模式后顶部黄色横条警告："G 代码已脱离画布同步，重新编译会覆盖"
- 编辑后视图状态变为 `Detached`，画布上显示"G 代码已偏离 IR"提示
- V1 不提供"应用回画布"。编辑后的控制器程序只作为 `Detached` 派生产物保存和导出，不能直接下发为受控生产程序；如需纳入受控流程，必须回到画布 / IR 重新编译。

### 18.5 导出 G 代码文件

- 一键导出 `.nc`（Beckhoff）/ `.acspl`（ACS）/ `.pmc`（PMAC）
- 文件名格式：`<recipeName>_v<recipeVersion>_<irHash8>.nc`
- 文件头自动加注释：生成时间、用户、配方、IR hash、控制器目标
- 可附加为现场服务诊断包的一部分

### 18.6 AvalonEdit 集成

- 自定义语法高亮规则（`.xshd` 文件）：G 代码 / ACSPL+ / PMAC 各一份
- 行号面板
- 折叠：按 `;`---- block 注释折叠 / 按 SEG 注释段折叠
- 主题响应：编辑器配色与 Token 联动

### 18.7 大文件性能

- 几十万行 G 代码时启用虚拟化
- 解析与高亮异步进行，避免阻塞 UI
- 双向高亮采用稀疏映射表（`SegmentIndex → LineNumber`），避免每次扫描

---

## 19. 测试策略

### 19.1 几何运算单元测试（Drafting.Geometry.Tests）

覆盖：

- 基本运算（距离、相交、投影）
- 偏移算法（含病态情形：自相交、零半径、共线）
- 布尔运算（含退化场景）
- 样条采样精度
- 路径优化结果稳定性
- 数值稳健性（极小坐标 / 极大坐标 / 接近平行）

工具：xUnit + FsCheck（属性测试，对几何尤其有效）。

### 19.2 命令端到端测试（Drafting.Commands.Tests）

每个命令一组测试：

- 正常路径
- 用户中途取消
- 输入无效参数
- 选中集为空
- 命令栈正确入栈与回滚

实现技巧：用一个 mock 的 `ICommandPrompter` 输入预设序列，断言文档最终状态。

### 19.3 文档 I/O 测试

- DXF 标准用例集（多个版本：R12 / 2000 / 2018）
- 读 → 写 → 读 往返一致性
- `.dpdoc` 序列化往返
- 自动保存崩溃恢复

### 19.4 渲染回归测试

关键用例的画面截图 baseline：

- 基础几何渲染
- 工艺图元叠加
- 主题切换
- 大数据量（10k / 100k 图元）

每次 PR 截图比对（容忍像素差异阈值），差异超过阈值人工 review。

工具：Verify.Xunit（黄金文件）+ SkiaSharp 离屏渲染。

### 19.5 编辑器集成测试

端到端用例：

- 新建文档 → DXF 导入 → 命令绘制 → 套用工艺 → 编译为 IR → 保存 → 重新打开 → 比对
- 撤销 / 重做的最终状态等于不做任何操作
- 命令宏脚本执行结果稳定

### 19.6 性能基准

BenchmarkDotNet 测试：

- 10k / 100k 图元的渲染帧时间
- 吸附查询延迟
- 路径优化算法用时
- DXF 导入用时

性能 baseline 写入 `tests/Benchmarks/baseline.json`，PR 性能下降 > 20% 自动报警。

### 19.7 仿真器联合测试

`Drafting + Process.Compiler + Process.Simulation + Hal.Simulator` 端到端：

- 设计 → 编译 → 仿真 → 仿真硬件执行 → 比对
- 真机回采闭环（仿真版）

详见文档 5。

### 19.8 测试数据管理

- 标准用例集放在 `tests/TestData/`
- DXF 样例覆盖各版本与典型工艺场景（PCB 点胶、IC 底填、芯片粘接）
- 大数据量样例独立子目录，CI 跳过、本地手动跑
- 黄金文件（renders / serializations）随测试一起提交 Git

---

## 附录 A — 逻辑命名空间

```
DispensingPlatform.Drafting.Core
├─ Documents/             # DraftingDocument, DocumentStore
├─ Entities/              # EntityBase, components
├─ Layers/                # LayerEntry, LayerTable
├─ Blocks/                # BlockDefinition, BlockReference
├─ Tables/                # StyleTable, UcsTable, ParameterTable
├─ Views/                 # ViewState, NamedView
└─ Events/                # 文档变更事件

DispensingPlatform.Drafting.Geometry
├─ Primitives/            # Point2, Segment2, Arc2, Bezier2, AABB
├─ Polygons/              # Polygon2, 偏移与布尔
├─ Curves/                # Spline / NURBS
├─ Spatial/               # R-Tree, Quadtree
├─ Algorithms/            # 优化、拐角、采样
└─ Tolerances/            # GeometryTolerance

DispensingPlatform.Drafting.Snap
├─ Engine/                # ISnapEngine 实现
├─ Providers/             # 各种 Provider
└─ Visuals/               # 吸附图标元数据

DispensingPlatform.Drafting.Commands
├─ Registry/              # ICommandRegistry
├─ Drawing/               # 绘制命令
├─ Modify/                # 修改命令
├─ Query/                 # 查询命令
├─ View/                  # 视图命令
├─ Layer/                 # 图层命令
├─ Process/               # 工艺命令
├─ History/               # CommandTransaction, IReversibleChange
├─ Prompt/                # ICommandPrompter
└─ Macro/                 # 宏脚本

DispensingPlatform.Drafting.Rendering
├─ Scene/                 # RenderScene, RenderLayer, RenderItem
├─ Skia/                  # SkRenderer, SkTypeface 缓存
├─ Themes/                # 主题映射
└─ Export/                # PDF / PNG / SVG 导出

DispensingPlatform.Drafting.Interaction
├─ Tools/                 # 工具基类与具体工具
├─ Grips/                 # 夹点系统
├─ Input/                 # 坐标输入解析、表达式求值
└─ Status/                # 状态栏交互

DispensingPlatform.Drafting.IO
├─ Native/                # .dpdoc
├─ Dxf/                   # netDxf 集成
├─ Pdf/                   # PDF 导出
├─ Image/                 # PNG / SVG
├─ Bitmap/                # 参考底图
└─ Importers/             # IDocumentImporter 扩展

DispensingPlatform.Drafting.Process
├─ Entities/              # 工艺图元
├─ Templates/             # ProcessTemplate
├─ Recipes/               # DispenseRecipeData
├─ Annotations/           # 工艺批注、标记
└─ Validation/            # 工艺校验
```

---

## 附录 B — 关键性能目标

| 场景 | 目标 |
|------|------|
| 渲染（10k 图元） | 60 fps |
| 渲染（100k 图元） | 30 fps |
| 吸附查询 | < 1 ms |
| 选择查询（窗交） | < 5 ms |
| 命令执行（普通） | < 10 ms |
| DXF 导入（1 万图元） | < 3 s |
| 路径优化（1k 段） | < 500 ms |
| 编译为 IR（1 万 segment） | < 2 s |

---

## 附录 C — 与其他文档的关系

- **文档 1 Architecture**：编辑器在整体分层中的位置
- **文档 2 Solution-Structure**：Drafting 子系统的目录占位和按需项目化规则
- **文档 3 Core-Contracts**：工艺图元参考的服务接口（IRecipeService、ICalibrationService 等）
- **文档 5 Sync-Mechanism**：编辑器编译为 IR 后的下游链路（仿真 / G 代码 / 真机回采）
- **文档 7 Data-Persistence**：工艺模板库的持久化
- **文档 8 Design-System**：编辑器 UI 的 Token 应用、命令面板、状态栏样式
