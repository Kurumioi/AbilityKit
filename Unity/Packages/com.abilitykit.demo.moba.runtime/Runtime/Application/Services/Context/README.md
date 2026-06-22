# MOBA Context Module Guidelines

## Purpose

The `Context` module is the shared runtime context infrastructure for MOBA gameplay execution. It connects strongly typed trigger payloads, execution-time context aggregation, source snapshots, origin propagation, lineage construction, and trace integration.

This module must not become a generic business data bag. New gameplay logic should prefer strongly typed payloads and only use key/value pipeline context as an integration fallback.

## Primary model priority

Use these models in the following order:

1. `MobaTriggerInvocationContextBase`
   - Recommended base for new trigger payloads.
   - A payload should expose origin, lineage, and trace through the unified `IMobaTriggerExecutionPayload` contract.

2. `MobaCombatExecutionContext`
   - Canonical execution-time model inside effect/action/condition execution.
   - Execution services should normalize payloads into this model before running action logic.

3. `MobaPersistentContextSourceSnapshot`
   - Canonical cross-frame and async-lifecycle source snapshot.
   - Buff, projectile, summon, continuous, and delayed execution flows should retain this snapshot instead of retaining live runtime objects.

4. `MobaContextSourceView`
   - Query, debug, retention, and transport view.
   - It is intentionally broad, but it should not replace `MobaCombatExecutionContext` as the main execution model.

5. `AbilityContextKeys` / `AbilityContextExtensions`
   - Pipeline data-bag compatibility layer.
   - These keys are not a replacement for strongly typed payloads.

## Origin, lineage, trace, and source semantics

- `MobaGameplayOrigin` answers where this gameplay operation comes from.
- `MobaTriggerLineageContext` answers how this operation joins the trace lineage chain.
- `MobaTriggerTraceContext` is the compact trigger trace representation.
- `MobaContextSourceView` is a resolved source view for queries, snapshots, retention, debug panels, and diagnostics.
- `MobaCombatExecutionContext` aggregates the current executable payload, lineage input, origin, execution snapshot, skill runtime handle, and frame.

## New payload rules

New trigger payloads should:

1. Inherit `MobaTriggerInvocationContextBase` when they are formal trigger execution payloads.
2. Implement `TryGetOrigin`, `TryGetLineageContext`, and `TryGetTraceContext` using existing origin or lineage data.
3. Implement `IMobaContextSourceProvider` when they can expose query/retention source information.
4. Implement `IMobaPersistentContextSourceProvider` when their source must survive async or cross-frame execution.
5. Avoid adding only primitive fields such as actor/config/context IDs without also exposing a formal origin or lineage provider.

## Legacy primitive compatibility

`MobaGameplayOrigin.FromLegacy` and builder legacy primitive APIs are compatibility bridges for old payloads that still carry only actor/config/context primitives.

New code should prefer:

- Propagating an existing `MobaGameplayOrigin`.
- Building from `MobaTriggerLineageContext`.
- Capturing `MobaPersistentContextSourceSnapshot` for async lifetimes.
- Normalizing into `MobaCombatExecutionContext` inside execution services.

## Naming conventions

Use `OwnerContextId` as the preferred name for ownership context identity.
`OwnerKey` remains as a compatibility alias on lineage-oriented structures.

Use `SourceContextId` for the direct source execution context.
Use `ParentContextId` for the immediate parent in a propagated origin chain.
Use `RootContextId` for the stable root of the causal chain.
