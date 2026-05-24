# 文档 8 — UI 设计系统（Design-System.md）

> 版本：v0.1 · 最后更新：2026-05-20

本文规定整个 UI 的视觉与交互一致性来源：设计 Token、主题、控件库混用、双 UI（工程师 / 操作员）切换、关键页面骨架。这不是美工指南，而是工程文档——让任意一个新模块进来都能"自动好看"，让主题切换、双 UI 切换、国际化都不需要改业务代码。

---

## 1. 设计目标

### 1.1 现代美观

参考 Microsoft Fluent 2、Material Design 3、Apple HIG 这一代设计语言，目标体验：

- 干净，留白舒适
- 圆角柔和（不是扁平也不是 Skeuomorphism）
- 层次感来自阴影 + 透明度，不是粗边框
- 微动效（Hover / Press / 状态变化的过渡）
- 可读性放在装饰之上

### 1.2 双 UI 切换

按文档 1 §6.1 已敲定：

- **工程师 UI**：信息密集型，参数全开放，调试工具齐全
- **操作员 UI**：流程化，按钮大，关键状态突出，少干扰

两套 UI 优先共享领域模型、服务和可复用的 ViewModel 基类；简单页面可以共享同一个 ViewModel，仅 View（XAML）不同。对于工程师 / 操作员命令、权限、布局差异很大的页面，允许拆成 `EngineerViewModel` / `OperatorViewModel`，避免一个 ViewModel 过胖。切换由权限驱动，运行时切换不重启。

### 1.3 主题切换

至少四套主题，运行时切换不重启：

- 亮色（Light）
- 暗色（Dark）
- 高对比（HighContrast，强光环境 / 远距离）
- 工业产线（Industrial，大字 + 强对比，操作员主用）

主题切换走自研 Token 系统（文档 1 已敲定）。

### 1.4 工业可读性

工业现场环境特殊：

- 强光（车间天花板照度 > 500 lux）
- 远距离（操作员 1.5–2 米看屏幕）
- 戴手套（触摸目标至少 12 mm 直径）
- 视力多样（一线工人视力差异大）
- 显示器素质参差（老旧 TN 屏、低色域、不校色）

设计 Token 的"工业产线"主题就是为这些场景调优。

### 1.5 一致性高于个性

同一应用内任何两个相似元素必须**视觉完全一致**。新模块开发时**不允许**自定义颜色字号，必须引用 Token。

### 1.6 无障碍

V1 起步要求：

- 所有可点击元素键盘可达（Tab 顺序合理）
- 焦点环明显
- 颜色对比度 ≥ WCAG AA（4.5:1 文本、3:1 大字 / 图标）
- 关键状态不仅靠颜色（图标 + 文字辅助）

V2 引入屏幕阅读器支持。

---

## 2. 设计 Token 三层体系

### 2.1 总体结构

```
┌──────────────────────────────────────┐
│ ③ 组件 Token（Button.Primary.Bg）    │  ← 控件直接引用
└─────────────┬────────────────────────┘
              │ 引用
┌─────────────▼────────────────────────┐
│ ② 语义 Token（Color.Action.Primary）  │  ← 业务语义层
└─────────────┬────────────────────────┘
              │ 引用
┌─────────────▼────────────────────────┐
│ ① 原始 Token（Color.Blue.500 = #...）│  ← 数值定义层
└──────────────────────────────────────┘
```

主题切换只换**第二层映射**和**部分第一层**。组件 Token 不变。

### 2.2 第一层 — 原始 Token

纯数值 / 颜色，无业务含义，命名规范：

```
Color.<Hue>.<Shade>          颜色阶梯：50 100 200 ... 900 950
Spacing.<Step>               4 8 12 16 24 32 48 64 96
FontSize.<Step>              12 13 14 16 18 20 24 28 32 40 48
LineHeight.<Step>            16 20 24 28 32 36 40 48 56 64
Radius.<Name>                None Small Medium Large Pill
Shadow.<Level>               None Sm Md Lg Xl 2xl
Duration.<Step>              Fast Normal Slow
Easing.<Name>                Standard Decelerate Accelerate
StrokeWidth.<Step>           1 2 3 4
ZIndex.<Name>                Base Tooltip Popup Dialog Toast Top
```

#### 颜色阶梯（示例）

```
Color.Neutral
├─ 0       #FFFFFF
├─ 50      #FAFAFA
├─ 100     #F5F5F5
├─ 200     #E5E5E5
├─ 300     #D4D4D4
├─ 400     #A3A3A3
├─ 500     #737373
├─ 600     #525252
├─ 700     #404040
├─ 800     #262626
├─ 900     #171717
└─ 950     #0A0A0A

Color.Brand
├─ 50  100 200 ... 900 950   # 客户品牌色（通过 branding 配置覆盖）

Color.Blue / Green / Yellow / Orange / Red / Purple / Cyan
└─ 完整阶梯
```

#### 间距阶梯

```
Spacing.0   = 0
Spacing.1   = 4 px
Spacing.2   = 8 px
Spacing.3   = 12 px
Spacing.4   = 16 px        ← 默认基线
Spacing.5   = 20 px
Spacing.6   = 24 px
Spacing.8   = 32 px
Spacing.10  = 40 px
Spacing.12  = 48 px
Spacing.16  = 64 px
Spacing.24  = 96 px
```

#### 字号阶梯

```
FontSize.Caption      = 12
FontSize.Body         = 14
FontSize.BodyLarge    = 16
FontSize.Subtitle     = 18
FontSize.Title        = 20
FontSize.TitleLarge   = 24
FontSize.Heading      = 28
FontSize.HeadingLarge = 32
FontSize.Display      = 40
FontSize.DisplayLarge = 48
```

#### 圆角

```
Radius.None      = 0
Radius.Small     = 4
Radius.Medium    = 8     ← 默认按钮 / 卡片
Radius.Large     = 12
Radius.XLarge    = 16
Radius.Pill      = 9999
```

#### 阴影（Elevation）

```
Shadow.None  = none
Shadow.Sm    = 0 1px 2px rgba(0,0,0,0.05)
Shadow.Md    = 0 2px 4px rgba(0,0,0,0.08)
Shadow.Lg    = 0 4px 12px rgba(0,0,0,0.10)
Shadow.Xl    = 0 8px 24px rgba(0,0,0,0.14)
Shadow.2xl   = 0 16px 48px rgba(0,0,0,0.20)
```

暗色主题阴影更柔（光感不同）。

#### 动效

```
Duration.Fast   = 100 ms
Duration.Normal = 200 ms
Duration.Slow   = 320 ms

Easing.Standard    = cubic-bezier(0.4, 0, 0.2, 1)
Easing.Decelerate  = cubic-bezier(0,   0, 0.2, 1)
Easing.Accelerate  = cubic-bezier(0.4, 0, 1,   1)
Easing.Emphasized  = cubic-bezier(0.2, 0, 0,   1)
```

### 2.3 第二层 — 语义 Token

把"业务语义"映射到原始 Token：

#### Action（行动颜色）

```
Color.Action.Primary           → Color.Brand.500
Color.Action.PrimaryHover      → Color.Brand.600
Color.Action.PrimaryPressed    → Color.Brand.700
Color.Action.PrimaryDisabled   → Color.Brand.300
Color.Action.Secondary         → Color.Neutral.200
Color.Action.SecondaryHover    → Color.Neutral.300
Color.Action.Danger            → Color.Red.500
Color.Action.DangerHover       → Color.Red.600
```

#### Status（状态颜色）

```
Color.Status.Idle              → Color.Neutral.400
Color.Status.Running           → Color.Green.500
Color.Status.Paused            → Color.Yellow.500
Color.Status.Alarm             → Color.Red.500
Color.Status.Maintenance       → Color.Purple.500
Color.Status.Offline           → Color.Neutral.500
```

