from __future__ import annotations

import argparse
from pathlib import Path

from .behavior_cloning import read_behavior_cloning_model, train_behavior_cloning_model, write_behavior_cloning_model
from .dataset import build_dataset_from_jsonl, read_dataset_json, write_dataset_json
from .metadata import create_model_artifact_metadata, read_metadata, validate_metadata, validate_model_artifact, write_metadata


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="AbilityKit offline AI training tools")
    subparsers = parser.add_subparsers(dest="command", required=True)

    build_dataset = subparsers.add_parser("build-dataset", help="Build a dataset JSON file from rollout JSONL")
    build_dataset.add_argument("--rollout", required=True, help="Input rollout JSONL path")
    build_dataset.add_argument("--environment", required=True, help="Environment name, for example shooter or moba")
    build_dataset.add_argument("--output", required=True, help="Output dataset JSON path")

    train = subparsers.add_parser("train-bc", help="Train the stdlib behavior cloning baseline")
    train.add_argument("--dataset", required=True, help="Input dataset JSON path")
    train.add_argument("--model", required=True, help="Output model JSON path")
    train.add_argument("--metadata", required=True, help="Output metadata JSON path")
    train.add_argument("--epochs", type=int, default=400, help="Training epochs")
    train.add_argument("--learning-rate", type=float, default=0.01, help="Training learning rate")

    validate = subparsers.add_parser("validate-metadata", help="Validate a model artifact metadata JSON file")
    validate.add_argument("--metadata", required=True, help="Metadata JSON path")
    validate.add_argument("--dataset", help="Dataset JSON path for full artifact validation")
    validate.add_argument("--model", help="Model JSON path for full artifact validation")

    args = parser.parse_args(argv)
    if args.command == "build-dataset":
        dataset = build_dataset_from_jsonl(args.rollout, environment=args.environment)
        write_dataset_json(dataset, args.output)
        print(
            f"dataset=true samples={len(dataset.samples)} observationLength={dataset.observation_length} "
            f"continuousActionLength={dataset.continuous_action_length} discreteActionLength={dataset.discrete_action_length}"
        )
        return 0

    if args.command == "train-bc":
        dataset = read_dataset_json(args.dataset)
        model = train_behavior_cloning_model(dataset, learning_rate=args.learning_rate, epochs=args.epochs)
        write_behavior_cloning_model(model, args.model)
        metadata = create_model_artifact_metadata(
            dataset,
            model,
            dataset_path=args.dataset,
            model_path=args.model,
            training={
                "algorithm": "behavior_cloning_linear",
                "dependencyProfile": "python-stdlib",
                "epochs": args.epochs,
                "learningRate": args.learning_rate,
            },
        )
        write_metadata(metadata, args.metadata)
        print(f"model=true samples={model.sample_count} meanSquaredError={model.mean_squared_error:.6f}")
        return 0

    if args.command == "validate-metadata":
        metadata = read_metadata(args.metadata)
        if args.dataset or args.model:
            if not args.dataset or not args.model:
                parser.error("validate-metadata requires both --dataset and --model for full artifact validation")
            dataset = read_dataset_json(args.dataset)
            model = read_behavior_cloning_model(args.model)
            validate_model_artifact(metadata, dataset, model)
            print(f"metadata=true artifact=true artifactType={metadata.artifact_type} modelType={metadata.model_type}")
        else:
            validate_metadata(metadata)
            print(f"metadata=true artifact=false artifactType={metadata.artifact_type} modelType={metadata.model_type}")
        return 0

    raise AssertionError(f"Unsupported command: {args.command}")


if __name__ == "__main__":
    raise SystemExit(main())
