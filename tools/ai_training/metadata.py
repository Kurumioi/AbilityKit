from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
from typing import Any

from .behavior_cloning import BehaviorCloningModel
from .dataset import RolloutDataset, SUPPORTED_SCHEMA_VERSION

MODEL_ARTIFACT_SCHEMA_VERSION = 1


@dataclass(frozen=True)
class ModelArtifactMetadata:
    schema_version: int
    artifact_type: str
    environment: str
    model_type: str
    data_schema_version: int
    observation_length: int
    continuous_action_length: int
    discrete_action_length: int
    sample_count: int
    source_dataset_path: str
    model_path: str
    model_sha256: str
    created_utc: str
    training: dict[str, Any]
    metrics: dict[str, Any]

    def to_dict(self) -> dict:
        return {
            "schemaVersion": self.schema_version,
            "artifactType": self.artifact_type,
            "environment": self.environment,
            "modelType": self.model_type,
            "dataSchemaVersion": self.data_schema_version,
            "observationLength": self.observation_length,
            "continuousActionLength": self.continuous_action_length,
            "discreteActionLength": self.discrete_action_length,
            "sampleCount": self.sample_count,
            "sourceDatasetPath": self.source_dataset_path,
            "modelPath": self.model_path,
            "modelSha256": self.model_sha256,
            "createdUtc": self.created_utc,
            "training": self.training,
            "metrics": self.metrics,
        }

    @staticmethod
    def from_dict(data: dict) -> "ModelArtifactMetadata":
        metadata = ModelArtifactMetadata(
            schema_version=_require_int(data, "schemaVersion"),
            artifact_type=_require_string(data, "artifactType"),
            environment=_require_string(data, "environment"),
            model_type=_require_string(data, "modelType"),
            data_schema_version=_require_int(data, "dataSchemaVersion"),
            observation_length=_require_int(data, "observationLength"),
            continuous_action_length=_require_int(data, "continuousActionLength"),
            discrete_action_length=_require_int(data, "discreteActionLength"),
            sample_count=_require_int(data, "sampleCount"),
            source_dataset_path=_require_string(data, "sourceDatasetPath"),
            model_path=_require_string(data, "modelPath"),
            model_sha256=_require_string(data, "modelSha256"),
            created_utc=_require_string(data, "createdUtc"),
            training=_require_object(data, "training"),
            metrics=_require_object(data, "metrics"),
        )
        validate_metadata(metadata)
        return metadata


def create_model_artifact_metadata(
    dataset: RolloutDataset,
    model: BehaviorCloningModel,
    dataset_path: str | Path,
    model_path: str | Path,
    training: dict[str, Any] | None = None,
) -> ModelArtifactMetadata:
    metadata = ModelArtifactMetadata(
        schema_version=MODEL_ARTIFACT_SCHEMA_VERSION,
        artifact_type="abilitykit.ai.model-artifact.v1",
        environment=dataset.environment,
        model_type=model.model_type,
        data_schema_version=dataset.schema_version,
        observation_length=model.observation_length,
        continuous_action_length=model.continuous_action_length,
        discrete_action_length=model.discrete_action_length,
        sample_count=model.sample_count,
        source_dataset_path=str(dataset_path),
        model_path=str(model_path),
        model_sha256=sha256_file(model_path),
        created_utc=datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        training=training or {"algorithm": "behavior_cloning_linear", "dependencyProfile": "python-stdlib"},
        metrics={"meanSquaredError": model.mean_squared_error, "totalReward": dataset.total_reward},
    )
    validate_model_artifact(metadata, dataset, model)
    return metadata


def write_metadata(metadata: ModelArtifactMetadata, path: str | Path) -> None:
    target = Path(path)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(metadata.to_dict(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def read_metadata(path: str | Path) -> ModelArtifactMetadata:
    source = Path(path)
    return ModelArtifactMetadata.from_dict(json.loads(source.read_text(encoding="utf-8")))


def validate_model_artifact(metadata: ModelArtifactMetadata, dataset: RolloutDataset, model: BehaviorCloningModel) -> None:
    validate_metadata(metadata)
    if metadata.environment != dataset.environment:
        raise ValueError("Metadata environment does not match dataset")
    if metadata.model_type != model.model_type:
        raise ValueError("Metadata modelType does not match model")
    if metadata.data_schema_version != dataset.schema_version:
        raise ValueError("Metadata dataSchemaVersion does not match dataset")
    if metadata.observation_length != dataset.observation_length or metadata.observation_length != model.observation_length:
        raise ValueError("Metadata observationLength does not match dataset/model")
    if metadata.continuous_action_length != dataset.continuous_action_length or metadata.continuous_action_length != model.continuous_action_length:
        raise ValueError("Metadata continuousActionLength does not match dataset/model")
    if metadata.discrete_action_length != dataset.discrete_action_length or metadata.discrete_action_length != model.discrete_action_length:
        raise ValueError("Metadata discreteActionLength does not match dataset/model")
    if metadata.sample_count != len(dataset.samples) or metadata.sample_count != model.sample_count:
        raise ValueError("Metadata sampleCount does not match dataset/model")
    if sha256_file(metadata.model_path) != metadata.model_sha256:
        raise ValueError("Metadata modelSha256 does not match model file")


def validate_metadata(metadata: ModelArtifactMetadata) -> None:
    if metadata.schema_version != MODEL_ARTIFACT_SCHEMA_VERSION:
        raise ValueError("Unsupported model artifact schemaVersion")
    if metadata.artifact_type != "abilitykit.ai.model-artifact.v1":
        raise ValueError("Unsupported model artifact type")
    if metadata.data_schema_version != SUPPORTED_SCHEMA_VERSION:
        raise ValueError("Unsupported data schemaVersion")
    if metadata.sample_count <= 0:
        raise ValueError("sampleCount must be positive")
    if not metadata.environment:
        raise ValueError("environment must not be empty")
    if not metadata.model_type:
        raise ValueError("modelType must not be empty")


def sha256_file(path: str | Path) -> str:
    digest = hashlib.sha256()
    with Path(path).open("rb") as file:
        for chunk in iter(lambda: file.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _require_key(data: dict, name: str):
    if not isinstance(data, dict) or name not in data:
        raise ValueError(f"missing field '{name}'")
    return data[name]


def _require_string(data: dict, name: str) -> str:
    value = _require_key(data, name)
    if not isinstance(value, str):
        raise ValueError(f"field '{name}' must be a string")
    return value


def _require_int(data: dict, name: str) -> int:
    value = _require_key(data, name)
    if not isinstance(value, int) or isinstance(value, bool):
        raise ValueError(f"field '{name}' must be an integer")
    return value


def _require_object(data: dict, name: str) -> dict[str, Any]:
    value = _require_key(data, name)
    if not isinstance(value, dict):
        raise ValueError(f"field '{name}' must be an object")
    return value