#### Alarm（按报警等级）

```
Color.Alarm.Fatal              → Color.Red.700
Color.Alarm.Critical           → Color.Red.500
Color.Alarm.Warning            → Color.Orange.500
Color.Alarm.Info               → Color.Blue.500
```

#### Sync（同步状态，文档 5 §10）

```
Color.Sync.InSync              → Color.Green.500
Color.Sync.Stale               → Color.Yellow.500
Color.Sync.Detached            → Color.Orange.500
Color.Sync.Failed              → Color.Red.500
```

#### Surface（表面层）

```
Color.Surface.Background       → Color.Neutral.0   (亮)  / Color.Neutral.950 (暗)
Color.Surface.Panel            → Color.Neutral.50  / Color.Neutral.900
Color.Surface.Elevated         → Color.Neutral.0   / Color.Neutral.800
Color.Surface.Overlay          → rgba(0,0,0,0.4)   / rgba(0,0,0,0.6)
Color.Surface.Border           → Color.Neutral.200 / Color.Neutral.700
Color.Surface.Divider          → Color.Neutral.100 / Color.Neutral.800
```

#### Text（文字）

```
Color.Text.Primary             → Color.Neutral.900 / Color.Neutral.50
Color.Text.Secondary           → Color.Neutral.600 / Color.Neutral.300
Color.Text.Tertiary            → Color.Neutral.500 / Color.Neutral.400
Color.Text.Disabled            → Color.Neutral.300 / Color.Neutral.600
Color.Text.OnPrimary           → Color.Neutral.0
Color.Text.OnDark              → Color.Neutral.50
Color.Text.Link                → Color.Action.Primary
Color.Text.LinkHover           → Color.Action.PrimaryHover
```

#### Spacing 语义

```
Spacing.PageGutter             → Spacing.6
Spacing.SectionGap             → Spacing.8
Spacing.GroupGap               → Spacing.4
Spacing.ItemGap                → Spacing.2
Spacing.Inset                  → Spacing.4
Spacing.InsetCompact           → Spacing.2
Spacing.InsetLoose             → Spacing.6
```

#### Typography

```
Type.Display.Large             { FontSize.DisplayLarge, LineHeight.10, FontWeight.Bold }
Type.Display.Medium            { FontSize.Display, LineHeight.10, FontWeight.Bold }
Type.Heading.Large             { FontSize.HeadingLarge, LineHeight.10, FontWeight.SemiBold }
Type.Heading.Medium            { FontSize.Heading, LineHeight.9, FontWeight.SemiBold }
Type.Title.Large               { FontSize.TitleLarge, ..., FontWeight.SemiBold }
Type.Title.Medium              { FontSize.Title, ..., FontWeight.SemiBold }
Type.Body.Large                { FontSize.BodyLarge, ..., FontWeight.Regular }
Type.Body.Medium               { FontSize.Body, ..., FontWeight.Regular }
Type.Caption                   { FontSize.Caption, ..., FontWeight.Regular }
Type.Code                      { FontSize.Body, FontFamily.Mono, FontWeight.Regular }
```

### 2.4 第三层 — 组件 Token

每个 WPF 控件一组完整的状态映射：

```
Button.Primary.Background.Default      → Color.Action.Primary
Button.Primary.Background.Hover        → Color.Action.PrimaryHover
Button.Primary.Background.Pressed      → Color.Action.PrimaryPressed
Button.Primary.Background.Disabled     → Color.Action.PrimaryDisabled
Button.Primary.Foreground.Default      → Color.Text.OnPrimary
Button.Primary.Border.Default          → Transparent
Button.Primary.Radius                  → Radius.Medium
Button.Primary.Padding                 → { Spacing.4, Spacing.3 }   (横, 竖)
Button.Primary.MinHeight               → 36

Button.Secondary.Background.Default    → Color.Action.Secondary
Button.Secondary.Foreground.Default    → Color.Text.Primary
Button.Secondary.Border.Default        → Color.Surface.Border
...

Button.Danger...   ButtonGhost...    IconButton...    SplitButton...

TextBox.Background.Default             → Color.Surface.Elevated
TextBox.Border.Default                 → Color.Surface.Border
TextBox.Border.Focus                   → Color.Action.Primary
TextBox.Foreground.Default             → Color.Text.Primary
TextBox.Placeholder                    → Color.Text.Tertiary
TextBox.Radius                         → Radius.Medium
TextBox.MinHeight                      → 36

Card.Background                        → Color.Surface.Panel
Card.Border                            → Color.Surface.Border
Card.Radius                            → Radius.Large
Card.Shadow                            → Shadow.Sm
Card.Padding                           → Spacing.6

Tooltip / Popover / Toast / DataGrid.Row / ListItem ...
```

每个控件 Token 完整覆盖 default / hover / pressed / focused / disabled / selected 状态。

### 2.5 命名约定

- `<分类>.<对象>.<状态/属性>`
- PascalCase
- 不允许临时新增（必须先在 Token 文档登记）
- 不允许在 XAML 里写死颜色 / 字号 / 间距

---

## 3. 主题清单

四套主题，每套独立的 ResourceDictionary：

```
src/DesignSystem/Themes/
├─ Themes/
│   ├─ Light.xaml
│   ├─ Dark.xaml
│   ├─ HighContrast.xaml
│   └─ Industrial.xaml
└─ ThemeResources/
    ├─ Primitives.xaml          # 第一层共用部分
    └─ Components.xaml          # 第三层（所有主题共用）
```

### 3.1 亮色主题（Light）

- 默认主题，办公环境与开发期主用
- 背景偏白，强调对比度温和
- 阴影柔
- 字体：思源黑体 / Inter

### 3.2 暗色主题（Dark）

- 暗背景（`#171717`），文字反相
- 弱化纯白（避免眼疲劳）
- 阴影更柔（光感不同）
- 工程师调试时常用

### 3.3 高对比主题（HighContrast）

- 黑底白字 + 高饱和强调色
- 用于强光环境
- 字号统一 +1 阶
- 边框统一 2px
- WCAG AAA（7:1）对比度

### 3.4 工业产线主题（Industrial）

- 操作员主用
- 字号统一 +1–2 阶
- 按钮高度 +20%（戴手套友好）
- 状态色块更大
- 低密度（间距 +1 阶）
- 移除装饰性元素

### 3.5 主题文件结构

每套主题一份完整的 Primitives + Semantic + Components：

```xml
<ResourceDictionary>
  <!-- 第一层（少量主题差异，如 Color.Surface.* 颜色不同） -->
  <Color x:Key="Color.Neutral.0">#FFFFFF</Color>
  ...

  <!-- 第二层 -->
  <Color x:Key="Color.Action.Primary">{StaticResource Color.Brand.500}</Color>
  ...

  <!-- 第三层 -->
  <SolidColorBrush x:Key="Button.Primary.Background.Default"
                   Color="{DynamicResource Color.Action.Primary}"/>
  ...
</ResourceDictionary>
```

### 3.6 主题元数据

每个主题带元数据：

```csharp
public sealed record ThemeDescriptor(
    ThemeId Id,                    // Light / Dark / HighContrast / Industrial
    string DisplayName,            // i18n
    Uri ResourceUri,
    bool RequiresHighContrast,     // 系统级高对比开关
    UiDensity Density,             // Compact / Normal / Comfortable / Industrial
    double FontScale);             // 1.0 / 1.1 / 1.2
```

`IThemeService.Available` 返回当前可选主题。

### 3.7 客户品牌覆盖

`configs/<customer>/_shared/branding/`：

```json
{
  "brandColor": "#0066CC",
  "logoLight": "logo-light.png",
  "logoDark": "logo-dark.png",
  "appTitle": "XYZ 点胶系统"
}
```

