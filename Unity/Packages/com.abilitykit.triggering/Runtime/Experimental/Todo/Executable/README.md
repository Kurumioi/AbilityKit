# Triggering Experimental Executable Notes

`Runtime/Executable` 仅作为历史兼容与迁移参考，不是正式主线。

## 当前结论

- 新执行节点统一进入 `Runtime/Plan/Executables`。
- 旧 `Runtime/Executable` 相关类型只允许在兼容路径、旧数据读取或显式失败场景中出现。
- 当外部调用方不再依赖旧入口时，可继续删除剩余兼容壳与实验示例。

## 关注点

- 旧数据反序列化是否仍需要兼容。
- 旧示例是否仍被外部文档引用。
- 兼容 API 是否还能进一步改为显式失败或删除。
