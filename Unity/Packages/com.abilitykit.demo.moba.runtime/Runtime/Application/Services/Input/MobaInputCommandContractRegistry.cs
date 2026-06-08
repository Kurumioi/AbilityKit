using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaInputCommandContract
    {
        public MobaInputCommandContract(int opCode, Type handlerType, string name, bool required)
        {
            OpCode = opCode;
            HandlerType = handlerType;
            Name = string.IsNullOrEmpty(name) ? handlerType?.Name : name;
            Required = required;
        }

        public int OpCode { get; }
        public Type HandlerType { get; }
        public string Name { get; }
        public bool Required { get; }
    }

    public sealed class MobaInputCommandContractValidationResult
    {
        private readonly List<string> _errors = new List<string>(4);

        public IReadOnlyList<string> Errors => _errors;
        public bool Succeeded => _errors.Count == 0;

        public void AddError(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _errors.Add(message);
        }
    }

    [WorldService(typeof(MobaInputCommandContractRegistry), WorldLifetime.Singleton)]
    public sealed class MobaInputCommandContractRegistry
    {
        private readonly Dictionary<int, MobaInputCommandContract> _contracts = new Dictionary<int, MobaInputCommandContract>();
        private readonly List<MobaInputCommandContract> _contractList = new List<MobaInputCommandContract>(4);

        public MobaInputCommandContractRegistry(MobaInputCommandHandlerRegistry handlers)
        {
            HandlerRegistry = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        public MobaInputCommandHandlerRegistry HandlerRegistry { get; }
        public IReadOnlyList<MobaInputCommandContract> Contracts => _contractList;
        public int ContractCount => _contractList.Count;

        public static MobaInputCommandContractRegistry CreateDefault()
        {
            var registry = new MobaInputCommandContractRegistry(MobaInputCommandHandlerRegistry.CreateDefault());
            registry.Require(AbilityKit.Protocol.Moba.MobaOpCodes.Input.Move, typeof(MobaMoveInputCommandHandler), "Move");
            registry.Require(AbilityKit.Protocol.Moba.MobaOpCodes.Input.SkillInput, typeof(MobaSkillInputCommandHandler), "SkillInput");
            return registry;
        }

        public void Require(int opCode, Type handlerType, string name = null)
        {
            Register(new MobaInputCommandContract(opCode, handlerType, name, required: true));
        }

        public void Register(in MobaInputCommandContract contract)
        {
            if (contract.OpCode <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contract.OpCode), contract.OpCode, "input command opCode must be positive.");
            }

            if (contract.HandlerType == null)
            {
                throw new ArgumentNullException(nameof(contract.HandlerType), "input command handler type is required.");
            }

            if (!typeof(IMobaInputCommandHandler).IsAssignableFrom(contract.HandlerType))
            {
                throw new ArgumentException($"input command handler type must implement {nameof(IMobaInputCommandHandler)}. type={contract.HandlerType.FullName}");
            }

            if (_contracts.ContainsKey(contract.OpCode))
            {
                throw new InvalidOperationException($"duplicate input command contract. opCode={contract.OpCode}, name={contract.Name}");
            }

            _contracts.Add(contract.OpCode, contract);
            _contractList.Add(contract);
        }

        public bool TryGetContract(int opCode, out MobaInputCommandContract contract)
        {
            return _contracts.TryGetValue(opCode, out contract);
        }

        public MobaInputCommandContractValidationResult Validate()
        {
            var result = new MobaInputCommandContractValidationResult();
            if (_contractList.Count == 0)
            {
                result.AddError("input command contract registry has no declared contracts.");
                return result;
            }

            for (int i = 0; i < _contractList.Count; i++)
            {
                var contract = _contractList[i];
                if (!contract.Required) continue;

                if (!HandlerRegistry.TryGetHandlerDescriptor(contract.OpCode, out var descriptor))
                {
                    result.AddError($"missing input command handler. opCode={contract.OpCode}, name={contract.Name}, expected={contract.HandlerType.Name}");
                    continue;
                }

                if (descriptor.HandlerType == null || !contract.HandlerType.IsAssignableFrom(descriptor.HandlerType))
                {
                    var actual = descriptor.HandlerType == null ? "null" : descriptor.HandlerType.Name;
                    result.AddError($"input command handler type mismatch. opCode={contract.OpCode}, name={contract.Name}, expected={contract.HandlerType.Name}, actual={actual}");
                }
            }

            return result;
        }
    }
}