启动时把 `Color.Brand.*` 阶梯按客户色重新生成（基于色相旋转 + 明度调整算法）。

### 3.8 主题切换的不重启实现

- WPF 用 DynamicResource 引用 Token
- 切换时清空 App.Resources.MergedDictionaries 中的主题 dict
- 加载新主题 dict
- 所有 DynamicResource 自动响应

仅极少数自定义控件（SkiaSharp 画布）需要手动订阅 `IThemeService.Changes` 重绘。

### 3.9 持久化

用户偏好持久化到 system.db settings 表（`ui.theme = 'Dark'`）。下次启动恢复。

无偏好时按系统级判断（Windows 系统主题）。

---

## 4. Token 实现细节

### 4.1 ResourceDictionary 组织

```
src/DesignSystem/Tokens/
├─ Primitives/
│   ├─ Colors.xaml
│   ├─ Spacing.xaml
│   ├─ Typography.xaml
│   ├─ Radius.xaml
│   ├─ Shadows.xaml
│   ├─ Durations.xaml
│   └─ Easings.xaml
├─ Semantic/
│   ├─ Action.xaml
│   ├─ Status.xaml
│   ├─ Alarm.xaml
│   ├─ Sync.xaml
│   ├─ Surface.xaml
│   ├─ Text.xaml
│   └─ Typography.xaml
└─ Components/
    ├─ Button.xaml
    ├─ TextBox.xaml
    ├─ Card.xaml
    ├─ Tooltip.xaml
    ├─ DataGrid.xaml
    └─ ...
```

### 4.2 DynamicResource 引用

控件样式只引用 Token，不写死值：

```xml
<Setter Property="Background" Value="{DynamicResource Button.Primary.Background.Default}"/>
<Setter Property="Padding"    Value="{DynamicResource Button.Primary.Padding}"/>
<Setter Property="MinHeight"  Value="{DynamicResource Button.Primary.MinHeight}"/>
```

使用 DynamicResource（不是 StaticResource）保证主题切换不重启。

### 4.3 Token 在代码中引用

VM 中需要颜色 / 间距时：

```csharp
public sealed class TokenAccessor {
    public Brush GetBrush(string token) => (Brush)Application.Current.Resources[token];
    public double GetNumber(string token) => (double)Application.Current.Resources[token];
}
```

通过 DI 注入 `ITokenAccessor`，便于测试 mock。

业务代码尽量不直接读 Token，让 Token 只在 XAML 里。

### 4.4 主题切换运行时机制

```csharp
public sealed class ThemeService : IThemeService {
    public void Apply(ThemeId id) {
        var dict = LoadThemeDict(id);
        Application.Current.Resources.MergedDictionaries.RemoveWhere(IsThemeDict);
        Application.Current.Resources.MergedDictionaries.Add(dict);
        _changes.OnNext(new ThemeChangedEvent(id));
        _settings.Save("ui.theme", id);
    }
}
```

切换时所有 DynamicResource 自动响应。SkiaSharp 画布订阅 `Changes` 主动 invalidate。

### 4.5 Token 预览页（调试工具）

`Modules.Setting` 工程师页里有"Token 预览"页：

- 列所有原始 / 语义 / 组件 Token
- 实时预览颜色块、字号示意、间距条
- 切换主题对比
- 用户可以"复制 Token 名称"

帮助开发者快速选择正确 Token。

### 4.6 Token 快速校验

启动时可选启用"Token 校验器"：

- 扫描所有 ResourceDictionary
- 检查所有 DynamicResource 引用是否能解析到 Token
- 检查是否有 XAML 里写死颜色 / 字号
- 写不通过的项到日志（非阻塞）

CI 上有强制 lint：禁止 XAML 里出现裸 `#` 颜色或裸数字字号 / 间距。

### 4.7 Token 与状态流（Rx）

某些自定义可视化组件（SkiaSharp 画布）需要程序化访问 Token：

```csharp
public interface ITokenStream {
    IObservable<TokenSnapshot> Changes { get; }
    TokenSnapshot Current { get; }
}

public sealed record TokenSnapshot(
    ImmutableDictionary<string, Color> Colors,
    ImmutableDictionary<string, double> Numbers);
```

主题变更时推送新快照，画布订阅重绘。

### 4.8 Token 文档化

`docs/design-tokens/` 目录维护：

- `tokens.md`：所有 Token 名称 + 含义
- `tokens.html`：自动生成的 Token 可视化文档
- `tokens.json`：机器可读

文档与代码同步，CI 上若 Token 增减但文档未更新 → 告警。

---

## 5. 控件库混用规范

文档 1 §3.1 已敲定 Wpf.Ui + HandyControl 混用。本节给出具体边界。

### 5.1 Wpf.Ui 负责

提供基础控件的现代 Fluent 样式：

- Button / ToggleButton / SplitButton
- TextBox / PasswordBox / RichTextBox
- ComboBox / AutoSuggestBox
- CheckBox / RadioButton / ToggleSwitch
- Slider / RatingControl
- Card / Hyperlink
- NavigationView / Breadcrumb
- ContentDialog / Flyout / TeachingTip
- ProgressBar / ProgressRing
- 主窗口（FluentWindow）

样式优先 Wpf.Ui。

### 5.2 HandyControl 负责

提供工业向控件：

- PropertyGrid（参数面板必备）
- TabControl（多 Tab，关闭按钮、拖动）
- TreeView with Search
- DateTimePicker / TimePicker / Calendar
- ColorPicker
- Loading / Spinner
- Growl（弹窗式通知，比 Wpf.Ui 的 SnackBar 更适合工业告警）
- Step（流程步骤，配方编辑、向导用）
- Carousel
- TimeBar（时间轴，仿真回放用）
- ImageBrowser

### 5.3 自研控件

落在 `src/DesignSystem/Controls` 逻辑目录；未来 DesignSystem 独立项目化后，根命名空间可使用 `DispensingPlatform.DesignSystem.Controls`：

- StatusIndicator（状态灯，多色 + 文字）
- AxisPositionPanel（轴位置实时显示）
- AlarmBar（顶部 / 底部报警条）
- DispensePathPreview（小型轨迹预览，区别于 Drafting 的全功能画布）
- SyncStatusChip（同步状态指示器）
- WaveformPanel（基于 ScottPlot 的波形组件，工业封装）
- PermissionRibbon（按权限隐藏 / 灰显的命令按钮组）
- ToolbarSection（按 Token 自动间距的工具栏分段）
- IndustrialNumberBox（工业级数字输入：单位选择、范围限制、增量步进）

### 5.4 冲突处理

两个库都提供"基础控件"（Button / TextBox）时：

- 默认走 Wpf.Ui 样式
- 进入 HandyControl 复杂控件（TabControl）的内部子控件时，遵循该控件的内部样式（不强行覆盖）
- 在 PropertyGrid 等 HandyControl 容器内的 Button 仍用 Wpf.Ui 样式：通过显式 Style 引用

### 5.5 样式合并加载顺序

App.xaml。下面的 pack URI 是 DesignSystem 被创建为独立项目后的写法；当前未项目化时，可先由 Shell 合并本地资源路径，语义顺序保持一致。

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <!-- 1. Wpf.Ui 基础（最先，作为默认） -->
      <ui:ThemesDictionary Theme="Light"/>
      <ui:ControlsDictionary/>

      <!-- 2. HandyControl（可能覆盖部分基础） -->
      <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml"/>
      <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml"/>

      <!-- 3. 自研 Token（最高优先级，覆盖所有） -->
      <ResourceDictionary Source="/DispensingPlatform.DesignSystem;component/Tokens/Tokens.xaml"/>

      <!-- 4. 自研主题 -->
      <ResourceDictionary Source="/DispensingPlatform.DesignSystem;component/Themes/Light.xaml"/>

      <!-- 5. 自研控件 -->
      <ResourceDictionary Source="/DispensingPlatform.DesignSystem;component/Controls/Generic.xaml"/>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

