# Runtime Compatibility

该目录承载触发器运行时根目录兼容入口的正式机器清单与防回流边界；当前根目录 `.cs` 兼容占位入口已清理完成，清单为空。

## 定位

- [`RootRuntimeCompatibilityCatalog.cs`](RootRuntimeCompatibilityCatalog.cs)：根目录 `.cs` 兼容入口的机器可读清单；当前无登记项，用于防止新的根目录占位入口回流。
- [`RuntimeCompatibilityCatalog`](../Validation/RuntimeCompatibilityCatalog.cs)：验证层门面，复用本目录的正式清单，避免文档清单与校验清单分叉。
- [`Runtime/Compatibility.md`](../Compatibility.md)：面向使用者的人类可读清理记录，保留已删除入口、正式替代路径和维护规则。

## 规则

1. 新功能不得在 Runtime 根目录 `.cs` 文件首发，应进入对应正式子目录。
2. 不再新增根目录 `.cs` 兼容占位入口；如确需兼容旧路径，必须同步更新 `RootRuntimeCompatibilityCatalog`、`Runtime/Compatibility.md` 与相关测试。
3. 相关测试会把未知根目录 `.cs` 文件报告为兼容清单缺失，用于阻止占位入口重新出现。
