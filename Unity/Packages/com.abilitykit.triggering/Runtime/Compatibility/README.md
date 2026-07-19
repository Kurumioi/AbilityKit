# Runtime compatibility catalog

This directory documents the runtime compatibility boundary. It does not contain compatibility implementation placeholders.

The authoritative machine-readable catalog is `RootRuntimeCompatibilityCatalog.cs`. The human-readable status is maintained in `Runtime/Compatibility.md`.

Any compatibility entry addition, migration, or removal must update the catalog, the human-readable document, and the 相关测试 in `RuntimeCompatibilityCatalogTests.cs` in the same change.