加载顺序保证：自研 Token > 自研主题 > 第三方默认。

### 5.6 第三方控件的 Token 适配

部分 HandyControl 控件不直接支持 Token，需要"覆盖样式":

- 在 `src/DesignSystem/Controls` 写覆盖样式；项目化后命名空间为 `DispensingPlatform.DesignSystem.Controls`
- 引用我们的 Token
- 通过 `BasedOn={StaticResource ...}` 继承原样式后部分覆盖

例如 PropertyGrid 标题栏背景：

```xml
<Style TargetType="hc:PropertyGrid"
       BasedOn="{StaticResource {x:Type hc:PropertyGrid}}">
  <Setter Property="Background"
          Value="{DynamicResource Color.Surface.Panel}"/>
  <Setter Property="BorderBrush"
          Value="{DynamicResource Color.Surface.Border}"/>
</Style>
```

### 5.7 选型决策原则

新增控件需求时按这个顺序选：

1. Wpf.Ui 有 → 用 Wpf.Ui
2. HandyControl 有 → 用 HandyControl
3. 都没有但简单 → 写在 `DesignSystem.Controls`
4. 复杂业务专用 → 写在对应 Module，但样式必须用 Token

不允许：

- 新增第四个控件库
- 在 Module 里写不通过 Token 的 hardcoded 样式

## 6. 排版与栅格

### 6.1 字体族

```
FontFamily.Sans      = "Inter, Segoe UI, 思源黑体 CN, Microsoft YaHei UI, sans-serif"
FontFamily.Serif     = "Source Serif Pro, 思源宋体 CN, serif"
FontFamily.Mono      = "JetBrains Mono, Cascadia Code, Consolas, 等距更纱黑体 SC, monospace"
FontFamily.Numeric   = "JetBrains Mono, Cascadia Code, monospace"   # 数字仪表显示
```

约定：

- 中英文统一栈，西文优先 Inter（开源、易读）
- 等宽字体专用于代码 / 数字仪表（避免数字"跳动"）
- 字体 fallback 链覆盖工业 Windows IoT 默认安装
- 安装包随附 Inter / JetBrains Mono 字体文件，避免客户机缺字体

### 6.2 字号阶梯回顾

参见 §2.2 第一层 Token。语义层组合用 §2.3 Type.\* 系列。

工业产线主题字号整体 +1 阶（见 §3.4）。

### 6.3 行高

行高与字号成比例（约 1.4–1.5 倍）：

```
LineHeight.4   = 16
LineHeight.5   = 20
LineHeight.6   = 24
LineHeight.7   = 28
LineHeight.8   = 32
LineHeight.9   = 36
LineHeight.10  = 40
LineHeight.12  = 48
LineHeight.14  = 56
LineHeight.16  = 64
```

### 6.4 字距

中文字距用默认（0），英文字距按字号：

- Display / Heading：负 0.02 em（更紧凑）
- Title / Body：默认 0
- Caption / 全大写：正 0.04 em（更易读）

### 6.5 字重

```
FontWeight.Regular   = 400
FontWeight.Medium    = 500
FontWeight.SemiBold  = 600
FontWeight.Bold      = 700
```

避免使用 300（Light）和 800/900（Black）— 工业屏显示效果差。

### 6.6 栅格系统

整体走 8px 基线：

- 所有间距是 4 的倍数，优先 8 的倍数
- 关键元素对齐到 8px 网格
- 工业产线主题改为 12px 基线

### 6.7 容器栅格

主内容区使用 12 列响应式栅格：

```
Container.Maxwidth        = 1920 px
Container.PageGutter      → Spacing.PageGutter
Grid.Columns              = 12
Grid.Gap                  → Spacing.4
```

不强制 Web 风格的栅格嵌套，但表单 / 卡片群组建议对齐栅格。

### 6.8 间距使用约定

| 场景 | Token |
|------|-------|
| 页面整体边距 | `Spacing.PageGutter` |
| 卡片内 padding | `Spacing.Inset` |
| 章节之间 | `Spacing.SectionGap` |
| 标题与内容 | `Spacing.GroupGap` |
| 列表项之间 | `Spacing.ItemGap` |
| 紧凑面板 | `Spacing.InsetCompact` |
| 宽松场景 | `Spacing.InsetLoose` |

不允许在 XAML 里写 `Margin="12,16,12,16"` 这种裸数字。复杂 `Thickness` 需要预先定义组合 Token，例如 `Thickness.PageInset`、`Thickness.GroupBottom`，而不是在属性中拼接 DynamicResource。

### 6.9 排版示例

```xml
<TextBlock Text="设备总览"
           Style="{DynamicResource Type.Heading.Medium}"
           Margin="{DynamicResource Thickness.GroupBottom}"/>
<TextBlock Text="当前状态：Running"
           Style="{DynamicResource Type.Body.Medium}"/>
```

`Type.*` 是组合 Token，包含字号 / 行高 / 字重 / 字距。

---

## 7. 颜色系统

### 7.1 行动颜色（Action）

按重要性分级：

- **Primary**：主行动按钮（启动 Job、保存配方、确认）— Brand 色
- **Secondary**：次要行动（取消、关闭）— Neutral 200
- **Tertiary / Ghost**：弱化（文字按钮、图标按钮）— 透明背景
- **Danger**：破坏性（删除、急停、强制停止）— Red 500

每页**最多一个 Primary 按钮**。多个 Primary 按钮 = 用户不知道点哪个。

### 7.2 状态颜色

文档 6 §10.1 已用：

```
Idle           Neutral 400
Running        Green 500
Paused         Yellow 500
Alarm          Red 500
Maintenance    Purple 500
Offline        Neutral 500
```

跨页面统一颜色，不允许"这页 Running 是绿色，那页 Running 是蓝色"。

### 7.3 数据可视化颜色板

为图表 / 波形 / 多通道叠加准备的"分类色板"：

```
Series.1   Brand
Series.2   Cyan 500
Series.3   Orange 500
Series.4   Purple 500
Series.5   Green 600
Series.6   Yellow 600
Series.7   Red 500
Series.8   Blue 600
```

约定：

- 同一图表内尽量不超过 8 个系列
- 系列颜色对色盲友好（避免红绿对比依赖）
- 仿真轨迹用 Series.2 (Cyan)，实际轨迹用 Series.3 (Orange)，差异色对比强（详见文档 5 §8.6）

### 7.4 报警颜色（按等级）

```
Alarm.Fatal     Red 700
Alarm.Critical  Red 500
Alarm.Warning   Orange 500
Alarm.Info      Blue 500
```

报警 Bar / Toast / 弹窗都引用同一组语义 Token。

### 7.5 同步状态颜色

文档 5 §10 状态条用：

```
Sync.InSync    Green 500
Sync.Stale     Yellow 500
Sync.Detached  Orange 500
Sync.Failed    Red 500
```

### 7.6 表面层级

阴影 + 颜色共同表达层级：

| 层 | 颜色 | 阴影 |
|----|------|------|
| Background | `Surface.Background` | 无 |
| Panel | `Surface.Panel` | 无 |
| Elevated（卡片） | `Surface.Elevated` | `Shadow.Sm` |
| Modal（对话框） | `Surface.Elevated` | `Shadow.Lg` |
| Overlay（遮罩） | `Surface.Overlay` | 无 |

暗色主题尤其依赖层级颜色，避免全部用一种灰。

### 7.7 文字层级

```
Text.Primary     主要内容
Text.Secondary   次要内容（描述、辅助信息）
Text.Tertiary    弱化（占位符、时间戳）
Text.Disabled    禁用
Text.OnPrimary   主行动按钮文字（白）
Text.Link        链接
```

