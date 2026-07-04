from __future__ import annotations

from dataclasses import dataclass
import json
from math import sqrt
from pathlib import Path

from .dataset import RolloutDataset, RolloutSample, read_dataset_json


@dataclass(frozen=True)
class BehaviorCloningModel:
    model_type: str
    observation_length: int
    continuous_action_length: int
    discrete_action_length: int
    continuous_weights: tuple[tuple[float, ...], ...]
    continuous_bias: tuple[float, ...]
    discrete_defaults: tuple[int, ...]
    sample_count: int
    mean_squared_error: float

    def predict(self, observation: tuple[float, ...]) -> tuple[tuple[float, ...], tuple[int, ...]]:
        if len(observation) != self.observation_length:
            raise ValueError("Observation length does not match model")
        continuous = []
        for output_index in range(self.continuous_action_length):
            value = self.continuous_bias[output_index]
            weights = self.continuous_weights[output_index]
            for input_index, input_value in enumerate(observation):
                value += weights[input_index] * input_value
            continuous.append(value)
        return tuple(continuous), self.discrete_defaults

    def to_dict(self) -> dict:
        return {
            "modelType": self.model_type,
            "observationLength": self.observation_length,
            "continuousActionLength": self.continuous_action_length,
            "discreteActionLength": self.discrete_action_length,
            "continuousWeights": [list(row) for row in self.continuous_weights],
            "continuousBias": list(self.continuous_bias),
            "discreteDefaults": list(self.discrete_defaults),
            "sampleCount": self.sample_count,
            "meanSquaredError": self.mean_squared_error,
        }

    @staticmethod
    def from_dict(data: dict) -> "BehaviorCloningModel":
        model = BehaviorCloningModel(
            model_type=_require_string(data, "modelType"),
            observation_length=_require_int(data, "observationLength"),
            continuous_action_length=_require_int(data, "continuousActionLength"),
            discrete_action_length=_require_int(data, "discreteActionLength"),
            continuous_weights=_require_number_matrix(data, "continuousWeights"),
            continuous_bias=tuple(_require_number_array(data, "continuousBias")),
            discrete_defaults=tuple(_require_int_array(data, "discreteDefaults")),
            sample_count=_require_int(data, "sampleCount"),
            mean_squared_error=float(_require_number(data, "meanSquaredError")),
        )
        _validate_model_shape(model)
        return model


def train_behavior_cloning_model(dataset: RolloutDataset, learning_rate: float = 0.01, epochs: int = 400) -> BehaviorCloningModel:
    if not dataset.samples:
        raise ValueError("Dataset must contain at least one sample")
    if learning_rate <= 0:
        raise ValueError("learning_rate must be positive")
    if epochs <= 0:
        raise ValueError("epochs must be positive")

    weights = [[0.0 for _ in range(dataset.observation_length)] for _ in range(dataset.continuous_action_length)]
    bias = [0.0 for _ in range(dataset.continuous_action_length)]
    scale = _max_observation_magnitude(dataset.samples)

    for _ in range(epochs):
        for sample in dataset.samples:
            observation = tuple(value / scale for value in sample.observation)
            for output_index in range(dataset.continuous_action_length):
                prediction = bias[output_index]
                for input_index, input_value in enumerate(observation):
                    prediction += weights[output_index][input_index] * input_value
                error = prediction - sample.continuous_action[output_index]
                bias[output_index] -= learning_rate * error
                for input_index, input_value in enumerate(observation):
                    weights[output_index][input_index] -= learning_rate * error * input_value

    unscaled_weights = tuple(tuple(value / scale for value in row) for row in weights)
    model = BehaviorCloningModel(
        model_type="abilitykit.behavior_cloning.linear.v1",
        observation_length=dataset.observation_length,
        continuous_action_length=dataset.continuous_action_length,
        discrete_action_length=dataset.discrete_action_length,
        continuous_weights=unscaled_weights,
        continuous_bias=tuple(bias),
        discrete_defaults=_most_common_discrete_actions(dataset.samples, dataset.discrete_action_length),
        sample_count=len(dataset.samples),
        mean_squared_error=0.0,
    )
    return BehaviorCloningModel(
        model_type=model.model_type,
        observation_length=model.observation_length,
        continuous_action_length=model.continuous_action_length,
        discrete_action_length=model.discrete_action_length,
        continuous_weights=model.continuous_weights,
        continuous_bias=model.continuous_bias,
        discrete_defaults=model.discrete_defaults,
        sample_count=model.sample_count,
        mean_squared_error=_mean_squared_error(model, dataset.samples),
    )


