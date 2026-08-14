# 施工定额 · 代码优化与改进清单

> 基于对当前代码库的通读整理（学习项目，代码未刻意优化）。  
> 按优先级与类别分类，便于分阶段落地。

---

## 一、架构与可维护性（高优先级）

### 1. Form1 仍然偏重

**现状**  
事件处理、网格刷新、选择状态、导出硬编码等逻辑集中在 `Form1`。

**建议**  
- 将「选中变化 → 刷新子表」「工程量/市场价变更后的联动」完全下沉到 Presenter。  
- Form 只负责 `Invoke`、数据绑定与用户交互触发。

**涉及文件**  
`Form1.cs`、`UI/QingdanPresenter.cs`、`UI/SummaryPresenter.cs`

---

### 2. 全局单例使用过多

**现状**  
`AppCache.Instance`、`SelectionState.Instance`、`AppConfig` 静态类广泛使用。

**问题**  
单元测试困难、生命周期难控、隐式依赖。

**建议**  
- 引入简单 DI（构造注入即可）。  
- 或至少将 Cache / SelectionState 改为可注入服务。

**涉及文件**  
`Helper/AppCache.cs`、`UI/SelectionState.cs`、`Helper/AppConfig.cs`、`Program.cs`

---

### 3. 缺少接口抽象（除计算服务外）

**现状**  
`QingdanRepository`、`ImportService`、`YdjcExportService` 等直接依赖具体类。

**建议**  
为 Repository / Import / Export 补充接口，便于测试与策略替换。

**涉及文件**  
`Repository/QingdanRepository.cs`、`Service/ImportService.cs`、`Service/YdjcExportService.cs`、`Export/*`

---

### 4. 命名不一致 / 文件名错误

**现状**  
- 存在 `QingdanDetail.cs.cs`（双后缀）。  
- 中文属性与英文类名混用，可读性一般。

**建议**  
统一命名规范，修正错误文件名。

**涉及文件**  
`Entity/QingdanDetail.cs.cs` 及实体命名相关

---

## 二、性能与数据访问（中高优先级）

### 5. 市场价全局同步查找效率低

**现状**（`QingdanPresenter.OnMarketPriceChanged`）

```csharp
foreach (var qd in _qingdanList)
    foreach (var dg in qd.定额列表)
        foreach (var x in dg.消耗量列表)
            if (x.消耗量编码 == xhl.消耗量编码) ...
```

**问题**  
数据量大时明显卡顿。

**建议**  
维护 `Dictionary<string, List<Xiaohaoliang>>` 按消耗量编码索引，或在加载时建立反向索引。

**涉及文件**  
`UI/QingdanPresenter.cs`

---

### 6. 每次变更都全量 SaveTree

**现状**  
改一个含量/市场价即整棵清单树 UPDATE。

**建议**  
- 细粒度更新（只更新变化字段/行）。  
- 或增加「脏标记 + 批量保存」。

**涉及文件**  
`Repository/QingdanRepository.cs`、`UI/QingdanPresenter.cs`

---

### 7. UpdateDisplay 反复 Clear + 重新 Add

**现状**  
`BindingList` 清空再添加，触发大量 UI 刷新。

**建议**  
批量操作时关闭 `RaiseListChangedEvents`，完成后统一通知；或改用更高效绑定方式。

**涉及文件**  
`Form1.cs`（`UpdateDisplay`）

---

### 8. 数据库连接与事务

**现状**  
多处 `new SqliteConnection` + 同步操作。

**建议**  
- 考虑连接复用或轻量连接池。  
- 关键路径使用异步 API（`QueryAsync` 等）。  
- 启动加载增加进度反馈。

**涉及文件**  
`Repository/QingdanRepository.cs`、`Helper/DbHelper.cs`、`Helper/AppCache.cs`、`Service/*`

---

### 9. AppCache 全表加载

**现状**  
`SELECT * FROM 定额_市政工程` 等一次性进内存。

**问题**  
定额库变大后内存与启动时间膨胀。

**建议**  
按分类懒加载 / 分页 / 虚拟化树。

**涉及文件**  
`Helper/AppCache.cs`、`Helper/DbHelper.cs`

---

## 三、业务逻辑与计算（中优先级）

### 10. 费用构成计算边界不够清晰

**现状**  
`CostBreakdown` 挂在实体上且不持久化；税金在定额层与清单层均有计算，易混淆。

**建议**  
明确计算边界文档；或将税金仅放在项目汇总层。

**涉及文件**  
`Service/CostCalculationService.cs`、实体中的 `费用构成`

---

### 11. 费率配置不可热更新

**现状**  
`AppConfig.FeeRates` 仅启动时读取一次。

**建议**  
支持运行时修改费率并触发全量重算。

**涉及文件**  
`Helper/AppConfig.cs`、`Service/CostCalculationService.cs`

---

### 12. 清单工程量强制同步到定额工程量

**现状**

```csharp
foreach (var dg in qd.定额列表)
    dg.定额工程量 = qd.工程量;
```

**问题**  
实际工程中定额工程量常与清单工程量存在换算系数。

**建议**  
增加「换算系数」字段，避免直接赋值。

**涉及文件**  
`UI/QingdanPresenter.cs`（`OnQingdanWorkAmountChanged`）、实体模型

---

### 13. 导出硬编码路径与项目信息

**现状**

```csharp
exportService.Export(..., @"D:\导出\示例项目.YDJC");
```

**建议**  
使用 `SaveFileDialog` 选择路径，并提供项目信息输入窗体。

**涉及文件**  
`Form1.cs`（`button1_Click`）、`Service/YdjcExportService.cs`