使用约定：

- 主标题 → Primary
- 副标题 / 描述 → Secondary
- 时间戳 / 计数 → Tertiary
- 不要给同一段文字混三种文字色

### 7.8 颜色无障碍校验

每对前景 / 背景组合**必须**满足 WCAG AA：

- 普通文本 4.5:1
- 大字（18px+ 或 14px+ 加粗）3:1
- 图标 / 控件状态 3:1

CI 上有 lint：自动校验所有 Text.\* on Surface.\* 组合。

高对比主题要求 7:1（AAA）。

### 7.9 不传达意义不只用颜色

- 状态指示同时给图标 / 文字（绿色不够，加 ✓）
- 报警等级同时给颜色 / 图标 / 文字
- 必要时加底纹 / 边框

色盲用户与黑白打印场景都能理解。

### 7.10 透明度

避免在主流程使用透明度表达层级（暗色主题下混色看不清）。仅在以下场景用：

- 遮罩（`Color.Surface.Overlay` 自带 alpha）
- 禁用状态（前景 alpha 50%）
- 鼠标悬停高亮（背景 alpha 8%）

---

## 8. 图标系统

### 8.1 图标集选型

V1 标准：

- **Lucide**（推荐）：开源、设计一致、覆盖广
- **Fluent System Icons**：Microsoft 官方，与 Wpf.Ui 一致
- 二者混用：基础走 Fluent，缺的补 Lucide

后续添加：

- 工业专属（点胶针头、胶水瓶、压力表、安全门、急停）— 自研，由设计师补充

### 8.2 图标尺寸

```
IconSize.Small    = 16
IconSize.Medium   = 20
IconSize.Large    = 24
IconSize.XLarge   = 32
IconSize.Display  = 48
```

按上下文选：按钮内 16/20，工具栏 24，页面标题 32。

### 8.3 图标颜色

引用 Token：

- 默认：`Text.Primary`
- 弱化：`Text.Secondary`
- 主行动：`Color.Action.Primary`
- 危险：`Color.Action.Danger`
- 状态：`Color.Status.*`

不允许在 XAML 里给图标硬编码颜色。

### 8.4 图标资源结构

```
src/DesignSystem/Controls/
└─ Icons/
    ├─ Generated/                   # 从 Lucide / Fluent 自动生成的 Path Geometry
    │   ├─ Lucide.xaml
    │   └─ Fluent.xaml
    ├─ Industrial/                  # 自研
    │   ├─ Needle.xaml
    │   ├─ Glue.xaml
    │   ├─ Purge.xaml
    │   └─ ...
    └─ Icon.cs                       # 通用 IconControl
```

`<dp:Icon Glyph="Play" Size="Medium"/>` 通过 Glyph 名称引用。

### 8.5 自研工业图标规范

- 24x24 grid 设计
- 2px 描边（与 Lucide 一致）
- 圆角 0.5–1px
- 不带颜色，由 Foreground 控制
- 关键工业概念：针头、胶滴、压力、温度、加热、Mark、禁打区、抬笔、清针、视觉、相机、PSO、Barrier

### 8.6 图标使用反模式

- ❌ 同一概念多个图标
- ❌ 图标比邻位置无对齐
- ❌ 图标尺寸 17 / 22 / 26 这种不在阶梯上的值
- ❌ 图标颜色硬编码

---

## 9. 双 UI 切换

### 9.1 一 ViewModel + 多 View

每个模块的 ViewModel 共享，View 按目标受众分两套：

```
Modules.Manual/
├─ ViewModels/
│   └─ ManualViewModel.cs               # 共享
├─ Views/
│   ├─ Engineer/
│   │   └─ ManualView.xaml              # 工程师视图
│   └─ Operator/
│       └─ ManualView.xaml              # 操作员视图
└─ Module.cs
```

### 9.2 视图选择机制

注册 View 时按 Audience 标记：

```csharp
public sealed class ManualModule : IModule {
    public void RegisterTypes(IContainerRegistry r) {
        r.RegisterForNavigation<EngineerViews.ManualView, ManualViewModel>("manual:engineer");
        r.RegisterForNavigation<OperatorViews.ManualView, ManualViewModel>("manual:operator");
    }
}
```

导航时按当前 Audience 选键：

```csharp
public void Navigate(string moduleKey) {
    var audience = _permission.CurrentUser?.Role == UserRole.Operator ? "operator" : "engineer";
    _region.RequestNavigate("MainRegion", $"{moduleKey}:{audience}");
}
```

### 9.3 工程师 View 设计原则

- 信息密集型，参数全开放
- 多列布局，每列内信息分组
- 使用 PropertyGrid（HandyControl）展示数据
- 命令行 / 控制台默认显示
- 高级开关默认显示
- 字号默认（不放大）

### 9.4 操作员 View 设计原则

- 流程化（顶部步骤指示）
- 主操作按钮大（高度 ≥ 56 px，宽度 ≥ 160 px）
- 关键状态色块大（占视觉中心）
- 装饰最少
- 字号 +1 阶（更易读）
- 危险操作必须二次确认

### 9.5 双 View 切换的 ViewModel 设计

ViewModel 提供同时满足两类 View 的能力：

- 暴露丰富属性供工程师 View 绑定
- 暴露简化属性 / 命令供操作员 View 绑定
- ViewModel 不感知当前是哪种 View

例如同一 ViewModel 既有 `JogStepMm`（工程师可改步进）又有 `IsMoving`（操作员看状态指示）。

### 9.6 切换时的状态保持

切换权限（如工程师切到操作员）时：

- ViewModel 不重建（保留状态）
- 仅切换 View
- 切换前提示工程师"正在编辑的内容如何处理"
- 切换后操作员看不到工程师未保存的临时数据

### 9.7 公共组件

某些组件两套 View 都用：

- 报警栏（同一组件）
- 状态栏（同一组件）
- 主壳（同一窗口）

只有内容区按 Audience 切换。

### 9.8 测试策略

- 每个模块两套 View 独立 UI 测试
- 共享 ViewModel 单元测试
- View 切换的状态保持测试

---

## 10. 主壳布局

### 10.1 整体骨架

```
┌─────────────────────────────────────────────────────────┐
│ TopBar  状态 / 报警计数 / 节拍 / 用户 / 主题 / 语言     │
├─────────────────────────────────────────────────────────┤
│        │                                                │
│ Side   │  MainRegion（Prism Region）                    │
│ Nav    │  按当前 Module + Audience 渲染                 │
│        │                                                │
│        │                                                │
│        │                                                │
├─────────────────────────────────────────────────────────┤
│ AlarmBar  当前最高级别报警 + 数量                       │
├─────────────────────────────────────────────────────────┤
│ StatusBar  轴位置 / IO / 节拍 / 同步状态                 │
└─────────────────────────────────────────────────────────┘
```

各区域是独立 Prism Region，可注入：

- `TopBar` Region：顶栏可被高优先级模块（报警 / 维护提示）覆盖
- `SideNav` Region：导航树由配置驱动
- `MainRegion`：主导航目标
- `AlarmBar` Region：报警模块占
- `StatusBar` Region：底部条由多个 ViewModel 拼装

### 10.2 顶栏（TopBar）

固定高度 56px（操作员主题 64px）。从左到右：

| 区 | 内容 |
|----|------|
| 左 | 客户 logo（可点回首页） |
| 中-左 | 整机状态色块（大字） + 当前 Job 名称 |
| 中 | 关键 KPI（节拍 / 完成数 / 偏差 P95） |
| 中-右 | 同步状态 chip + 报警计数 chip |
| 右 | 用户头像 / 主题切换 / 语言切换 / 通知 |

紧急情况（致命报警）顶栏整条变红 + 文字提示。

### 10.3 左侧导航（SideNav）

宽度 240px（操作员主题 280px）。可折叠到 64px（仅图标）。

