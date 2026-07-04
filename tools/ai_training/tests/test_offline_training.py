from __future__ import annotations

import json
from pathlib import Path
import tempfile
import unittest

from tools.ai_training.behavior_cloning import read_behavior_cloning_model, train_behavior_cloning_model
from tools.ai_training.cli import main
from tools.ai_training.dataset import build_dataset_from_jsonl, read_dataset_json, write_dataset_json
from tools.ai_training.metadata import create_model_artifact_metadata, read_metadata, validate_model_artifact, write_metadata


class OfflineTrainingTests(unittest.TestCase):
    def test_build_dataset_from_rollout_jsonl_reads_step_rows(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            rollout_path = Path(directory) / "rollout.jsonl"
            rollout_path.write_text(_sample_rollout_jsonl(), encoding="utf-8")

            dataset = build_dataset_from_jsonl(rollout_path, environment="shooter")

            self.assertEqual("shooter", dataset.environment)
            self.assertEqual(2, len(dataset.samples))
            self.assertEqual(3, dataset.observation_length)
            self.assertEqual(2, dataset.continuous_action_length)
            self.assertEqual(1, dataset.discrete_action_length)
            self.assertEqual(1, dataset.episode_count)
            self.assertEqual(9, dataset.samples[0].state_hash)

    def test_build_dataset_rejects_unsupported_schema(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            rollout_path = Path(directory) / "rollout.jsonl"
            row = _step_row()
            row["schemaVersion"] = 99
            rollout_path.write_text(json.dumps(row) + "\n", encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "unsupported schemaVersion"):
                build_dataset_from_jsonl(rollout_path, environment="shooter")

    def test_train_behavior_cloning_model_predicts_action_shape(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            rollout_path = Path(directory) / "rollout.jsonl"
            rollout_path.write_text(_sample_rollout_jsonl(), encoding="utf-8")
            dataset = build_dataset_from_jsonl(rollout_path, environment="shooter")

            model = train_behavior_cloning_model(dataset, learning_rate=0.02, epochs=80)
            continuous, discrete = model.predict(dataset.samples[0].observation)

            self.assertEqual(2, len(continuous))
            self.assertEqual((1,), discrete)
            self.assertEqual(2, model.sample_count)
            self.assertGreaterEqual(model.mean_squared_error, 0.0)

    def test_behavior_cloning_model_rejects_invalid_export_shapes(self) -> None:
        model_data = _model_dict()
        loaded = read_behavior_cloning_model(_write_model_json(model_data))
        self.assertEqual(2, loaded.observation_length)

        invalid_row_count = _model_dict()
        invalid_row_count["continuousWeights"] = [[1.0, 2.0]]
        with self.assertRaisesRegex(ValueError, "continuousWeights row count"):
            read_behavior_cloning_model(_write_model_json(invalid_row_count))

        invalid_row_length = _model_dict()
        invalid_row_length["continuousWeights"] = [[1.0], [0.5, -1.0]]
        with self.assertRaisesRegex(ValueError, "continuousWeights row 0 length"):
            read_behavior_cloning_model(_write_model_json(invalid_row_length))

        invalid_discrete_defaults = _model_dict()
        invalid_discrete_defaults["discreteDefaults"] = [1.0]
        with self.assertRaisesRegex(ValueError, "discreteDefaults"):
            read_behavior_cloning_model(_write_model_json(invalid_discrete_defaults))

    def test_metadata_validates_dataset_model_and_file_hash(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            rollout_path = base / "rollout.jsonl"
            dataset_path = base / "dataset.json"
            model_path = base / "model.json"
            metadata_path = base / "metadata.json"
            rollout_path.write_text(_sample_rollout_jsonl(), encoding="utf-8")
            dataset = build_dataset_from_jsonl(rollout_path, environment="shooter")
            write_dataset_json(dataset, dataset_path)
            model = train_behavior_cloning_model(dataset, epochs=40)
            model_path.write_text(json.dumps(model.to_dict(), indent=2), encoding="utf-8")

            metadata = create_model_artifact_metadata(dataset, model, dataset_path, model_path)
            write_metadata(metadata, metadata_path)
            loaded = read_metadata(metadata_path)

            validate_model_artifact(loaded, dataset, model)
            model_path.write_text("{}", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "modelSha256"):
                validate_model_artifact(loaded, dataset, model)

    def test_cli_build_dataset_train_and_validate_metadata(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            rollout_path = base / "rollout.jsonl"
            dataset_path = base / "dataset.json"
            model_path = base / "model.json"
            metadata_path = base / "metadata.json"
            rollout_path.write_text(_sample_rollout_jsonl(), encoding="utf-8")

            self.assertEqual(0, main(["build-dataset", "--rollout", str(rollout_path), "--environment", "shooter", "--output", str(dataset_path)]))
            self.assertEqual(0, main(["train-bc", "--dataset", str(dataset_path), "--model", str(model_path), "--metadata", str(metadata_path), "--epochs", "20"]))
            self.assertEqual(0, main(["validate-metadata", "--metadata", str(metadata_path)]))
            self.assertEqual(0, main(["validate-metadata", "--metadata", str(metadata_path), "--dataset", str(dataset_path), "--model", str(model_path)]))

            dataset = read_dataset_json(dataset_path)
            model = read_behavior_cloning_model(model_path)
            metadata = read_metadata(metadata_path)
            self.assertEqual(len(dataset.samples), metadata.sample_count)
            self.assertEqual(model.model_type, metadata.model_type)
            model_path.write_text("{}", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "modelType"):
                main(["validate-metadata", "--metadata", str(metadata_path), "--dataset", str(dataset_path), "--model", str(model_path)])

            write_metadata(metadata, metadata_path)
            model_path.write_text(json.dumps(model.to_dict(), indent=2), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "modelSha256"):
                main(["validate-metadata", "--metadata", str(metadata_path), "--dataset", str(dataset_path), "--model", str(model_path)])



def _model_dict() -> dict:
    return {
        "modelType": "abilitykit.behavior_cloning.linear.v1",
        "observationLength": 2,
        "continuousActionLength": 2,
        "discreteActionLength": 1,
        "continuousWeights": [[1.0, 2.0], [0.5, -1.0]],
        "continuousBias": [0.25, -0.5],
        "discreteDefaults": [2],
        "sampleCount": 3,
        "meanSquaredError": 0.01,
    }


def _write_model_json(data: dict) -> Path:
    directory = tempfile.TemporaryDirectory()
    path = Path(directory.name) / "model.json"
    path.write_text(json.dumps(data), encoding="utf-8")
    _MODEL_TEMP_DIRECTORIES.append(directory)
    return path


_MODEL_TEMP_DIRECTORIES: list[tempfile.TemporaryDirectory[str]] = []


def _sample_rollout_jsonl() -> str:
    rows = [
        {"schemaVersion": 1, "type": "run", "episodes": 1, "totalSteps": 2, "totalReward": 3.0, "averageReward": 3.0, "averageSteps": 2.0, "completedEpisodes": 0, "truncatedEpisodes": 1, "seed": 7, "maxSteps": 2, "fixedDeltaSeconds": 0.0333333},
        _step_row(step_index=1, observation=[1.0, 0.0, 0.5], continuous=[0.5, -0.25], discrete=[1], reward=1.0, state_hash=9),
        _step_row(step_index=2, observation=[0.0, 1.0, 0.25], continuous=[0.25, 0.75], discrete=[1], reward=2.0, state_hash=10),
    ]
    return "\n".join(json.dumps(row) for row in rows) + "\n"


def _step_row(
    step_index: int = 1,
    observation: list[float] | None = None,
    continuous: list[float] | None = None,
    discrete: list[int] | None = None,
    reward: float = 1.0,
    state_hash: int = 9,
) -> dict:
    return {
        "schemaVersion": 1,
        "type": "step",
        "episodeIndex": 0,
        "seed": 7,
        "stepIndex": step_index,
        "observation": observation or [1.0, 0.0, 0.5],
        "continuousAction": continuous or [0.5, -0.25],
        "discreteAction": discrete or [1],
        "reward": reward,
        "done": False,
        "truncated": step_index >= 2,
        "stateHash": state_hash,
    }


if __name__ == "__main__":
    unittest.main()
