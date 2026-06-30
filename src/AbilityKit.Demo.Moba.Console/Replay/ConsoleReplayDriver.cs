using System;
using System.Collections.Generic;
using System.IO;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Replay
{
    /// <summary>
    /// 录像回放驱动接口
    /// </summary>
    public interface IReplayDriver : IDisposable
    {
        /// <summary>
        /// 是否正在播放
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 当前帧号
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// 总帧�?
        /// </summary>
        int TotalFrames { get; }

        /// <summary>
        /// 播放速度
        /// </summary>
        float PlaybackSpeed { get; set; }

        /// <summary>
        /// 开始播�?
        /// </summary>
        void Play();

        /// <summary>
        /// 暂停播放
        /// </summary>
        void Pause();

        /// <summary>
        /// 停止并重�?
        /// </summary>
        void Stop();

        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        void SeekToFrame(int frame);

        /// <summary>
        /// 获取当前帧的输入命令（返回null表示没有更多命令�?
        /// </summary>
        PlayerInputCommand? GetCommandAtFrame(int frame);

        /// <summary>
        /// 获取录像文件信息
        /// </summary>
        RecordFileHeader? GetHeader();
    }

    /// <summary>
    /// 控制台录像回放驱�?
    /// </summary>
    public sealed class ConsoleReplayDriver : IReplayDriver
    {
        private readonly FrameRecordFile _recordFile;
        private readonly Dictionary<int, List<PlayerInputCommand>> _commandsByFrame;
        private readonly string _filePath;
        private int _currentFrame;
        private bool _isPlaying;
        private bool _isPaused;
        private float _playbackSpeed = 1.0f;

        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;
        public int CurrentFrame => _currentFrame;
        public int TotalFrames => _recordFile?.Header.EndFrame ?? 0;

        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Math.Max(0.1f, Math.Min(10f, value));
        }

        public ConsoleReplayDriver(string filePath)
        {
            _filePath = filePath;
            _commandsByFrame = new Dictionary<int, List<PlayerInputCommand>>();
            _recordFile = LoadRecordFile(filePath);
            IndexCommands();
        }

        private FrameRecordFile LoadRecordFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Log.Error($"[Replay] Record file not found: {filePath}");
                return new FrameRecordFile();
            }

            try
            {
                using var stream = File.OpenRead(filePath);
                var file = FrameRecordFile.ReadFromStream(stream);
                Log.System($"[Replay] Loaded record: {file.Header.TotalCommands} commands, frames {file.Header.StartFrame}-{file.Header.EndFrame}");
                Log.System($"[Replay] Recorded: {file.Header.RecordTime:yyyy-MM-dd HH:mm:ss}, Mode: {file.Header.GameMode}");
                return file;
            }
            catch (Exception ex)
            {
                Log.Error($"[Replay] Failed to load record file: {ex.Message}");
                return new FrameRecordFile();
            }
        }

        private void IndexCommands()
        {
            foreach (var cmd in _recordFile.Commands)
            {
                if (!_commandsByFrame.ContainsKey(cmd.Frame))
                    _commandsByFrame[cmd.Frame] = new List<PlayerInputCommand>();
                _commandsByFrame[cmd.Frame].Add(cmd);
            }
        }

        public void Play()
        {
            if (_recordFile.Commands.Count == 0)
            {
                Log.Warn("[Replay] No commands to replay");
                return;
            }

            _isPlaying = true;
            _isPaused = false;
            Log.System("[Replay] Playing...");
        }

        public void Pause()
        {
            _isPaused = !_isPaused;
            Log.System(_isPaused ? "[Replay] Paused" : "[Replay] Resumed");
        }

        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            _currentFrame = _recordFile.Header.StartFrame;
            Log.System("[Replay] Stopped");
        }

        public void SeekToFrame(int frame)
        {
            if (_recordFile.Commands.Count == 0) return;

            _currentFrame = Math.Max(_recordFile.Header.StartFrame, 
                                    Math.Min(frame, _recordFile.Header.EndFrame));
            Log.System($"[Replay] Seeked to frame {_currentFrame}");
        }

        public PlayerInputCommand? GetCommandAtFrame(int frame)
        {
            if (!_commandsByFrame.TryGetValue(frame, out var commands) || commands.Count == 0)
                return null;

            // 返回该帧的第一个命�?
            return commands[0];
        }

        /// <summary>
        /// 获取指定帧的所有命�?
        /// </summary>
        public List<PlayerInputCommand> GetCommandsAtFrame(int frame)
        {
            if (!_commandsByFrame.TryGetValue(frame, out var commands))
                return new List<PlayerInputCommand>();
            return commands;
        }

        public RecordFileHeader? GetHeader() => _recordFile.Header;

        public void AdvanceFrame()
        {
            if (!_isPlaying || _isPaused) return;
            _currentFrame++;
        }

        public void Dispose()
        {
            _commandsByFrame.Clear();
        }
    }

    /// <summary>
    /// 录像文件管理�?
    /// </summary>
    public static class RecordFileManager
    {
        private static readonly List<string> _recordExtensions = new() { ".akrec" };

        /// <summary>
        /// 获取所有可用的录像文件
        /// </summary>
        public static List<string> GetAvailableRecords(string directory = "Records")
        {
            var records = new List<string>();

            if (!Directory.Exists(directory))
            {
                Log.System($"[Record] No records directory: {directory}");
                return records;
            }

            foreach (var ext in _recordExtensions)
            {
                foreach (var file in Directory.GetFiles(directory, $"*{ext}"))
                {
                    records.Add(file);
                }
            }

            records.Sort((a, b) => string.Compare(b, a, StringComparison.Ordinal)); // 最新的在前
            return records;
        }

        /// <summary>
        /// 列出所有录像文�?
        /// </summary>
        public static void ListRecords(string directory = "Records")
        {
            var records = GetAvailableRecords(directory);

            if (records.Count == 0)
            {
                Log.System("[Record] No record files found");
                return;
            }

            Log.System($"[Record] Found {records.Count} record file(s) in {directory}:");
            for (int i = 0; i < records.Count; i++)
            {
                var file = records[i];
                var info = new FileInfo(file);
                Log.System($"  [{i + 1}] {Path.GetFileName(file)} ({info.Length / 1024.0:F1} KB, {info.LastWriteTime:yyyy-MM-dd HH:mm})");
            }
        }

        /// <summary>
        /// 删除录像文件
        /// </summary>
        public static bool DeleteRecord(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Log.System($"[Record] Deleted: {filePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[Record] Failed to delete: {ex.Message}");
                return false;
            }
        }
    }
}
