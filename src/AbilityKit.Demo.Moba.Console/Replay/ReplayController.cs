using System;
using System.IO;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Replay
{
    /// <summary>
    /// 录像回放控制器
    /// 管理录制和回放状态的切换
    /// </summary>
    public sealed class ReplayController : IDisposable
    {
        private readonly RecordConfig _config;
        private IInputRecordWriter? _recordWriter;
        private IReplayDriver? _replayDriver;
        private RecordMode _mode;
        private int _snapshotCounter;

        public RecordMode Mode => _mode;
        public IInputRecordWriter? Writer => _recordWriter;
        public IReplayDriver? Driver => _replayDriver;
        public bool IsRecording => _mode == RecordMode.Recording;
        public bool IsReplaying => _mode == RecordMode.Replaying;

        public ReplayController(RecordConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _mode = config.Mode;
        }

        /// <summary>
        /// 初始化录制
        /// </summary>
        public void StartRecording()
        {
            if (_mode == RecordMode.Recording) return;

            // 确保输出目录存在
            var dir = Path.GetDirectoryName(_config.OutputPath) ?? "Records";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _recordWriter = new ConsoleRecordWriter(_config.OutputPath, _config.SnapshotIntervalFrames);
            _mode = RecordMode.Recording;
            _snapshotCounter = 0;

            Log.System("[Record] Recording started");
        }

        /// <summary>
        /// 停止录制
        /// </summary>
        public void StopRecording()
        {
            if (_mode != RecordMode.Recording) return;

            _recordWriter?.Close();
            _recordWriter?.Dispose();
            _recordWriter = null;
            _mode = RecordMode.None;

            Log.System("[Record] Recording stopped");
        }

        /// <summary>
        /// 初始化回放
        /// </summary>
        public bool StartReplay(string? filePath = null)
        {
            if (_mode == RecordMode.Replaying) return false;

            var path = filePath ?? _config.InputFilePath;
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("[Replay] No replay file specified");
                return false;
            }

            if (!File.Exists(path))
            {
                Log.Error($"[Replay] File not found: {path}");
                return false;
            }

            try
            {
                _replayDriver = new ConsoleReplayDriver(path);
                _mode = RecordMode.Replaying;

                var header = _replayDriver.GetHeader();
                if (header != null)
                {
                    Log.System($"[Replay] Loaded: {header.GameMode}, {header.TotalCommands} commands, {header.RecordTime:yyyy-MM-dd HH:mm:ss}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[Replay] Failed to start replay: {ex.Message}");
                _mode = RecordMode.None;
                return false;
            }
        }

        /// <summary>
        /// 停止回放
        /// </summary>
        public void StopReplay()
        {
            if (_mode != RecordMode.Replaying) return;

            _replayDriver?.Stop();
            _replayDriver?.Dispose();
            _replayDriver = null;
            _mode = RecordMode.None;

            Log.System("[Replay] Stopped");
        }

        /// <summary>
        /// 记录输入命令
        /// </summary>
        public void RecordCommand(int actorId, int frame, InputCommandType type, byte opCode, byte[] payload)
        {
            if (!IsRecording) return;

            var cmd = new PlayerInputCommand(actorId, frame, type, opCode, payload);
            _recordWriter?.Append(in cmd);

            // 定期添加快照
            _snapshotCounter++;
            if (_snapshotCounter >= _config.SnapshotIntervalFrames)
            {
                _snapshotCounter = 0;
                // 快照需要从外部传入，这里只记录计数器
            }
        }

        /// <summary>
        /// 添加帧快照
        /// </summary>
        public void AddSnapshot(int frame, int actorCount)
        {
            if (!IsRecording) return;
            (_recordWriter as ConsoleRecordWriter)?.AddSnapshot(frame, actorCount);
        }

        /// <summary>
        /// 获取回放驱动
        /// </summary>
        public T? GetReplayDriver<T>() where T : class, IReplayDriver
        {
            return _replayDriver as T;
        }

        /// <summary>
        /// 打印帮助信息
        /// </summary>
        public static void PrintHelp()
        {
            Log.System("");
            Log.System("=== Replay Controls ===");
            Log.System("  R - Start Recording");
            Log.System("  S - Stop Recording");
            Log.System("  P - Play/Pause Replay");
            Log.System("  L - List Records");
            Log.System("  V - View Record Info");
            Log.System("======================");
        }

        public void Dispose()
        {
            StopRecording();
            StopReplay();
        }
    }
}
