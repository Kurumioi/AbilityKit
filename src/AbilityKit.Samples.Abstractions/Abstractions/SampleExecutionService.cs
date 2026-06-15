using System;
using System.Collections.Generic;

namespace AbilityKit.Samples.Abstractions
{
    /// <summary>
    /// Runs catalog entries for hosts that manage samples through buttons, lists, or menus.
    /// </summary>
    public sealed class SampleExecutionService
    {
        private readonly SampleCatalog _catalog;
        private readonly Func<ExecutionMode, ISampleEnvironment> _environmentFactory;
        private readonly IConfigProvider? _config;
        private readonly IResourceProvider? _resources;

        /// <summary>
        /// Creates an execution service.
        /// </summary>
        public SampleExecutionService(
            SampleCatalog catalog,
            Func<ExecutionMode, ISampleEnvironment> environmentFactory,
            IConfigProvider? config = null,
            IResourceProvider? resources = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _environmentFactory = environmentFactory ?? throw new ArgumentNullException(nameof(environmentFactory));
            _config = config;
            _resources = resources;
        }

        /// <summary>
        /// Runs samples from a host-neutral request.
        /// </summary>
        public IReadOnlyList<SampleExecutionResult> Run(SampleRunRequest request, ILogger output)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            var results = new List<SampleExecutionResult>();
            switch (request.SelectionKind)
            {
                case SampleRunSelectionKind.Index:
                    if (!request.Index.HasValue)
                        throw new ArgumentException("Index request must include an index.", nameof(request));
                    results.Add(RunByIndex(request.Index.Value, output, request.Options));
                    break;
                case SampleRunSelectionKind.Id:
                    results.Add(RunById(request.Id, output, request.Options));
                    break;
                case SampleRunSelectionKind.All:
                    foreach (var entry in _catalog.Entries)
                    {
                        results.Add(Run(entry, output, request.Options));
                    }
                    break;
                default:
                    throw new ArgumentException("Sample run request did not select a sample.", nameof(request));
            }

            return results;
        }

        /// <summary>
        /// Runs a sample by display index.
        /// </summary>
        public SampleExecutionResult RunByIndex(int index, ILogger output, SampleRunOptions? options = null)
        {
            if (!_catalog.TryGetByIndex(index, out var entry))
                throw new ArgumentOutOfRangeException(nameof(index), $"Sample index not found: {index}");

            return Run(entry, output, options);
        }

        /// <summary>
        /// Runs a sample by stable id.
        /// </summary>
        public SampleExecutionResult RunById(string id, ILogger output, SampleRunOptions? options = null)
        {
            if (!_catalog.TryGetById(id, out var entry))
                throw new ArgumentException($"Sample id not found: {id}", nameof(id));

            return Run(entry, output, options);
        }

        /// <summary>
        /// Starts a sample by stable id and returns a host-driven run handle.
        /// </summary>
        public SampleRunHandle StartById(string id, ILogger output, SampleRunOptions? options = null)
        {
            if (!_catalog.TryGetById(id, out var entry))
                throw new ArgumentException($"Sample id not found: {id}", nameof(id));

            return Start(entry, output, options);
        }

        /// <summary>
        /// Starts a sample by display index and returns a host-driven run handle.
        /// </summary>
        public SampleRunHandle StartByIndex(int index, ILogger output, SampleRunOptions? options = null)
        {
            if (!_catalog.TryGetByIndex(index, out var entry))
                throw new ArgumentOutOfRangeException(nameof(index), $"Sample index not found: {index}");

            return Start(entry, output, options);
        }

        /// <summary>
        /// Runs a catalog entry with host-provided output and options.
        /// </summary>
        public SampleExecutionResult Run(SampleCatalogEntry entry, ILogger output, SampleRunOptions? options = null)
        {
            using var handle = Start(entry, output, options);
            return handle.Result ?? new SampleExecutionResult(entry, succeeded: true);
        }

        /// <summary>
        /// Starts a catalog entry and keeps its environment available for host-driven ticks.
        /// </summary>
        public SampleRunHandle Start(SampleCatalogEntry entry, ILogger output, SampleRunOptions? options = null)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            var runOptions = options ?? new SampleRunOptions();
            var environment = _environmentFactory(runOptions.ExecutionMode);
            var context = new SampleRuntimeContext(
                output,
                environment,
                runOptions.HostKind,
                _config,
                _resources,
                runOptions.OutputDirectory,
                runOptions.HostCapabilities,
                runOptions.Inputs);

            try
            {
                var sample = entry.CreateSample();
                if (sample is SampleBase sampleBase)
                {
                    sampleBase.Initialize(context);
                }

                sample.Run();
                output.Flush();
                return new SampleRunHandle(entry, sample, environment, output, new SampleExecutionResult(entry, succeeded: true));
            }
            catch (Exception ex)
            {
                output.Error(ex.Message);
                output.Flush();
                return new SampleRunHandle(entry, null, environment, output, new SampleExecutionResult(entry, succeeded: false, ex.Message, ex));
            }
        }
    }
}
