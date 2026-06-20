# Moba.Runtime 逻辑层设计结论

## 结论

当前 `moba.runtime` 的设计方向是正确的：`system` 负责调度与编排，`service` 负责业务实现与规则处理，`config/dto/mo` 负责数据表达，`trigger/event` 负责统一入口与解耦。

## 建议边界

- `system`
  - 负责生命周期推进、事件消费、上下文绑定、队列分发。
  - 只保留轻量的流程控制，不承载复杂规则。
- `service`
  - 负责触发执行、状态计算、规则判断、跨模块复用逻辑。
  - 适合承接 AOE、子弹、buff、被动等正式化业务。
- `dto/mo/config`
  - 只描述数据，不做业务判断。
- `trigger/event`
  - 作为统一入口，承接溯源、统计、执行和扩展点。

## 判断标准

如果某个 `system` 开始出现大量业务分支、配置解释、效果执行细节，就应该下沉到 `service`。
如果 `system` 只是“取数据 -> 调 service -> 发事件/改状态”，通常就是合适的。

## 当前项目状态

- AOE / projectile 阶段触发已经在向统一 trigger 入口收敛。
- 配置链路已经补齐，运行时不再直接依赖散落的表结构。
- payload 已开始承担上下文和溯源信息，便于后续统一管理。

## 后续优化方向

- 继续收敛 `system` 中重复的 trigger 分发逻辑。
- 统一 AOE / projectile / buff / passive 的阶段 payload 抽象。
- 明确哪些生命周期推进留在 `system`，哪些规则与效果下沉到 `service`。
