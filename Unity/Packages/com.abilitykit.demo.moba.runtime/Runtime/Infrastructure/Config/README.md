# MOBA Runtime Config

This directory contains the runtime-side configuration layer for the MOBA sample logic world.

## Ownership

- `com.abilitykit.demo.moba.share` owns shared DTO definitions under `Runtime/Game/Config/Dto`.
- `com.abilitykit.demo.moba.runtime` owns runtime MO models, table registration, loading profiles, and logic-world access APIs.
- Platform or host packages own the actual asset source, such as Unity Resources, ET file system paths, or an external service.

## Directory Layout

```text
Config/
|-- Core/
|   |-- MobaConfigDatabase.cs
|   |-- MobaConfigLoadPipeline.cs
|   |-- IMobaConfigLoadProfile.cs
|   |-- IMobaConfigDtoProvider.cs
|   |-- IMobaConfigDtoDeserializer.cs
|   |-- IMobaConfigDtoBytesDeserializer.cs
|   |-- IMobaConfigTableRegistry.cs
|   |-- MobaConfigPaths.cs
|   |-- MobaConfigGroups.cs
|   |-- IConfigGroup.cs
|   |-- IConfigGroupProvider.cs
|   |-- MobaAttrTypes.cs
|   `-- ...
|
|-- BattleDemo/
|   |-- MobaConfigRegistry.cs
|   |-- Loaders/
|   |-- Deserializers/
|   `-- MO/
|       |-- CharacterMO.cs
|       |-- SkillMO.cs
|       |-- BuffMO.cs
|       |-- MobaCoreDtos.cs
|       `-- ...
|
`-- README.md
```

## Main Extension Points

Use one of these integration paths for a new host or project.

### Default Resources Profile

Use this when the host can provide `ITextAssetLoader` and the exported JSON files are available under the default Resources directory.

```csharp
var database = new MobaConfigDatabase(textAssetLoader: textAssetLoader);
database.LoadFromResources(MobaConfigPaths.DefaultResourcesDir);
```

### Source-Based Loading

Use this when the host can expose config files through the generic Ability config source abstraction.

```csharp
var database = new MobaConfigDatabase();
database.LoadFromSource(configSource, basePath: "moba");
```

### DTO Provider Loading

Use this when the host already owns deserialization and only wants to provide DTO arrays to the logic layer.

```csharp
var database = new MobaConfigDatabase();
database.LoadFromDtoProvider(dtoProvider);
```

The provider only needs to implement `IMobaConfigDtoProvider`. The runtime registry decides which DTO arrays are required for the current project.

## Runtime Access

Game logic should read config through `MobaConfigDatabase` or injected services that depend on it.

```csharp
var skill = database.GetSkill(skillId);
if (database.TryGetCharacter(characterId, out var character))
{
    // Use runtime MO data.
}
```

Generic table access is also available when feature code should not depend on MOBA-specific convenience methods.

```csharp
var table = database.GetTable<SkillMO>();
var skill = table.Get(skillId);
```

## Design Rules

- Keep DTOs in the share package so editor, view, server, ET, and runtime code can reuse the same contracts.
- Keep runtime MO types in this package because they model logic-world behavior and convenience access.
- Prefer `IMobaConfigLoadProfile` or `IMobaConfigLoadPipeline` for host integration instead of calling many database loading methods directly.
- Prefer `IConfigSource` for external storage systems and `IMobaConfigDtoProvider` for hosts that already deserialize config.
- Add new tables through `MobaConfigRegistry` and the share DTO folder first, then add MO conversion only when runtime logic needs richer access.

## Current Cleanup Direction

`MobaConfigDatabase` is kept backward compatible for existing callers, but new integration should treat it as a runtime facade rather than a source-specific loader. Loading policy belongs in profiles or pipelines; table access belongs in the database facade.
