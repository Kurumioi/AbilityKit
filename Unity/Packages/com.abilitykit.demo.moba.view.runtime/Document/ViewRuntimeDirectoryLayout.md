# View Runtime Directory Layout

本文记录 `com.abilitykit.demo.moba.view.runtime` 当前阶段的目录职责边界。

本阶段只做包内目录重组，不新增 package / asmdef，也不调整 namespace。目标是先让代码的物理位置表达职责，后续再根据稳定程度决定是否拆成独立包或程序集。

## Top-level

- `Runtime/Game/App`：Unity 入口、根流程、启动配置等应用级编排。
- `Runtime/Game/Battle`：对局内客户端、表现、输入、实体视图模型以及共享上下文。
- `Runtime/Game/UI`：通用 UI 基础设施。
- `Runtime/Game/EntityCreation`：通用实体创建辅助。
- `Runtime/Game/EntityDebug`：实体调试与可视化。
- `Runtime/Game/Test`：运行期测试和调试入口。

## Battle

- `Battle/Bootstrap`：BattlePhase、启动配置、测试 bootstrapper、MOBA 配置加载等对局装配入口。
- `Battle/Client`：对局客户端侧运行时，包含 session、transport、gateway、replay、prediction、snapshot routing、world start。
- `Battle/Presentation`：表现层绑定、HUD、VFX、floating text、view events、view sub-features。
- `Battle/Input`：输入 feature 与输入处理边界，包含 sources、mapping、submission 三类职责。
- `Battle/EntityViewModel`：表现层实体、组件和实体 feature。
- `Battle/Shared`：BattleContext runtime/snapshot/debug/entity/input slices、HUD input state、session hooks、module host、跨子域共享的轻量 domain 对象。
- `Battle/Debug`：对局调试 facade、OnGUI debug feature 等。
- `Battle/Legacy`：暂时保留的兼容/待迁移代码。新增逻辑不应继续放入该目录。

## Follow-up Order

1. 继续把 input state 的公开入口稳定成小接口，后续方便迁移到纯 C# 表现核心。
2. 为 snapshot/entity/debug slices 增加更小的访问方法，逐步减少外部直接读写字段。
3. 收敛 `Legacy`：确认 `IBattleLogicTransport` 与 requests 的归属后迁出或删除。
4. 视稳定度再评估 asmdef/package 拆分。
