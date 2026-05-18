using System;
using System.IO;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Replay
{
    /// <summary>
    /// Console 版本的回放录制器
    /// 封装 Share 层的 ReplayRecorder，提供文件持久化功能
    /// 
    /// 职责边界：
    /// - ✅ 使用 Share 的 ReplayRecorder
    /// - ✅ 管理回放文件的读写
    /// - ✅ 提供录制控制（开始/停止/暂停）
    /// - ❌ 不做数值计算
    /// - ❌ 不直接处理帧数据
    /// </summary>
    public sealed class ShareReplayRecorder : IDisposable
    {
        private readonly ReplayRecorder _recorder;
        private string _currentReplayId;
        private string _outputPath;
        private bool _isRecording;
        private bool _disposed;
        private int _snapshotIntervalFrames;
        private int _framesSinceLastSnapshot;

        public bool IsRecording => _isRecording;
        public string CurrentReplayId => _currentReplayId;
        public ReplayRecorder Recorder => _recorder;

        public ShareReplayRecorder()
        {
            _recorder = new ReplayRecorder();
            _currentReplayId = string.Empty;
            _outputPath = "Records";
            _isRecording = false;
            _snapshotIntervalFrames = 30; // 每 30 帧记录一次快照
            _framesSinceLastSnapshot = 0;
        }

        /// <summary>
        /// 设置输出路径
        /// </summary>
        public void SetOutputPath(string path)
        {
            _outputPath = path ?? "Records";
        }

        /// <summary>
        /// 设置快照间隔
        /// </summary>
        public void SetSnapshotInterval(int frames)
        {
            _snapshotIntervalFrames = Math.Max(1, frames);
        }

        /// <summary>
        /// 开始录制
        /// </summary>
        public bool StartRecording(string replayId = null)
        {
            if (_isRecording) return false;

            _currentReplayId = replayId ?? GenerateReplayId();
            _recorder.StartRecording(_currentReplayId);
            _isRecording = true;
            _framesSinceLastSnapshot = 0;

            EnsureOutputDirectory();

            Log.System($"[ShareReplay] Recording started: {_currentReplayId}");
            return true;
        }

        /// <summary>
        /// 停止录制并保存文件
        /// </summary>
        public string StopRecording()
        {
            if (!_isRecording) return string.Empty;

            _isRecording = false;
            _recorder.StopRecording();

            var data = _recorder.StopRecordingAndGetData();
            if (data == null || data.Length == 0)
            {
                Log.Warn("[ShareReplay] No data recorded");
                return string.Empty;
            }

            var filePath = GetOutputFilePath();
            try
            {
                File.WriteAllBytes(filePath, data);
                Log.System($"[ShareReplay] Saved to: {filePath} ({data.Length} bytes)");
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error($"[ShareReplay] Failed to save: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 记录快照
        /// </summary>
        public void RecordSnapshot(int frameIndex, byte[] snapshotData)
        {
            if (!_isRecording) return;

            _recorder.RecordSnapshot(frameIndex, snapshotData);
            _framesSinceLastSnapshot = 0;
        }

        /// <summary>
        /// 记录输入
        /// </summary>
        public void RecordInput(int frameIndex, int playerId, byte[] inputData)
        {
            if (!_isRecording) return;

            _recorder.RecordInput(frameIndex, playerId, inputData);
        }

        /// <summary>
        /// 检查是否应该记录快照（基于间隔）
        /// </summary>
        public bool ShouldRecordSnapshot()
        {
            if (!_isRecording) return false;

            _framesSinceLastSnapshot++;
            return _framesSinceLastSnapshot >= _snapshotIntervalFrames;
        }

        /// <summary>
        /// 暂停录制
        /// </summary>
        public void Pause()
        {
            if (!_isRecording) return;
            _recorder.StopRecording();
            Log.System("[ShareReplay] Recording paused");
        }

        /// <summary>
        /// 恢复录制
        /// </summary>
        public void Resume()
        {
            if (_isRecording) return;
            if (string.IsNullOrEmpty(_currentReplayId)) return;

            _recorder.StartRecording(_currentReplayId);
            _isRecording = true;
            Log.System("[ShareReplay] Recording resumed");
        }

        /// <summary>
        /// 获取录制统计
        /// </summary>
        public ReplayStats GetStats()
        {
            return new ReplayStats
            {
                ReplayId = _currentReplayId,
                IsRecording = _isRecording,
                SnapshotCount = _recorder.SnapshotCount,
                InputCount = _recorder.InputCount
            };
        }

        private string GenerateReplayId()
        {
            return $"replay_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        private void EnsureOutputDirectory()
        {
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }
        }

        private string GetOutputFilePath()
        {
            return Path.Combine(_outputPath, $"{_currentReplayId}.replay");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_isRecording)
            {
                StopRecording();
            }

            _recorder.Dispose();
            Log.Trace("[ShareReplay] Disposed");
        }
    }

    /// <summary>
    /// 回放统计信息
    /// </summary>
    public struct ReplayStats
    {
        public string ReplayId { get; set; }
        public bool IsRecording { get; set; }
        public int SnapshotCount { get; set; }
        public int InputCount { get; set; }
    }

    /// <summary>
    /// Console 版本的回放播放器
    /// 封装 Share 层的 ReplayPlayer
    /// </summary>
    public sealed class ShareReplayPlayer : IDisposable
    {
        private readonly ReplayPlayer _player;
        private string _loadedReplayId;
        private bool _disposed;
        private bool _isPaused;

        public bool IsPlaying => _player.IsPlaying;
        public bool IsPaused => _isPaused;
        public int CurrentFrame => _player.CurrentFrame;
        public int TotalFrames => _player.TotalFrames;
        public float PlaybackSpeed => _player.PlaybackSpeed;
        public ReplayPlayer.ReplayMetadata Metadata => _player.Metadata;

        public ShareReplayPlayer()
        {
            _player = new ReplayPlayer();
            _loadedReplayId = string.Empty;
            _isPaused = false;
        }

        /// <summary>
        /// 加载回放文件
        /// </summary>
        public bool LoadReplay(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Log.Error($"[ShareReplay] File not found: {filePath}");
                return false;
            }

            try
            {
                var data = File.ReadAllBytes(filePath);
                _player.LoadReplay(data);
                _loadedReplayId = Path.GetFileNameWithoutExtension(filePath);
                Log.System($"[ShareReplay] Loaded: {_loadedReplayId} ({data.Length} bytes)");
                Log.System($"[ShareReplay]   Frames: {TotalFrames}, StartFrame: {Metadata.StartFrame}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[ShareReplay] Failed to load: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 开始播放
        /// </summary>
        public void Play()
        {
            _player.Play();
            _isPaused = false;
            Log.System("[ShareReplay] Playing");
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            _player.Pause();
            _isPaused = true;
            Log.System("[ShareReplay] Paused");
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            _player.Stop();
            _isPaused = false;
            Log.System("[ShareReplay] Stopped");
        }

        /// <summary>
        /// 定位到指定帧
        /// </summary>
        public void SeekToFrame(int frameIndex)
        {
            _player.SeekToFrame(frameIndex);
            Log.System($"[ShareReplay] Seeked to frame {frameIndex}");
        }

        /// <summary>
        /// 设置播放速度
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            _player.SetPlaybackSpeed(speed);
            Log.System($"[ShareReplay] Speed set to {speed}x");
        }

        /// <summary>
        /// 获取当前帧的快照数据
        /// </summary>
        public bool TryGetCurrentSnapshot(out int frameIndex, out byte[] snapshotData)
        {
            return _player.TryGetCurrentSnapshot(out frameIndex, out snapshotData);
        }

        /// <summary>
        /// 前进到下一帧
        /// </summary>
        public bool AdvanceFrame()
        {
            return _player.AdvanceFrame();
        }

        /// <summary>
        /// 列出可用的回放文件
        /// </summary>
        public static string[] ListReplayFiles(string directory = "Records")
        {
            if (!Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(directory, "*.replay");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _player.Dispose();
            Log.Trace("[ShareReplayPlayer] Disposed");
        }
    }
}
