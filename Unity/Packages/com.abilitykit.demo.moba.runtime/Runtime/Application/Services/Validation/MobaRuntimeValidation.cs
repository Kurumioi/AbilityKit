using System;
using System.Collections.Generic;
using System.Text;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Gameplay.Triggering;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaRuntimeValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    public readonly struct MobaRuntimeValidationEntry
    {
        public readonly MobaRuntimeValidationSeverity Severity;
        public readonly string Source;
        public readonly string Path;
        public readonly string Message;
        public readonly string BusinessId;
        public readonly bool BlocksStartup;

        public MobaRuntimeValidationEntry(
            MobaRuntimeValidationSeverity severity,
            string source,
            string path,
            string message,
            string businessId = null,
            bool blocksStartup = true)
        {
            Severity = severity;
            Source = string.IsNullOrEmpty(source) ? "unknown" : source;
            Path = string.IsNullOrEmpty(path) ? "runtime" : path;
            Message = message ?? string.Empty;
            BusinessId = businessId;
            BlocksStartup = blocksStartup;
        }
    }

    public sealed class MobaRuntimeValidationReport
    {
        private readonly List<MobaRuntimeValidationEntry> _entries = new List<MobaRuntimeValidationEntry>(64);

        public IReadOnlyList<MobaRuntimeValidationEntry> Entries => _entries;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;
        public bool ShouldBlockStartup { get; private set; }

        public void Info(string source, string path, string message, string businessId = null)
        {
            Add(new MobaRuntimeValidationEntry(MobaRuntimeValidationSeverity.Info, source, path, message, businessId, blocksStartup: false));
        }

        public void Warning(string source, string path, string message, string businessId = null)
        {
            Add(new MobaRuntimeValidationEntry(MobaRuntimeValidationSeverity.Warning, source, path, message, businessId, blocksStartup: false));
        }

        public void Error(string source, string path, string message, string businessId = null, bool blocksStartup = true)
        {
            Add(new MobaRuntimeValidationEntry(MobaRuntimeValidationSeverity.Error, source, path, message, businessId, blocksStartup));
        }

        public void Add(in MobaRuntimeValidationEntry entry)
        {
            _entries.Add(entry);
            switch (entry.Severity)
            {
                case MobaRuntimeValidationSeverity.Error:
                    ErrorCount++;
                    if (entry.BlocksStartup) ShouldBlockStartup = true;
                    break;
                case MobaRuntimeValidationSeverity.Warning:
                    WarningCount++;
                    break;
                default:
                    InfoCount++;
                    break;
            }
        }

        public string FormatSummary()
        {
            return $"errors={ErrorCount}, warnings={WarningCount}, infos={InfoCount}, blockStartup={ShouldBlockStartup}";
        }

        public string FormatEntry(in MobaRuntimeValidationEntry entry)
        {
            var business = string.IsNullOrEmpty(entry.BusinessId) ? string.Empty : $" businessId={entry.BusinessId}";
            return $"[MobaValidation] {entry.Severity} source={entry.Source} path={entry.Path}{business} {entry.Message}";
        }

        public string FormatAllEntries(int maxEntries = 32)
        {
            if (_entries.Count == 0) return string.Empty;

            var limit = maxEntries <= 0 ? _entries.Count : Math.Min(maxEntries, _entries.Count);
            var sb = new StringBuilder(limit * 96);
            for (int i = 0; i < limit; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.Append(FormatEntry(_entries[i]));
            }

            if (limit < _entries.Count)
            {
                sb.AppendLine();
                sb.Append("[MobaValidation] further entries suppressed. remaining=").Append(_entries.Count - limit);
            }

            return sb.ToString();
        }
    }

    public readonly struct MobaRuntimeValidationContext
    {
        public readonly IWorldResolver Services;
        public readonly string StageName;

        public MobaRuntimeValidationContext(IWorldResolver services, string stageName)
        {
            Services = services;
            StageName = string.IsNullOrEmpty(stageName) ? "runtime" : stageName;
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = null;
            return Services != null && Services.TryResolve(out service) && service != null;
        }
    }

    public interface IMobaRuntimeValidator
    {
        string Name { get; }
        void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report);
    }

    public interface IMobaRuntimeValidationRegistry
    {
        void Register(IMobaRuntimeValidator validator);
        IReadOnlyList<IMobaRuntimeValidator> Validators { get; }
    }

    public interface IMobaRuntimeValidationRunner
    {
        MobaRuntimeValidationReport ValidateAll(in MobaRuntimeValidationContext context);
    }

    public readonly struct MobaRequiredRuntimeValidatorContract
    {
        public readonly string Name;
        public readonly Type ValidatorType;

        public MobaRequiredRuntimeValidatorContract(string name, Type validatorType)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required.", nameof(name));
            ValidatorType = validatorType ?? throw new ArgumentNullException(nameof(validatorType));
            Name = name;
        }
    }

    public sealed class MobaRuntimeValidatorContractValidationResult
    {
        private readonly List<string> _errors = new List<string>(8);

        public IReadOnlyList<string> Errors => _errors;
        public bool Succeeded => _errors.Count == 0;

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                _errors.Add(error);
            }
        }
    }

    public sealed class MobaRuntimeValidatorContract
    {
        private readonly List<MobaRequiredRuntimeValidatorContract> _requiredValidators = new List<MobaRequiredRuntimeValidatorContract>(8);

        public IReadOnlyList<MobaRequiredRuntimeValidatorContract> RequiredValidators => _requiredValidators;

        public static MobaRuntimeValidatorContract CreateDefault()
        {
            var contract = new MobaRuntimeValidatorContract();
            contract.Require<MobaRuntimeDependencyHealthValidator>("runtime.dependencies");
            contract.Require<MobaBattleMainFlowHealthValidator>("battle.main_flow");
            contract.Require<MobaBattleRuntimeReadinessValidator>("runtime.readiness");
            contract.Require<MobaTemporaryEntityLifecycleReadinessValidator>("temp_entity.lifecycle.readiness");
            contract.Require<MobaBattleConfigReferenceValidator>("battle.config.references");
            contract.Require<MobaGameplayTriggerRuntimeValidator>("gameplay.trigger.runtime");
            return contract;
        }

        public void Require<TValidator>(string name)
            where TValidator : IMobaRuntimeValidator, new()
        {
            _requiredValidators.Add(new MobaRequiredRuntimeValidatorContract(name, typeof(TValidator)));
        }

        public void RegisterInto(IMobaRuntimeValidationRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            for (int i = 0; i < _requiredValidators.Count; i++)
            {
                var validator = (IMobaRuntimeValidator)Activator.CreateInstance(_requiredValidators[i].ValidatorType);
                registry.Register(validator);
            }
        }

        public MobaRuntimeValidatorContractValidationResult Validate(IMobaRuntimeValidationRegistry registry)
        {
            var result = new MobaRuntimeValidatorContractValidationResult();
            if (registry == null)
            {
                result.AddError("runtime validator registry is missing.");
                return result;
            }

            for (int i = 0; i < _requiredValidators.Count; i++)
            {
                var required = _requiredValidators[i];
                if (HasValidator(registry.Validators, required)) continue;

                result.AddError($"missing required runtime validator. name={required.Name}, expected={required.ValidatorType.Name}.");
            }

            return result;
        }

        private static bool HasValidator(IReadOnlyList<IMobaRuntimeValidator> validators, in MobaRequiredRuntimeValidatorContract required)
        {
            if (validators == null) return false;

            for (int i = 0; i < validators.Count; i++)
            {
                var validator = validators[i];
                if (validator == null) continue;
                if (validator.GetType() == required.ValidatorType) return true;
                if (string.Equals(validator.Name, required.Name, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }
    }

    public interface IMobaRuntimeValidationHistory
    {
        long RunCount { get; }
        string LastStageName { get; }
        MobaRuntimeValidationReport LastReport { get; }
        bool HasLastReport { get; }
        bool TryGetLastReport(out MobaRuntimeValidationReport report);
    }

    [WorldService(typeof(IMobaRuntimeValidationRegistry), WorldLifetime.Scoped)]
    [WorldService(typeof(IMobaRuntimeValidationRunner), WorldLifetime.Scoped)]
    [WorldService(typeof(IMobaRuntimeValidationHistory), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaRuntimeValidationService), WorldLifetime.Scoped)]
    public sealed class MobaRuntimeValidationService : IMobaRuntimeValidationRegistry, IMobaRuntimeValidationRunner, IMobaRuntimeValidationHistory, IService
    {
        private const string MetricRun = "moba.validation.run";
        private const string MetricBlocked = "moba.validation.blocked";
        private const string MetricErrors = "moba.validation.errors";
        private const string MetricWarnings = "moba.validation.warnings";
        private const string MetricInfos = "moba.validation.infos";

        private readonly List<IMobaRuntimeValidator> _validators = new List<IMobaRuntimeValidator>(16);
        private readonly HashSet<string> _validatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private long _runCount;
        private string _lastStageName;
        private MobaRuntimeValidationReport _lastReport;

        public IReadOnlyList<IMobaRuntimeValidator> Validators => _validators;
        public long RunCount => _runCount;
        public string LastStageName => _lastStageName;
        public MobaRuntimeValidationReport LastReport => _lastReport;
        public bool HasLastReport => _lastReport != null;

        public void Register(IMobaRuntimeValidator validator)
        {
            if (validator == null) return;

            var name = string.IsNullOrEmpty(validator.Name) ? validator.GetType().Name : validator.Name;
            if (!_validatorNames.Add(name)) return;
            _validators.Add(validator);
        }

        public bool TryGetLastReport(out MobaRuntimeValidationReport report)
        {
            report = _lastReport;
            return report != null;
        }

        public MobaRuntimeValidationReport ValidateAll(in MobaRuntimeValidationContext context)
        {
            var report = new MobaRuntimeValidationReport();
            for (int i = 0; i < _validators.Count; i++)
            {
                var validator = _validators[i];
                try
                {
                    validator.Validate(in context, report);
                }
                catch (Exception ex)
                {
                    report.Error(validator.Name, context.StageName, "validator exception: " + ex.Message);
                    MobaRuntimeLog.Exception(ex, MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Exception, nameof(MobaRuntimeValidationService), $"Validator failed. name={validator.Name}");
                }
            }

            _runCount++;
            _lastStageName = context.StageName;
            _lastReport = report;

            RecordDiagnostics(in context, report);
            WriteReport(report);
            return report;
        }

        public void Dispose()
        {
            _validators.Clear();
            _validatorNames.Clear();
            _lastStageName = null;
            _lastReport = null;
            _runCount = 0L;
        }

        private static void RecordDiagnostics(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;
            if (!context.TryResolve<IMobaBattleDiagnosticsService>(out var diagnostics) || diagnostics == null) return;

            diagnostics.Counter(MetricRun);
            diagnostics.Gauge(MetricErrors, report.ErrorCount);
            diagnostics.Gauge(MetricWarnings, report.WarningCount);
            diagnostics.Gauge(MetricInfos, report.InfoCount);

            if (report.ShouldBlockStartup)
            {
                diagnostics.Counter(MetricBlocked);
            }
        }

        private static void WriteReport(MobaRuntimeValidationReport report)
        {
            if (report == null) return;

            if (report.Entries.Count == 0)
            {
                MobaRuntimeLog.Info(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), "Runtime validation completed. no issues.");
                return;
            }

            for (int i = 0; i < report.Entries.Count; i++)
            {
                var entry = report.Entries[i];
                var text = report.FormatEntry(in entry);
                switch (entry.Severity)
                {
                    case MobaRuntimeValidationSeverity.Error:
                        MobaRuntimeLog.Error(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), text);
                        break;
                    case MobaRuntimeValidationSeverity.Warning:
                        MobaRuntimeLog.Warning(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), text);
                        break;
                    default:
                        MobaRuntimeLog.Info(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), text);
                        break;
                }
            }

            MobaRuntimeLog.Warning(MobaRuntimeLogModule.Diagnostics, MobaRuntimeLogPurpose.Validation, nameof(MobaRuntimeValidationService), "Runtime validation completed. " + report.FormatSummary());
        }
    }
}
