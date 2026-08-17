# Engineer-Cost（施工定额）

工程计价 / 施工定额桌面工具（个人学习项目）。

> 代码仍在持续打磨中，欢迎提 Issue / PR。

## 功能概览

- 清单 / 定额 / 消耗量三层数据维护
- 人材机市场价全局同价、含量与工程量联动重算
- 管理费 / 利润 / 规费 / 增值税按 `appsettings.json` 取费
- 导入系统定额库条目到用户工程
- 河南省 YDJC（DBJ 41/T087-2024）成果导出
- 程序在线检查更新（工具栏手动触发）；定额库 systemDB 随程序包分发

## 技术栈

- .NET 8 Windows Forms
- SQLite + Dapper
- Microsoft.Extensions.Configuration

## 数据目录

| 数据 | 位置 |
|------|------|
| 用户库 `userDB.db` | `%AppData%\施工定额\`（首次运行从程序目录复制） |
| 系统定额库 `systemDB.db` | 程序目录（随更新包分发） |
| 运行日志 | `%AppData%\施工定额\logs\` |

## 配置

`appsettings.json`：

- `ConnectionStrings`：用户库 / 系统库
- `FeeSettings`：管理费基数与费率、利润率、规费、增值税等
- `UpdateSettings`：程序版本检查 URL

## 开发

```bash
dotnet build 施工定额.sln
dotnet run --project 施工定额
```

## 架构说明（简要）

- **Form**：只处理 UI 事件与绑定
- **Presenter**：业务编排（改价 / 改量 / 重算 / 保存）
- **Service**：计算引擎、导入、导出、更新
- **Repository**：SQLite 持久化（接口抽象便于测试）
- **Entity**：清单 / 定额 / 消耗量 / 费用构成

## License

个人学习用途。
