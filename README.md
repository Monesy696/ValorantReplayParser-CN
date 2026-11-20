# ValorantReplayParser CN

国服 VALORANT `.vrf` 回放解析器的实验性适配，基于 [michel-giehl/ValorantReplayParser](https://github.com/michel-giehl/ValorantReplayParser)。上游固定提交为 `157f376b26bedc58e996b8bd9d3ea50adeac5aaa`。

本项目增加 `++Ares-Core+release-china-13.10` 属性块转换，以及 13.10 战报、伤害和临时阵亡参数布局。转换直接加入默认注册表。国服 13.09 与上游已有分支的实现继续保留。

## 构建和使用

安装 .NET 10 SDK，然后在仓库根目录运行。具体 SDK 版本见 `global.json`。

```powershell
dotnet restore ValorantReplayParser.sln
dotnet build ValorantReplayParser.sln -c Release --no-restore
dotnet test ValorantReplayParser.sln -c Release --no-build --no-restore
dotnet run --project src/CliReader -c Release --no-build -- --help
dotnet run --project src/CliReader -c Release --no-build -- log "C:/replays/match.vrf"
dotnet run --project src/CliReader -c Release --no-build -- export "C:/replays/match.vrf" --output "C:/exports/match"
```

`export` 输出上游版本化 JSON/NDJSON 数据，包含元数据、原生事件、移动及诊断。请按 CLI `--help` 和导出清单消费数据。库调用示例与解析状态约定见 [上游 API 说明](UPSTREAM_README.md)。

## 支持范围

国服 13.10 转换已在本地授权的三份极速模式回放中完整遍历。定向探针选择星礈和钢锁相关类型，9445 条目标属性更新中 9280 条完整解码，236 条目标 RPC 中 165 条完整解码。这些数字是指定类型的检查结果。实验性 API 和技能类型化覆盖仍在完善。

测试包含国服转换的合成固定回归向量、错误输入和地区/版本精确注册检查。合成向量使用公开字节输入，根据恢复的转换公式独立计算，未标为客户端原生输出。原始私人回放和完整解码不会随仓库分发。

## 来源与许可

保留上游 MIT 许可及版权。国服 13.10 适配修改由 VECTOR 项目贡献，按 MIT 许可提供。上游其他转换的来源见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。本仓库尚无独立 NuGet 发布，构建命令使用本地项目。

项目为社区开发，与 Riot Games 无隶属或背书关系。VALORANT 和相关商标属于其各自权利人。

## 验证记录

完整独立构建、测试和真实原件 CLI 导出结果见 [VALIDATION.md](VALIDATION.md)。上游包名保留用于源码与包测试兼容，当前适配版本以源码构建使用。