导航项按角色过滤：

- Operator 看到：Production / Alarm / Trace / Setting (受限)
- Engineer 看到：全部
- Admin 看到：全部 + 系统管理

每项形态：

```
[图标]  名称
        子状态（"3 报警 / 2 暂停"）
```

### 10.4 主内容区（MainRegion）

填满剩余空间。每个模块自己负责内部布局。

子布局规范：

- 顶部：本模块的 Toolbar（命令按钮）
- 中：主体内容
- 右侧：可选的属性 / 详情面板（可隐藏）
- 底部：上下文反馈条（如"已保存"）

### 10.5 报警栏（AlarmBar）

固定高度 40px（多条时可折叠展开）。位于 StatusBar 之上。

显示规则：

- 0 报警：隐藏（高度 0）
- 仅 Info / Warning：黄色，普通显示
- Critical：橙色，加粗
- Fatal：红色，闪烁（缓慢，不刺眼），全屏边框红光

点击展开历史列表（`Modules.Alarm`）。

### 10.6 状态栏（StatusBar）

底部 28px。多个区域：

| 区 | 内容 |
|----|------|
| 左 | 主轴位置（X/Y/Z 实时坐标） |
| 中-左 | 关键 IO 状态指示（急停 / 安全门 / 光栅 / 气压） |
| 中 | 当前节拍 / FPS（调试期） |
| 中-右 | 通讯状态（PLC 心跳 / 相机连接） |
| 右 | 当前时间 / 用户 / 主题 |

每个区域是独立 ViewModel，可被插件扩展。

### 10.7 浮层（Overlay）

层级（z-index）：

```
ZIndex.Base      = 0       # 普通内容
ZIndex.Tooltip   = 100     # 悬浮提示
ZIndex.Popup     = 200     # 下拉、菜单
ZIndex.Dialog    = 300     # 对话框
ZIndex.Toast     = 400     # 通知 / Growl
ZIndex.Top       = 999     # 最高（致命报警弹窗）
```

报警弹窗 / 维护模式横条等用 `Top`，不被普通对话框覆盖。

### 10.8 响应式

最小分辨率 1366×768，目标分辨率 1920×1080，支持 4K：

- 1366×768：左侧 Nav 默认折叠到图标
- 1920×1080：默认展开
- 4K：字号 +1 阶（自动高 DPI 缩放）

不支持纵屏 / 平板尺寸（V1 不考虑）。

### 10.9 全屏模式

工程师可临时全屏某个模块（如 Drafting 画布）：

- 隐藏 SideNav
- TopBar 缩到 32px
- 报警栏 / 状态栏保留
- 按 Esc 退出

操作员模式不允许全屏（必须看到状态条）。

### 10.10 多窗口（V2 预留）

V1 单窗口。V2 支持把某个模块"拆出来"到独立窗口（多屏调试场景）。

## 11. 关键页面布局规范

每个 Module 主页面的"骨架模板"。具体细节由各 Module 的 README 决定，但骨架须遵循。

### 11.1 手动调试页（Modules.Manual）

**工程师 View**：

```
┌──────────────────────────────────────────────────────────┐
│ Toolbar  [回零] [使能] [急停] [仿真模式] [脚本控制台]    │
├──────────────────┬──────────────────┬───────────────────┤
│ 轴控制面板       │  IO 监视面板      │  当前位置 3D 预览 │
│  X / Y / Z / R   │  DI / DO / AI     │  HelixToolkit     │
│  Jog 步进        │  强制控制（维护） │  实时光标         │
│  绝对 / 相对     │                   │                   │
├──────────────────┴──────────────────┴───────────────────┤
│ 命令日志（实时）                                         │
└──────────────────────────────────────────────────────────┘
```

**操作员 View**：

仅显示极简的"安全 Jog"功能，且大部分操作受限。多数情况操作员页直接隐藏整个 Manual 模块。

### 11.2 配方 / 绘图页（Modules.Drafting）

**工程师 View**（这是重点页）：

```
┌──────────────────────────────────────────────────────────┐
│ Toolbar  文件 编辑 视图 工艺 仿真 | 编译 仿真 G代码 下发 │
├─────────┬──────────────────────────────┬────────────────┤
│ 工具    │                              │ 右侧面板（可折）│
│ 面板    │      画布主区                │  ▣ 图层        │
│ (绘制   │      (SkiaSharp)             │  ▣ 属性        │
│  修改   │                              │  ▣ 工艺        │
│  工艺)  │                              │  ▣ G 代码      │
│         │                              │  ▣ 仿真        │
├─────────┴──────────────────────────────┴────────────────┤
│ 命令行 + 命令历史                       同步状态条       │
└──────────────────────────────────────────────────────────┘
```

详细见文档 4。同步状态条见文档 5 §10。

**操作员 View**：

操作员通常不进绘图页。仅在权限允许时给"配方查看"只读视图（无工具栏，仅画布 + 工艺信息列表）。

### 11.3 视觉调试页（Modules.Vision）

```
┌──────────────────────────────────────────────────────────┐
│ Toolbar  [拍一张] [连续] [保存] [选择算法] [标定]        │
├───────────────────────────┬─────────────────────────────┤
│                           │ 算法配置 PropertyGrid         │
│   主图像区 + ROI           │  - 模板路径                   │
│   (实时预览)               │  - 阈值                       │
│                           │  - 搜索区域                   │
│                           │  - 置信度阈值                 │
│                           │ ───                          │
│                           │ 结果面板                      │
│                           │  - 置信度                     │
│                           │  - 偏差 dx / dy / θ          │
│                           │  - 运行时间                   │
├───────────────────────────┴─────────────────────────────┤
│ 历史结果时间轴 / 缩略图条                                 │
└──────────────────────────────────────────────────────────┘
```

### 11.4 报警页（Modules.Alarm）

```
┌──────────────────────────────────────────────────────────┐
│ Toolbar  [全部确认] [筛选] [导出] [按级别 ▼] [按时间 ▼]  │
├──────────────────────────────────────────────────────────┤
│ 当前活跃报警表（颜色按级别）                              │
│  级别 | 编号 | 时间 | 描述 | 来源 | 计数 | 操作          │
│  ●●●  ALM-MOTION-0023  ...                                │
│  ●●   ALM-PROCESS-0105 ...                                │
├──────────────────────────────────────────────────────────┤
│ 历史报警搜索（折叠区）                                    │
└──────────────────────────────────────────────────────────┘
```

操作员 View 简化：只显示活跃报警 + "确认"大按钮，历史折叠。

### 11.5 追溯查询页（Modules.Trace）

```
┌──────────────────────────────────────────────────────────┐
│ Toolbar  [Job] [产品] [配方] [时间] [偏差] [导出]        │
├──────────────────────────────┬──────────────────────────┤
│ 过滤 + 结果列表               │ 详情区                    │
│  Job-7c2f  PCB_v3 完成 ...    │  ▣ 元数据                 │
│  Product#1234  Pass ...       │  ▣ 设计 vs 实际叠加        │
│                               │  ▣ 偏差报告                │
│                               │  ▣ 报警时序                │
│                               │  ▣ 高频波形                │
└──────────────────────────────┴──────────────────────────┘
```

详情区是 Tab，按需加载（避免一次加载几百 MB）。

### 11.6 标定页（Modules.Calibration）

按 Step 引导式（HandyControl Step 控件）：

```
[相机标定] → [手眼标定] → [Mark 标定] → [验证]
```

每步独立子页，参数填写 + 实时预览 + 自动校验。完成后落库（recipe.db 标定表）+ 审计。

### 11.7 设置页（Modules.Setting）

折叠目录：

