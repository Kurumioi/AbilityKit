from __future__ import annotations

from dataclasses import dataclass
import json
from pathlib import Path
from typing import Iterable, Sequence

SUPPORTED_SCHEMA_VERSION = 1
STEP_ROW_TYPE = "step"


@dataclass(frozen=True)
class RolloutSample:
    episode_index: int
    seed: int
    step_index: int
    observation: tuple[float, ...]
    continuous_action: tuple[float, ...]
    discrete_action: tuple[int, ...]
    reward: float
    done: bool
    truncated: bool
    state_hash: int


@dataclass(frozen=True)
class RolloutDataset:
    schema_version: int
    environment: str
    source_path: str
    samples: tuple[RolloutSample, ...]
    observation_length: int
    continuous_action_length: int
    discrete_action_length: int
    total_reward: float
    episode_count: int
    seed_count: int

    def to_dict(self) -> dict:
        return {
            "schemaVersion": self.schema_version,
            "environment": self.environment,
            "sourcePath": self.source_path,
            "sampleCount": len(self.samples),
            "observationLength": self.observation_length,
            "continuousActionLength": self.continuous_action_length,
            "discreteActionLength": self.discrete_action_length,
            "totalReward": self.total_reward,
            "episodeCount": self.episode_count,
            "seedCount": self.seed_count,
            "samples": [
                {
                    "episodeIndex": sample.episode_index,
                    "seed": sample.seed,
                    "stepIndex": sample.step_index,
                    "observation": list(sample.observation),
                    "continuousAction": list(sample.continuous_action),
                    "discreteAction": list(sample.discrete_action),
                    "reward": sample.reward,
                    "done": sample.done,
                    "truncated": sample.truncated,
                    "stateHash": sample.state_hash,
                }
                for sample in self.samples
            ],
        }

    @staticmethod
    def from_dict(data: dict) -> "RolloutDataset":
        samples = tuple(
            RolloutSample(
                episode_index=_require_int(row, "episodeIndex"),
                seed=_require_int(row, "seed"),
                step_index=_require_int(row, "stepIndex"),
                observation=tuple(_require_number_array(row, "observation")),
                continuous_action=tuple(_require_number_array(row, "continuousAction")),
                discrete_action=tuple(_require_int_array(row, "discreteAction")),
                reward=float(_require_number(row, "reward")),
                done=_require_bool(row, "done"),
                truncated=_require_bool(row, "truncated"),
                state_hash=_require_int(row, "stateHash"),
            )
            for row in _require_list(data, "samples")
        )
        dataset = RolloutDataset(
            schema_version=_require_int(data, "schemaVersion"),
            environment=_require_string(data, "environment"),
            source_path=_require_string(data, "sourcePath"),
            samples=samples,
            observation_length=_require_int(data, "observationLength"),
            continuous_action_length=_require_int(data, "continuousActionLength"),
            discrete_action_length=_require_int(data, "discreteActionLength"),
            total_reward=float(_require_number(data, "totalReward")),
            episode_count=_require_int(data, "episodeCount"),
            seed_count=_require_int(data, "seedCount"),
        )
        _validate_dataset_shape(dataset)
        return dataset


def build_dataset_from_jsonl(path: str | Path, environment: str = "unknown") -> RolloutDataset:
    source = Path(path)
    samples = tuple(_read_step_samples(source))
    if not samples:
        raise ValueError(f"Rollout JSONL contains no step rows: {source}")

    observation_length = len(samples[0].observation)
    continuous_action_length = len(samples[0].continuous_action)
    discrete_action_length = len(samples[0].discrete_action)
    dataset = RolloutDataset(
        schema_version=SUPPORTED_SCHEMA_VERSION,
        environment=environment,
        source_path=str(source),
        samples=samples,
        observation_length=observation_length,
        continuous_action_length=continuous_action_length,
        discrete_action_length=discrete_action_length,
        total_reward=sum(sample.reward for sample in samples),
        episode_count=len({sample.episode_index for sample in samples}),
        seed_count=len({sample.seed for sample in samples}),
    )
    _validate_dataset_shape(dataset)
    return dataset


