# AbilityKit AI ML-Agents Bridge

This package is intentionally optional. AbilityKit AI runtime contracts stay in `com.abilitykit.ai.abstractions` and do not depend on Unity or ML-Agents. The bridge only adapts an `IAiEnvironment` to Unity ML-Agents when a Unity training workflow is needed.

## Direction

The stable contract is:

- Server/headless training and runtime inference use `IAiEnvironment`, `IAiPolicy`, `AiObservationBuffer`, and `AiActionBuffer`.
- Unity ML-Agents is a training frontend, not the core AI runtime API.
- Learned runtime models should later plug back into `IAiPolicy`, so Shooter and Moba can share the same server-side inference path.

This avoids coupling gameplay simulation to ML-Agents and keeps the server C# runtime independent from Unity packages.

## Enable ML-Agents

1. Install Unity ML-Agents in the Unity project.
2. Add the scripting define symbol `ABILITYKIT_ML_AGENTS`.
3. Import the `Shooter ML-Agents Agent Skeleton` sample from this package.
4. Add the sample agent component to a training scene.
5. Configure the ML-Agents `Behavior Parameters` component to match the environment specs:
   - Observation vector length: `ShooterAiTrainingEnvironment.ObservationSpec.Length`.
   - Continuous actions: `ShooterAiTrainingEnvironment.ActionSpec.ContinuousLength`.
   - Discrete actions: `ShooterAiTrainingEnvironment.ActionSpec.DiscreteLength`.

## Shooter Sample

The Shooter sample wraps `ShooterAiTrainingEnvironment` with an ML-Agents `Agent`. It does not replace the existing headless runner. Use it to train policies in Unity when visual debugging or ML-Agents trainers are needed.

For automated server training, continue using `AbilityKit.AI.Training.Runner` and its summary/rollout JSONL output.
