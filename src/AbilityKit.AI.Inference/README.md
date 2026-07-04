# AbilityKit AI Inference

This package is the server-side learned-policy boundary for AbilityKit AI.

The stable runtime contract remains in `AbilityKit.AI.Abstractions`:

- `IAiEnvironment` owns reset and step simulation.
- `IAiPolicy` owns action selection.
- `AiObservationBuffer` and `AiActionBuffer` are the data exchange format.

`AbilityKit.AI.Inference` adapts a learned model executor to `IAiPolicy` without depending on a concrete model runtime package. ONNX Runtime should be added as a separate executor implementation later, not as a dependency of the AI abstractions or training environment packages.

## Current Boundary

`AiModelPolicy` is the policy adapter used by servers and headless runners.

`IAiModelExecutor` is the model runtime seam. An ONNX executor, a native inference executor, or a deterministic test executor can implement this interface without changing Shooter or Moba environments.

`AiModelPolicySpec` records observation length, action length, and tensor names. This gives us a single place to validate that a trained model matches the target environment before it drives gameplay.

## Direction

The intended production path is:

1. Train with Unity ML-Agents or another trainer.
2. Export a learned model.
3. Load the model through an `IAiModelExecutor` implementation.
4. Run it as an `IAiPolicy` in the pure C# server/runtime path.

This keeps Unity training tooling optional while preserving a shared runtime AI contract for Shooter, Moba, and future demos.
