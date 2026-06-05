# ET Smoke Rule

## 目的

用于自动化验证 ET 控制台战斗流程是否按正式协议跑通，并在通过后自动收口输出与临时排查痕迹。

## 运行入口

建议统一使用仓库根目录下的脚本：

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_et_battle_smoke.ps1
```

## 默认判定规则

Smoke 通过至少需要满足以下条件：

- 战斗流程已启动，且进入 battle 状态。
- runtime 已 ready，且具备 state read model 能力。
- runtime 至少产出一次实体快照和一次 runtime 快照。
- 已提交正式输入：移动输入和技能输入。
- 已解析技能目标，并能定位到目标 actor。
- 已解析正式 DTO 快照：ActorTransform 和 StateHash。
- 已解析至少一种正式事件快照：Damage / Projectile / Area。
- battle 帧数达到最小门槛，且没有提前失败。

当前默认参数：

- `--smoke-frames=600`
- `--smoke-min-battle-frames=30`
- `--smoke-timeout-ms=15000`
- `--smoke-sleep-ms=16`
- `--smoke-drain-frames=5`

## 自动退出规则

- smoke 模式默认会在通过后返回退出码 `0`。
- 为避免控制台挂住，smoke 默认会在通过后执行短暂 drain，然后强制退出进程。
- 如果需要保留进程用于人工观察，可以传入 `--smoke-no-force-exit`。
- 如果需要调整总运行时间，优先调 `--smoke-timeout-ms`，而不是单纯拉大帧数。

## 临时日志清理约定

排查过程中允许存在短期诊断日志，但合入前需要清理或降级，以下类型应视为临时输出：

- `[AI-DIAG]` 前缀日志。
- 只用于定位执行链路的 success-path 逐步日志。
- 高频逐帧采样日志。
- 只为确认某个转换器、执行器、handler 已进入的调试输出。

应保留的日志：

- 参数缺失、配置缺失、依赖缺失。
- 解析失败、执行失败、异常堆栈。
- 影响流程判断的 warning/error。

## 脚本行为

`tools/run_et_battle_smoke.ps1` 会：

- 先停止残留的 smoke `dotnet` 进程。
- 清理历史 smoke 输出文件。
- 先 build，再 run smoke。
- 收集输出到 `src/AbilityKit.Demo.ET.App/smoke-output.txt`。
- 成功后删除临时 smoke 输出，失败时保留输出便于排查。
- 最后再次兜底停止残留 smoke 进程。