def write_dataset_json(dataset: RolloutDataset, path: str | Path) -> None:
    target = Path(path)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(dataset.to_dict(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def read_dataset_json(path: str | Path) -> RolloutDataset:
    source = Path(path)
    return RolloutDataset.from_dict(json.loads(source.read_text(encoding="utf-8")))


def _read_step_samples(path: Path) -> Iterable[RolloutSample]:
    with path.open("r", encoding="utf-8") as file:
        for line_number, raw_line in enumerate(file, start=1):
            line = raw_line.strip()
            if not line:
                continue
            try:
                row = json.loads(line)
            except json.JSONDecodeError as exc:
                raise ValueError(f"Line {line_number}: invalid JSON: {exc.msg}") from exc
            if _require_int(row, "schemaVersion", line_number) != SUPPORTED_SCHEMA_VERSION:
                raise ValueError(f"Line {line_number}: unsupported schemaVersion")
            row_type = _require_string(row, "type", line_number)
            if row_type != STEP_ROW_TYPE:
                continue
            yield RolloutSample(
                episode_index=_require_int(row, "episodeIndex", line_number),
                seed=_require_int(row, "seed", line_number),
                step_index=_require_int(row, "stepIndex", line_number),
                observation=tuple(_require_number_array(row, "observation", line_number)),
                continuous_action=tuple(_require_number_array(row, "continuousAction", line_number)),
                discrete_action=tuple(_require_int_array(row, "discreteAction", line_number)),
                reward=float(_require_number(row, "reward", line_number)),
                done=_require_bool(row, "done", line_number),
                truncated=_require_bool(row, "truncated", line_number),
                state_hash=_require_int(row, "stateHash", line_number),
            )


def _validate_dataset_shape(dataset: RolloutDataset) -> None:
    if dataset.schema_version != SUPPORTED_SCHEMA_VERSION:
        raise ValueError("Unsupported dataset schemaVersion")
    if not dataset.samples:
        raise ValueError("Dataset must contain at least one sample")
    for index, sample in enumerate(dataset.samples):
        if len(sample.observation) != dataset.observation_length:
            raise ValueError(f"Sample {index}: observation length mismatch")
        if len(sample.continuous_action) != dataset.continuous_action_length:
            raise ValueError(f"Sample {index}: continuous action length mismatch")
        if len(sample.discrete_action) != dataset.discrete_action_length:
            raise ValueError(f"Sample {index}: discrete action length mismatch")


def _require_key(data: dict, name: str, line_number: int | None = None):
    if not isinstance(data, dict) or name not in data:
        location = f"Line {line_number}: " if line_number is not None else ""
        raise ValueError(f"{location}missing field '{name}'")
    return data[name]


def _require_string(data: dict, name: str, line_number: int | None = None) -> str:
    value = _require_key(data, name, line_number)
    if not isinstance(value, str):
        raise ValueError(f"{_prefix(line_number)}field '{name}' must be a string")
    return value


def _require_int(data: dict, name: str, line_number: int | None = None) -> int:
    value = _require_key(data, name, line_number)
    if not isinstance(value, int) or isinstance(value, bool):
        raise ValueError(f"{_prefix(line_number)}field '{name}' must be an integer")
    return value


def _require_number(data: dict, name: str, line_number: int | None = None) -> float | int:
    value = _require_key(data, name, line_number)
    if not isinstance(value, (int, float)) or isinstance(value, bool):
        raise ValueError(f"{_prefix(line_number)}field '{name}' must be a number")
    return value


def _require_bool(data: dict, name: str, line_number: int | None = None) -> bool:
    value = _require_key(data, name, line_number)
    if not isinstance(value, bool):
        raise ValueError(f"{_prefix(line_number)}field '{name}' must be a boolean")
    return value


def _require_list(data: dict, name: str) -> list:
    value = _require_key(data, name)
    if not isinstance(value, list):
        raise ValueError(f"field '{name}' must be an array")
    return value


def _require_number_array(data: dict, name: str, line_number: int | None = None) -> list[float]:
    values = _require_key(data, name, line_number)
    _validate_sequence(values, name, line_number)
    result = []
    for value in values:
        if not isinstance(value, (int, float)) or isinstance(value, bool):
            raise ValueError(f"{_prefix(line_number)}field '{name}' must contain only numbers")
        result.append(float(value))
    return result


def _require_int_array(data: dict, name: str, line_number: int | None = None) -> list[int]:
    values = _require_key(data, name, line_number)
    _validate_sequence(values, name, line_number)
    result = []
    for value in values:
        if not isinstance(value, int) or isinstance(value, bool):
            raise ValueError(f"{_prefix(line_number)}field '{name}' must contain only integers")
        result.append(value)
    return result


def _validate_sequence(values: object, name: str, line_number: int | None) -> None:
    if not isinstance(values, Sequence) or isinstance(values, (str, bytes, bytearray)):
        raise ValueError(f"{_prefix(line_number)}field '{name}' must be an array")


def _prefix(line_number: int | None) -> str:
    return f"Line {line_number}: " if line_number is not None else ""