```
通用
├─ 主题
├─ 语言
├─ 时区
├─ 单位偏好
权限
├─ 用户管理
├─ 角色管理
网络
├─ NTP 服务器
├─ 客户内网代理
存储
├─ 数据目录
├─ 备份策略
├─ 归档
开发者（仅工程师）
├─ Token 预览
├─ 慢查询日志
├─ 仿真器控制
关于
├─ 版本 / 协议版本 / 客户配置 hash
└─ 第三方许可证
```

### 11.8 维护页（Modules.Maintenance）

只在维护模式下可见：

```
┌──────────────────────────────────────────────────────────┐
│ ⚠ 维护模式 / 已持续 0:23:14 / 工程师：engineer-01        │
├──────────────────────────────────────────────────────────┤
│ 工具集（卡片网格）                                        │
│  ▣ 轴诊断    ▣ IO 强制   ▣ PLC 监视   ▣ 通讯诊断        │
│  ▣ 报警注入  ▣ 实时日志  ▣ Trace 面板  ▣ 回零脚本        │
└──────────────────────────────────────────────────────────┘
```

页面边框红色，所有放宽限位的操作要二次确认。

### 11.9 生产页（Modules.Production）

操作员主用：

```
┌──────────────────────────────────────────────────────────┐
│  当前 Job:  PCB_v3   45 / 1000      节拍 35.2s   合格 98% │
├──────────────────────────────────────────────────────────┤
│  状态色块（大）                                           │
│      ▶ Running                                            │
├──────────────────────────────────────────────────────────┤
│  [开始]   [暂停]   [停止]    （按钮 ≥ 56px）              │
├──────────────────────────────────────────────────────────┤
│  各工位状态卡片 + 当前产品 ID                             │
└──────────────────────────────────────────────────────────┘
```

工程师 View 信息更密：增加节拍时间线、瓶颈段、关键参数实时图。

---

## 12. 国际化（i18n）

### 12.1 资源字典组织

```
src/Core/
└─ I18n/
    ├─ Resources/
    │   ├─ zh-CN.resx
    │   ├─ en-US.resx
    │   ├─ ja-JP.resx
    │   ├─ ko-KR.resx
    │   └─ de-DE.resx
    └─ I18nManager.cs

每个 Module 也带自己的资源：
src/Modules/Drafting/
└─ Resources/
    └─ Strings.{locale}.resx
```

启动时合并为一个全局 i18n 字典，按 key 查找。

### 12.2 资源 key 命名

层级化命名：

```
Common.Yes
Common.No
Common.Confirm
Common.Cancel
Common.Save

Alarms.MotionFollowingError
Alarms.GlueOutOfStock

Drafting.Toolbar.Line
Drafting.Toolbar.Arc
Drafting.Command.Pline.Prompt.FirstPoint

Module.Production.Title
Module.Production.Status.Running
```

### 12.3 i18n key 引用

XAML：

```xml
<Button Content="{i18n:Translate Key=Common.Save}"/>
```

代码：

```csharp
var msg = _i18n.Translate("Alarms.MotionFollowingError", axisId);
```

### 12.4 必备语言（V1）

| Locale | 状态 |
|--------|------|
| zh-CN | 必备（默认） |
| en-US | 必备 |
| ja-JP | 视客户加 |
| ko-KR | 视客户加 |
| de-DE | 视客户加 |

CI 上检查所有 zh-CN 的 key 在 en-US 都有，反之亦然。缺失 key 报警。

### 12.5 中英文 / 多语言混排

- 中文使用全角标点
- 英文使用半角标点
- 数字 + 单位之间留空格（"32 mm" 而不是 "32mm"）
- 中英混合不强制留空格（按 Unicode CJK 习惯）

### 12.6 日期 / 数字 / 单位

按 Locale + 用户偏好：

- 日期：zh-CN `2026-05-20`，en-US `2026-05-20`（ISO 优先）
- 数字千位分隔：zh-CN / en-US 都用 `,`，de-DE 用 `.`
- 小数点：zh-CN / en-US 用 `.`，de-DE 用 `,`
- 长度单位：默认 mm，可切 inch / um
- 时间间隔：自适应

通过 `IFormatService` 统一封装：

```csharp
public interface IFormatService {
    string FormatLength(Length value);
    string FormatDate(DateTimeOffset value);
    string FormatNumber(double value, int decimals = 2);
    string FormatDuration(TimeSpan value);
    string FormatBytes(long bytes);
}
```

### 12.7 文本扩展（中德文长度差）

德文一般比中文长 30%，UI 设计时按此预留空间：

- 按钮宽度按"最长翻译 + 16px padding"计算
- 必要时换行
- 超过两行显示 "..." + tooltip 全文

### 12.8 RTL（右到左）

V1 不支持。仅在文档中预留 isRtl 标志，将来扩展。

### 12.9 报警文本特殊处理

报警 key 与代码绑定：

```
Alarms.<Code>.Title
Alarms.<Code>.Description
Alarms.<Code>.SuggestedAction
```

每个语言一份完整的报警文案库，由产品 / 工程协同维护，存于 `configs/_shared/i18n/alarms/<locale>.json`。

### 12.10 翻译流程

- 主语言（zh-CN）由开发者直接维护
- 其他语言由翻译团队 / 客户翻译，导出 .resx → 翻译 → 导入
- 翻译过程审计（谁改的什么 key）

---

## 13. 动效与反馈

### 13.1 动画时长

约 200ms 是 UI 动画的"自然感"基线：

| 场景 | Duration |
|------|----------|
| 状态颜色过渡 | Fast (100ms) |
| Hover / Press | Fast |
| 弹窗打开 / 关闭 | Normal (200ms) |
| 抽屉滑入 / 滑出 | Normal |
| 大对象（页面）切换 | Slow (320ms) |
| Toast 进入 | Normal |
| 报警提示显示 | Normal |

不允许 500ms+ 的"花哨"动画。

### 13.2 缓动函数

| 场景 | Easing |
|------|--------|
| 普通过渡 | Standard |
| 入场（淡入、滑入） | Decelerate |
| 出场（淡出、滑出） | Accelerate |
| 强调变化 | Emphasized |

### 13.3 加载状态

| 时间 | 行为 |
|------|------|
| < 100ms | 不显示加载 |
| 100ms – 1s | 显示 spinner / progress |
| > 1s | 显示进度 + 描述 |
| > 10s | 显示进度 + 描述 + 取消按钮 |
| > 60s | 提示用户"操作较慢，是否继续等待？" |

加载组件 token：`Loading.Spinner.*` / `Loading.Skeleton.*`。

### 13.4 错误反馈

错误显示按严重性：

- **轻微**：内联提示（红字 + 图标）
- **中**：Toast / Growl
- **严重**：Dialog 弹窗，需要确认
- **致命**：全屏覆盖（无法绕过），仅"关闭程序" / "进入维护"

错误信息必须包含：

- 描述（人话）
- 错误码（用于支持工单）
- 建议操作
- "复制详情" 按钮（生成可粘贴的诊断文本）

### 13.5 操作确认

需要二次确认的场景：

- 删除（任何资源）
- 强制停止 Job
- 清除报警
- 切换权限（操作员 ↔ 工程师）
- 进入 / 退出维护模式
- 还原备份
- 重置标定

确认对话框结构：

```
┌────────────────────────────────────┐
│  ⚠  确认删除配方？                  │
│                                    │
│  此操作不可恢复，配方"PCB_v3"将被   │
│  归档，相关 Job 历史保留。          │
│                                    │
│  [取消]            [确认删除]      │
└────────────────────────────────────┘
```

危险操作的确认按钮用 Danger 颜色 + 默认聚焦在"取消"。

### 13.6 成功反馈

- 一般操作（保存、确认）：Toast 短暂提示（"已保存"）
- 重要操作（发布配方）：Toast + 状态栏更新
- 不要弹窗确认成功（操作员烦）

### 13.7 进度反馈