---

## 四、UI / 交互体验

### 14. 缺少加载/保存状态反馈

**现状**  
重算、导入、导出时无进度或禁用 UI。

**建议**  
长时间操作设置 `Cursor = WaitCursor` 或进度条，并禁用相关控件。

**涉及文件**  
`Form1.cs`、`Form2.cs`、更新相关 Form

---

### 15. 异常处理过于粗暴

**现状**  
多处 `MessageBox.Show(ex.Message)`，用户直接看到技术异常。

**建议**  
区分业务异常与系统异常，统一错误提示层（如 `ErrorHandler.Show(ex)`）。

**涉及文件**  
`Form1.cs`、`Program.cs`、各 Service

---

### 16. BindingList + 手动事件解绑/重绑

**现状**  
`CellValueChanged` 反复 `-=` / `+=`，容易遗漏。

**建议**  
使用 `_isUpdating` 标志位，或采用更稳健的数据绑定模式减少事件抖动。

**涉及文件**  
`Form1.cs`

---

### 17. 图片查看依赖本地文件夹命名

**现状**  
`Path.Combine(AppContext.BaseDirectory, code)` 脆弱。

**建议**  
将图片路径存数据库，或统一资源目录管理。

**涉及文件**  
`Form1.cs`（`dataGridView1_CellContentClick`）、`UI/ImageViewerForm.cs`

---

## 五、代码质量与健壮性

### 18. Xiaohaoliang 未实现 INotifyPropertyChanged

**现状**  
与 `Qingdan` / `Dinge` 不一致；仅改属性不走 Presenter 时 UI 不刷新。

**建议**  
实现 `INotifyPropertyChanged`，或明确约定「只能通过 Presenter 修改」。

**涉及文件**  
`Entity/Xiaohaoliang.cs`

---

### 19. 空引用与防御性编程不足

**现状**  
`?.` 与直接访问混用，部分路径假设数据一定存在。

**建议**  
关键路径增加校验，统一空值策略。

**涉及文件**  
多处 UI 与 Presenter

---

### 20. 事务与并发

**现状**  
用户库为本地 SQLite，无多实例保护。

**建议**  
启动时加文件锁，或检测数据库是否已被占用。

**涉及文件**  
`Program.cs`、`Helper/DbHelper.cs`、连接初始化处

---

### 21. 版本号硬编码

**现状**  
关于框写死 `1.0.0`，与 csproj `<Version>` 不同步。

**建议**  
从程序集读取：

```csharp
Assembly.GetExecutingAssembly().GetName().Version
```

**涉及文件**  
`Form1.cs`（`toolStripButton1_Click`）、`施工定额.csproj`

---

### 22. 删除逻辑的外键依赖

**现状**  
`DeleteQingdan` 手动按顺序删表，依赖表结构知识。

**建议**  
数据库增加外键 + `ON DELETE CASCADE`，或统一由 Repository 封装。

**涉及文件**  
`Repository/QingdanRepository.cs`、数据库 schema

---

## 六、工程与发布

### 23. 数据库随程序分发方式

**现状**  
`systemDB.db` / `userDB.db` 使用 `CopyToOutputDirectory`；用户数据更适合放在 `%AppData%`。

**建议**  
首次运行将空 userDB 复制到 `%AppData%\施工定额\`，之后始终读写 AppData。

**涉及文件**  
`Program.cs`、`Helper/AppConfig.cs`、`施工定额.csproj`

---

### 24. 更新机制不完善

**现状**  
程序更新 / 定额库更新已有雏形，缺少签名校验、回滚、差量更新。

**建议**  
至少增加文件哈希校验。

**涉及文件**  
`Service/AppUpdateService.cs`、`Service/DbUpdateService.cs`

---

### 25. 缺少单元测试

**现状**  
计算引擎最适合测试，但目前无测试项目。

**建议**  
为 `CostCalculationService` 补充费率、人材机汇总、税金等用例。

**涉及文件**  
新建测试项目；`Service/CostCalculationService.cs`

---

### 26. 日志几乎没有

**现状**  
启动失败、更新失败、导入失败仅靠 MessageBox。

**建议**  
引入简单日志（Serilog / NLog 或自写文件日志），便于排查。

**涉及文件**  
`Program.cs`、各 Service、统一异常处理

---

## 七、可快速落地的小优化（低成本高收益）

| 项 | 建议 |
|----|------|
| 文件名 | 修正 `QingdanDetail.cs.cs` |
| 导出按钮 | 改为 `SaveFileDialog` + 项目信息输入窗体 |
| 关于框 | 动态读取程序版本 |
| 市场价同步 | 建编码索引，避免三重循环 |
| SaveTree | 只更新变化字段，或增加「仅保存当前定额」方法 |
| BindingList 刷新 | 批量操作时关闭 `RaiseListChangedEvents` |
| 异常提示 | 统一 `ErrorHandler.Show(ex)` |
| 用户数据库路径 | 固定到 `%AppData%\施工定额\` |

---

## 建议落地优先级

1. **先做**  
   市场价索引、细粒度保存、导出交互、用户库路径、版本号、文件名修复。

2. **然后做**  
   减少 Form1 职责、补接口 + 简单测试、费率热更新、工程量换算系数。

3. **长期**  
   DI、懒加载定额库、日志与更新签名、更完善的 UI 状态管理。

---

## 说明

- 本清单仅作改进路线图，不包含具体代码改动。  
- 可按条目拆分为多个 Issue / PR 逐步实施。  
- 若需针对某一项直接出补丁级代码，可在本 PR 或后续 Issue 中指定条目。