def write_behavior_cloning_model(model: BehaviorCloningModel, path: str | Path) -> None:
    target = Path(path)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(model.to_dict(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def read_behavior_cloning_model(path: str | Path) -> BehaviorCloningModel:
    source = Path(path)
    return BehaviorCloningModel.from_dict(json.loads(source.read_text(encoding="utf-8")))


def train_behavior_cloning_model_from_dataset_file(dataset_path: str | Path, model_path: str | Path) -> BehaviorCloningModel:
    dataset = read_dataset_json(dataset_path)
    model = train_behavior_cloning_model(dataset)
    write_behavior_cloning_model(model, model_path)
    return model


def _max_observation_magnitude(samples: tuple[RolloutSample, ...]) -> float:
    max_value = 1.0
    for sample in samples:
        for value in sample.observation:
            max_value = max(max_value, abs(value))
    return max_value


def _most_common_discrete_actions(samples: tuple[RolloutSample, ...], discrete_action_length: int) -> tuple[int, ...]:
    defaults = []
    for action_index in range(discrete_action_length):
        counts: dict[int, int] = {}
        for sample in samples:
            value = sample.discrete_action[action_index]
            counts[value] = counts.get(value, 0) + 1
        defaults.append(max(counts.items(), key=lambda item: (item[1], -item[0]))[0] if counts else 0)
    return tuple(defaults)


def _mean_squared_error(model: BehaviorCloningModel, samples: tuple[RolloutSample, ...]) -> float:
    if model.continuous_action_length == 0:
        return 0.0
    total = 0.0
    count = 0
    for sample in samples:
        predicted, _ = model.predict(sample.observation)
        for index, value in enumerate(predicted):
            error = value - sample.continuous_action[index]
            total += error * error
            count += 1
    return sqrt(total / count) if count else 0.0


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


def _require_number(data: dict, name: str) -> float | int:
    value = _require_key(data, name)
    if not isinstance(value, (float, int)) or isinstance(value, bool):
        raise ValueError(f"field '{name}' must be a number")
    return value


def _require_list(data: dict, name: str) -> list:
    value = _require_key(data, name)
    if not isinstance(value, list):
        raise ValueError(f"field '{name}' must be an array")
    return value


def _require_number_array(data: dict, name: str) -> list[float]:
    values = _require_list(data, name)
    result = []
    for value in values:
        if not isinstance(value, (float, int)) or isinstance(value, bool):
            raise ValueError(f"field '{name}' must contain only numbers")
        result.append(float(value))
    return result


def _require_int_array(data: dict, name: str) -> list[int]:
    values = _require_list(data, name)
    result = []
    for value in values:
        if not isinstance(value, int) or isinstance(value, bool):
            raise ValueError(f"field '{name}' must contain only integers")
        result.append(value)
    return result


def _require_number_matrix(data: dict, name: str) -> tuple[tuple[float, ...], ...]:
    rows = _require_list(data, name)
    matrix = []
    for row_index, row in enumerate(rows):
        if not isinstance(row, list):
            raise ValueError(f"field '{name}' row {row_index} must be an array")
        values = []
        for value in row:
            if not isinstance(value, (float, int)) or isinstance(value, bool):
                raise ValueError(f"field '{name}' row {row_index} must contain only numbers")
            values.append(float(value))
        matrix.append(tuple(values))
    return tuple(matrix)


def _validate_model_shape(model: BehaviorCloningModel) -> None:
    if model.observation_length < 0:
        raise ValueError("observationLength must be non-negative")
    if model.continuous_action_length < 0:
        raise ValueError("continuousActionLength must be non-negative")
    if model.discrete_action_length < 0:
        raise ValueError("discreteActionLength must be non-negative")
    if model.sample_count <= 0:
        raise ValueError("sampleCount must be positive")
    if len(model.continuous_weights) != model.continuous_action_length:
        raise ValueError("continuousWeights row count must match continuousActionLength")
    for row_index, row in enumerate(model.continuous_weights):
        if len(row) != model.observation_length:
            raise ValueError(f"continuousWeights row {row_index} length must match observationLength")
    if len(model.continuous_bias) != model.continuous_action_length:
        raise ValueError("continuousBias length must match continuousActionLength")
    if len(model.discrete_defaults) != model.discrete_action_length:
        raise ValueError("discreteDefaults length must match discreteActionLength")