长时间操作（编译、导出、备份）：

- 总进度条
- 阶段描述（"正在编译路径..."）
- 已用时间 / 预计剩余
- 取消按钮（如可中断）

进度组件 token：`Progress.*`。

### 13.8 状态变化

UI 上状态变化必须有明显视觉过渡：

- 颜色淡入淡出（不要突变，眼睛跟不上）
- 数字翻牌动效（位置、计数）
- 状态指示灯渐变

但**关键报警的闪烁要克制**：缓慢闪烁（1Hz），不刺眼。

### 13.9 触感反馈（无）

工业上位机一般不接触摸屏，但部分客户用工业触摸屏：

- 点击放大反馈（Press 状态明显）
- 不依赖 hover（触摸没有 hover）
- 双击间隔放宽

### 13.10 声效（V2）

- 报警声：Fatal 短促刺耳，Critical 中等，Warning 弱
- 完成声：轻提示音
- 由 ISound 服务统一管理，可静音
- V1 仅靠 OS 蜂鸣 / 三色灯，不集成

---

## 14. 无障碍

### 14.1 V1 范围

V1 强制实现：

- 键盘可达
- 焦点指示
- 颜色对比度 WCAG AA
- 关键状态多感官（颜色 + 图标 + 文字）
- 字体可缩放（通过主题切换或 OS DPI）

V2 扩展：

- 屏幕阅读器（NVDA / Narrator）
- 高对比模式 7:1
- 完整键盘导航（无需鼠标完成所有操作）

### 14.2 键盘可达

所有可点击元素必须能用键盘到达：

- Tab 顺序合理（按视觉顺序）
- 用 Tab / Shift+Tab 在控件之间切换
- 空格 / Enter 触发按钮
- Esc 关闭对话框 / 取消命令
- 方向键在列表 / 树之间移动

工程师常用快捷键：

```
F1            帮助
F3            吸附开关（Drafting）
F8            正交（Drafting）
F12           动态输入（Drafting）

Ctrl+S        保存
Ctrl+Z / Y    Undo / Redo
Ctrl+N        新建
Ctrl+O        打开
Ctrl+P        打印 / 导出

Alt+1..9      切换主导航模块
Alt+Tab       切换文档
F11           全屏

Ctrl+,        设置
Ctrl+Shift+P  命令面板（Drafting 命令行）
```

操作员用的快捷键克制（避免误触）。

### 14.3 焦点指示

焦点环必须明显：

- 2px 实线，颜色 `Color.Action.Primary`
- 与边框间距 2px
- 高对比模式 3px

不允许用 `FocusVisualStyle="{x:Null}"` 隐藏焦点。

### 14.4 颜色对比度

XAML linter 校验所有 Text.\* on Surface.\* 的对比度：

- 普通文本 ≥ 4.5:1
- 大字 ≥ 3:1
- 图标 / 按钮边框 ≥ 3:1
- 高对比主题 ≥ 7:1

CI 强制。

### 14.5 多感官提示

任何状态变化不只用颜色：

- 图标变化（√ / ! / × ）
- 文字变化（"已就绪" / "运行中"）
- 位置变化（进度条）

色盲 / 黑白打印场景仍可读。

### 14.6 字体缩放

OS 级 DPI 缩放（100% / 125% / 150% / 200%）必须正确响应：

- 不写死 `Width` / `Height`，用 `MinWidth` / `MinHeight`
- 文本容器自适应高度
- 图标用 SVG（vector），不用 png

### 14.7 工业现场无障碍

工业现场特有：

- 戴手套触摸：触摸目标 ≥ 12mm（大约 48 px @ 100%DPI）
- 远距离观看：状态信息字号 ≥ 24
- 强光下看屏：高对比主题
- 工人轮班 / 视力差异：字号可放大（设置中"操作员字号"选项）

工业产线主题（§3.4）已经覆盖这些。

### 14.8 屏幕阅读器（V2）

V2 启用时：

- 所有控件正确设置 `AutomationProperties.Name`
- 状态变化通过 `LiveSetting=Polite` 通报
- 报警通过 `LiveSetting=Assertive` 通报
- 自定义控件实现 `AutomationPeer`

### 14.9 错误信息易懂

不允许显示 .NET 异常堆栈给操作员：

- 用人话（"无法连接到运动控制器，请检查网络"）
- 附带建议（"请检查网线 / 检查 PLC 是否上电"）
- 附带错误码（用于工单）
- "查看技术详情"折叠面板（工程师查看堆栈）

### 14.10 符合性测试

- 颜色对比度自动 lint
- 键盘可达性手动 + 自动化（每个页面跑一遍纯键盘交互）
- 高 DPI 截图回归测试

---

## 附录 A — 关键接口速查

以下名称是逻辑命名空间布局，不代表当前阶段必须存在同名 `.csproj`。

```
DispensingPlatform.Core.Contracts.Theme
└─ IThemeService
     ThemeId / ThemeDescriptor / ThemeChangedEvent

DispensingPlatform.Core.Contracts.I18n
├─ II18nManager
└─ IFormatService

DispensingPlatform.DesignSystem.Tokens
├─ ITokenAccessor
└─ ITokenStream

DispensingPlatform.DesignSystem.Themes
├─ ThemeDescriptor 注册
└─ Light / Dark / HighContrast / Industrial 资源
```

---

## 附录 B — Token 命名速查

```
颜色：    Color.<Hue>.<Shade>
         Color.Action.* / Color.Status.* / Color.Alarm.* / Color.Sync.* / Color.Surface.* / Color.Text.*
         Brand.Color.* （客户品牌覆盖）
间距：    Spacing.<Step> / Spacing.<Semantic>
字号：    FontSize.<Step>
行高：    LineHeight.<Step>
排版组合：Type.<Category>.<Size>
圆角：    Radius.<Name>
阴影：    Shadow.<Level>
动效：    Duration.<Step> / Easing.<Name>
图标尺寸：IconSize.<Name>
zindex：  ZIndex.<Name>
组件：    <Control>.<Variant>.<Property>.<State>
         例：Button.Primary.Background.Hover
```

---

## 附录 C — 不变量清单

| 不变量 | 说明 |
|--------|------|
| 同分类同状态色全局唯一 | Running 永远是 Green 500（基于品牌色映射） |
| 主行动按钮每页 ≤ 1 | 多个会让用户不知道点哪个 |
| 报警颜色与等级强绑定 | Fatal 永远 Red 700 |
| 同步状态颜色与状态强绑定 | InSync 永远 Green 500 |
| 工业产线主题字号 ≥ 默认 +1 阶 | 强光环境可读 |
| Token 命名层级化 | 不允许扁平命名 |
| 动画时长 ≤ Slow (320ms) | 不允许花哨动画 |
| 危险按钮默认聚焦在"取消" | 误回车不删除 |
| 操作员关键按钮 ≥ 56px | 戴手套点击 |
| 颜色不传达独占信息 | 必须搭配图标 / 文字 |

---

## 附录 D — 与其他文档的关系

- 文档 1 Architecture：技术栈选型（WPF / Prism / Wpf.Ui / HandyControl）
- 文档 2 Solution-Structure：DesignSystem 目录占位和未来拆分候选
- 文档 3 Core-Contracts：IThemeService / IFormatService 接口的实现规约
- 文档 4 Drafting-Subsystem：编辑器 UI 内布局、工具面板、命令行
- 文档 5 Sync-Mechanism：同步状态条 / G 代码视图配色
- 文档 6 StateMachine-Design：状态条 / 报警栏 / 维护模式视觉
- 文档 7 Data-Persistence：备份还原 UI、Token 预览页
- 文档 9 Config-Multitenancy：客户品牌覆盖、UI 布局可定制项
- 文档 10 DevOps：Token / 主题的 lint 与 CI 强制
