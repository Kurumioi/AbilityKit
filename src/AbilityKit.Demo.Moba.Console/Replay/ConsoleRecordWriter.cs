using System;
using System.Collections.Generic;
using System.IO;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console.Replay
{
    /// <summary>
    /// 输入录制写入器接�?
    /// </summary>
    public interface IInputRecordWriter : IDisposable
    {
        /// <summary>
        /// 追加输入命令
        /// </summary>
        void Append(in PlayerInputCommand command);

        /// <summary>
        /// 写入帧快�?
        /// </summary>
        void WriteSnapshot(in FrameSnapshot snapshot);

        /// <summary>
        /// 关闭并保存文�?
        /// </summary>
        void Close();

        /// <summary>
        /// 当前帧号
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// 是否已关�?
        /// </summary>
        bool IsClosed { get; }
    }

    /// <summary>
    /// 控制台录制写入器
    /// </summary>
    public sealed class ConsoleRecordWriter : IInputRecordWriter
    {
        private readonly string _filePath;
        private readonly List<PlayerInputCommand> _commands = new();
        private readonly List<FrameSnapshot> _snapshots = new();
        private readonly int _snapshotInterval;
        private readonly RecordFileHeader _header;
        private readonly object _lock = new();
        private bool _isClosed;
        private int _currentFrame;

        public int CurrentFrame => _currentFrame;
        public bool IsClosed => _isClosed;

        public ConsoleRecordWriter(string outputPath, int snapshotInterval = 300)
        {
            _snapshotInterval = snapshotInterval;
            _header = new RecordFileHeader
            {
                RecordTime = DateTime.Now,
                GameMode = "MobaConsole",
                PlayerName = Environment.UserName,
                MapName = "default"
            };

            // 生成文件�?
            var dir = Path.GetDirectoryName(outputPath) ?? ".";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _filePath = Path.Combine(dir, $"replay_{timestamp}.akrec");

            Log.System($"[Record] Recording to: {_filePath}");
        }

        public void Append(in PlayerInputCommand command)
        {
            lock (_lock)
            {
                if (_isClosed) return;

                _commands.Add(command);
                _currentFrame = command.Frame;

                // 定期写入快照
                if (_snapshotInterval > 0 && _currentFrame % _snapshotInterval == 0)
                {
                    // 快照将在Flush时基于当前状态生�?
                }
            }
        }

        public void WriteSnapshot(in FrameSnapshot snapshot)
        {
            lock (_lock)
            {
                if (_isClosed) return;
                _snapshots.Add(snapshot);
            }
        }

        /// <summary>
        /// 添加一个帧快照（基于当前状态）
        /// </summary>
        public void AddSnapshot(int frame, int actorCount)
        {
            var hash = ComputeSimpleHash(frame, actorCount);
            WriteSnapshot(new FrameSnapshot(frame, actorCount, hash));
        }

        public void Close()
        {
            lock (_lock)
            {
                if (_isClosed) return;
                _isClosed = true;

                try
                {
                    _header.StartFrame = _commands.Count > 0 ? _commands[0].Frame : 0;
                    _header.EndFrame = _commands.Count > 0 ? _commands[^1].Frame : 0;
                    _header.TotalCommands = _commands.Count;

                    var file = new FrameRecordFile
                    {
                        Header = _header
                    };
                    file.Commands.AddRange(_commands);
                    file.Snapshots.AddRange(_snapshots);

                    using var stream = File.Create(_filePath);
                    file.WriteToStream(stream);

                    Log.System($"[Record] Saved {_commands.Count} commands, {_snapshots.Count} snapshots");
                    Log.System($"[Record] Record file: {_filePath}");
                }
                catch (Exception ex)
                {
                    Log.Error($"[Record] Failed to save record: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            Close();
        }

        private static uint ComputeSimpleHash(int frame, int actorCount)
        {
            // 简单的哈希函数用于状态校�?
            unchecked
            {
                uint hash = 17;
                hash = hash * 31 + (uint)frame;
                hash = hash * 31 + (uint)actorCount;
                return hash;
            }
        }
    }

    /// <summary>
    /// 空写入器（不录制�?
    /// </summary>
    public sealed class NullRecordWriter : IInputRecordWriter
    {
        public int CurrentFrame { get; private set; }
        public bool IsClosed { get; private set; }

        public void Append(in PlayerInputCommand command)
        {
            CurrentFrame = command.Frame;
        }

        public void WriteSnapshot(in FrameSnapshot snapshot) { }
        public void Close() => IsClosed = true;
        public void Dispose() => Close();
    }
}
