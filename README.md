# ValorantReplayParser-CN

[![Build and test](https://github.com/Monesy696/ValorantReplayParser-CN/actions/workflows/build.yml/badge.svg)](https://github.com/Monesy696/ValorantReplayParser-CN/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)](global.json)

国服无畏契约 **13.10 VRF 通用回放解析器**，提供 C#/.NET 库和命令行工具，可将回放导出为 JSON/NDJSON。

A C# parser for VALORANT China 13.10 replay files, with a .NET API and JSON/NDJSON export CLI.

基于 [michel-giehl/ValorantReplayParser](https://github.com/michel-giehl/ValorantReplayParser)，上游基线为 `157f376b26bedc58e996b8bd9d3ea50adeac5aaa`。本仓库维护国服 13.10 转换与相应字段布局适配，保留上游署名及 MIT 许可。

## 支持范围

支持国服无畏契约 **13.10** VRF 回放读取、字段解析和 JSON/NDJSON 导出，可通过命令行或 .NET 库接入使用。

## 功能

- 读取回放元数据、版本、对象引用、属性更新及 RPC。
- 通过事件扩展层处理人物移动、射击、脚步和已接入的技能事件。
- 按类别、对象路径和字段选择解析内容。
- 导出版本化 JSON/NDJSON，同时保留解析统计和诊断。

国服 13.10 转换直接注册在默认读取器中。国服 13.09 及上游已有版本的实现继续保留，具体版本由回放文件中的分支标识匹配。

## 环境要求

- 构建需要 .NET 10 SDK，具体版本见 [global.json](global.json)。
- Windows 和 Linux 均通过 GitHub Actions 构建与测试。

当前使用源码构建。仓库尚未发布独立的 NuGet 包。

## 快速开始

```powershell
git clone https://github.com/Monesy696/ValorantReplayParser-CN.git
cd ValorantReplayParser-CN
dotnet restore ValorantReplayParser.sln
dotnet build ValorantReplayParser.sln -c Release --no-restore
```

导出一份本地回放，将示例路径替换成自己的文件路径和输出目录。

```powershell
dotnet run --project src/CliReader -c Release --no-build -- export "C:/replays/match.vrf" --output "C:/exports/match"
```

查看命令帮助或输出解析日志。

```powershell
dotnet run --project src/CliReader -c Release --no-build -- --help
dotnet run --project src/CliReader -c Release --no-build -- export --help
dotnet run --project src/CliReader -c Release --no-build -- log "C:/replays/match.vrf"
```

## 导出文件

| 文件 | 内容 |
|---|---|
| `manifest.json` | 导出 schema 版本、回放元数据、统计、诊断及字段目录 |
| `events.ndjson` | 原生对象与 RPC 事件，以及已接入的扩展事件 |
| `movement.ndjson` | 人物移动记录 |

NDJSON 每行是一条 JSON 记录，适合逐行读取。全量导出为未压缩文本，文件可能明显大于原 VRF。

当前导出使用 schema 9。对象引用采用 `{"net_guid": 123, "path": null}` 这样的结构，路径无法解析时为 `null`。消费端应检查 `manifest.json` 的版本和诊断信息。字段覆盖与验证细节见 [VALIDATION.md](VALIDATION.md)。

## 在 .NET 项目中使用

通过项目引用接入 `Replay.Valorant`。需要移动、射击等扩展事件时，一并引用 `Replay.Valorant.Events`。

下面的示例使用默认国服版本注册和事件扩展层。

```csharp
using System;
using System.IO;
using Replay.Models.Errors;
using Replay.Models.Events;
using Replay.Valorant;
using Replay.Valorant.Events;

try
{
    var events = new ValorantEventEnricher(new ConsoleEventSink());
    var reader = new ValorantReplayReader(eventSink: events);
    using var stream = File.OpenRead("path/to/match.vrf");

    var result = reader.Read(stream);
    events.Complete();

    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Packets: {result.PacketStats.PacketCount}");
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
    }
}
catch (ReplayParseException exception)
{
    Console.Error.WriteLine(exception.Message);
}

sealed class ConsoleEventSink : IReplayEventSink
{
    public void Emit(ReplayEvent replayEvent)
    {
        Console.WriteLine(
            $"{replayEvent.GetType().Name} at {replayEvent.TimeSeconds:0.000}s " +
            $"(packet {replayEvent.PacketId})");
    }
}
```

每份回放使用独立的事件扩展器，读取成功后调用 `Complete()`。每次读取使用一个位于文件开头的新流。事件在读取过程中逐步发出，`Read` 返回后再确认该次读取结果。

元数据读取、解析类别、自定义描述符以及字段是否实际解码的判断方式，见 [上游 API 参考](UPSTREAM_README.md)。

## 项目结构

| 项目 | 职责 |
|---|---|
| `Replay.Models` | 共享模型、解析结果和错误类型 |
| `Replay.Encoding` | 位流读取、版本转换与解压缩 |
| `Replay.Unreal` | 回放容器、封包和字段读取 |
| `Replay.Valorant` | VALORANT 对象、属性及 RPC 描述符 |
| `Replay.Valorant.Events` | 原生记录的事件扩展和移动等数据处理 |
| `CliReader` | 日志输出与 JSON/NDJSON 导出 |
| `tests` | 单元测试、固定回归向量和集成测试 |

国服 13.10 转换位于 `src/Replay.Encoding/PayloadEncryption/VersionedTransforms/ValorantSeededTransformChina13_10.cs`。

## 测试

```powershell
dotnet test ValorantReplayParser.sln -c Release --no-build --no-restore
```

仓库提供合成测试和上游已公开的测试片段。完整 VRF 夹具不随仓库分发，需要额外回放文件的集成测试在缺少文件时明确跳过。本地验证结果和字段覆盖说明见 [VALIDATION.md](VALIDATION.md)。

## 反馈与贡献

欢迎通过 [Issues](https://github.com/Monesy696/ValorantReplayParser-CN/issues) 报告问题，或提交修复和新版本适配的 Pull Request。

报告问题时请提供游戏地区与版本、运行系统、执行命令和错误摘要。分享日志前请去除玩家身份、令牌和私人文件路径。新版本适配应提供对应分支注册、字段布局说明及回归测试。

## 来源与许可

- 上游解析器由 Michel Giehl 维护，本仓库保留其版权和许可。
- 国服 13.10 适配由 VECTOR 项目贡献，按 MIT 许可提供。
- 其他转换与依赖的来源见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)，完整许可见 [LICENSE](LICENSE)。

本项目为社区开发，与 Riot Games 无隶属或背书关系。VALORANT 及相关商标属于其各自权利人。
