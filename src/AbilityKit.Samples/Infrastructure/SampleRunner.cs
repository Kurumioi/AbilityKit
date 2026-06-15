using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Infrastructure
{
    public sealed class SampleRunner
    {
        private readonly List<SampleInfo> _samples = new();
        private readonly Dictionary<SampleCategory, List<SampleInfo>> _grouped = new();
        private readonly SampleRunOptions _options;
        private readonly IConfigProvider? _config;
        private readonly IResourceProvider? _resources;

        public SampleRunner(
            SampleRunOptions? options = null,
            IConfigProvider? config = null,
            IResourceProvider? resources = null)
        {
            _options = options ?? new SampleRunOptions();
            _config = config;
            _resources = resources;
        }

        public IReadOnlyList<SampleInfo> AllSamples => _samples;
        public IReadOnlyDictionary<SampleCategory, List<SampleInfo>> Grouped => _grouped;

        public void Register(Type sampleType)
        {
            if (sampleType == null)
                throw new ArgumentNullException(nameof(sampleType));
            if (!typeof(ISample).IsAssignableFrom(sampleType))
                throw new ArgumentException("Type must implement ISample.", nameof(sampleType));

            var sample = CreateSample(sampleType);
            Register(new SampleInfo
            {
                Index = _samples.Count,
                Id = SampleCatalog.CreateStableId(sample.Category, sample.Title),
                Title = sample.Title,
                Description = sample.Description,
                Category = sample.Category,
                SampleType = sampleType,
                Factory = () => CreateSample(sampleType)
            });
        }

        public void Register(ISample sample)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));

            Register(new SampleInfo
            {
                Index = _samples.Count,
                Id = SampleCatalog.CreateStableId(sample.Category, sample.Title),
                Title = sample.Title,
                Description = sample.Description,
                Category = sample.Category,
                SampleType = sample.GetType(),
                Factory = () => sample
            });
        }

        public void Register(SampleCatalogEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            Register(new SampleInfo
            {
                Index = _samples.Count,
                Id = entry.Id,
                Title = entry.Title,
                Description = entry.Description,
                Category = entry.Category,
                SampleType = entry.SampleType,
                Factory = entry.CreateSample
            });
        }

        public int Run(SampleRunRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            switch (request.SelectionKind)
            {
                case SampleRunSelectionKind.Index:
                    return request.Index.HasValue && Run(request.Index.Value) ? 1 : 0;
                case SampleRunSelectionKind.Id:
                    return Run(request.Id) ? 1 : 0;
                case SampleRunSelectionKind.All:
                    return RunAll();
                default:
                    Console.Error.WriteLine("[ERR] Sample run request did not select a sample.");
                    return 0;
            }
        }

        public bool Run(int index)
        {
            if (index < 0 || index >= _samples.Count)
            {
                Console.Error.WriteLine($"[ERR] Invalid sample index: {index}");
                return false;
            }

            return Run(_samples[index]);
        }

        public bool Run(string id)
        {
            var sample = _samples.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (sample == null)
            {
                Console.Error.WriteLine($"[ERR] Invalid sample id: {id}");
                return false;
            }

            return Run(sample);
        }

        public int RunAll()
        {
            var passed = 0;
            foreach (var sample in _samples)
            {
                if (Run(sample))
                    passed++;
            }

            return passed;
        }

        public void PrintMenu()
        {
            Console.WriteLine();

            foreach (var category in _grouped.Keys.OrderBy(k => (int)k))
            {
                var samples = _grouped[category];
                if (samples.Count == 0)
                    continue;

                Console.WriteLine($"-- {category.GetDisplayName()} --");
                foreach (var sample in samples)
                    Console.WriteLine($"  [{sample.Index:D2}] {sample.Title}  ({sample.Id})");
            }

            Console.WriteLine();
        }

        public void PrintHeader()
        {
            Console.WriteLine("AbilityKit.Samples");
            Console.WriteLine("Pure logic samples with host-provided runtime output and environments.");
            Console.WriteLine();
        }

        private bool Run(SampleInfo info)
        {
            using var logger = CreateLogger(info);
            var environment = SampleEnvironmentFactory.Create(_options.ExecutionMode);
            var context = new SampleRuntimeContext(
                logger,
                environment,
                _options.HostKind,
                _config,
                _resources,
                _options.OutputDirectory,
                _options.HostCapabilities);

            try
            {
                var sample = info.Factory();
                if (sample is SampleBase sampleBase)
                {
                    sampleBase.Initialize(context);
                }
                else
                {
                    logger.Section($"Running: {info.Title}");
                    if (!string.IsNullOrWhiteSpace(info.Description))
                        logger.Info(info.Description);
                    logger.Line();
                }

                sample.Run();
                logger.Line();
                logger.Info("Sample completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                logger.Info(ex.StackTrace ?? string.Empty);
                return false;
            }
            finally
            {
                logger.Flush();
            }
        }

        private IDisposableLogger CreateLogger(SampleInfo info)
        {
            var loggers = new List<ILogger>();

            if (_options.WriteConsole)
                loggers.Add(new TextWriterSampleLogger(Console.Out));

            if (_options.WriteFile)
            {
                Directory.CreateDirectory(_options.OutputDirectory);
                var fileName = $"{info.Index:D2}-{SanitizeFileName(info.Title)}.log";
                var filePath = Path.Combine(_options.OutputDirectory, fileName);
                var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                loggers.Add(new TextWriterSampleLogger(writer, ownsWriter: true));
            }

            if (loggers.Count == 0)
                return new LoggerScope(NullSampleLogger.Instance);

            if (loggers.Count == 1)
                return new LoggerScope(loggers[0]);

            return new LoggerScope(new CompositeSampleLogger(loggers));
        }

        private void Register(SampleInfo info)
        {
            _samples.Add(info);

            if (!_grouped.TryGetValue(info.Category, out var categorySamples))
            {
                categorySamples = new List<SampleInfo>();
                _grouped[info.Category] = categorySamples;
            }

            categorySamples.Add(info);
        }

        private static ISample CreateSample(Type sampleType)
        {
            return (ISample)(Activator.CreateInstance(sampleType)
                ?? throw new InvalidOperationException($"Cannot create sample: {sampleType.FullName}"));
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
                builder.Append(invalid.Contains(ch) ? '_' : ch);
            return builder.ToString();
        }

        private interface IDisposableLogger : ILogger, IDisposable
        {
        }

        private sealed class LoggerScope : IDisposableLogger
        {
            private readonly ILogger _inner;

            public LoggerScope(ILogger inner)
            {
                _inner = inner;
            }

            public void Info(string message) => _inner.Info(message);
            public void Warn(string message) => _inner.Warn(message);
            public void Error(string message) => _inner.Error(message);
            public void Section(string title) => _inner.Section(title);
            public void Line() => _inner.Line();
            public void Divider() => _inner.Divider();
            public void Bullet(string text) => _inner.Bullet(text);
            public void Numbered(int num, string text) => _inner.Numbered(num, text);
            public void KeyValue(string key, string value) => _inner.KeyValue(key, value);
            public void Flush() => _inner.Flush();

            public void Dispose()
            {
                if (_inner is IDisposable disposable)
                    disposable.Dispose();
                else
                    _inner.Flush();
            }
        }
    }

    public sealed class SampleInfo
    {
        public int Index { get; init; }
        public string Id { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public SampleCategory Category { get; init; }
        public Type? SampleType { get; init; }
        public Func<ISample> Factory { get; init; } = () => throw new InvalidOperationException();
    }
}
