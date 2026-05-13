using System;
using System.IO;

namespace AbilityKit.Demo.Moba.Console.Replay
{
    /// <summary>
    /// 输入命令类型
    /// </summary>
    public enum InputCommandType : byte
    {
        Move = 1,
        SkillPress = 2,
        SkillAim = 3,
        SkillRelease = 4,
        Ping = 5
    }

    /// <summary>
    /// 玩家输入命令
    /// </summary>
    public readonly struct PlayerInputCommand
    {
        public int ActorId { get; }
        public int Frame { get; }
        public InputCommandType Type { get; }
        public byte OpCode { get; }
        public byte[] Payload { get; }

        public PlayerInputCommand(int actorId, int frame, InputCommandType type, byte opCode, byte[] payload)
        {
            ActorId = actorId;
            Frame = frame;
            Type = type;
            OpCode = opCode;
            Payload = payload ?? Array.Empty<byte>();
        }

        public static PlayerInputCommand Read(BinaryReader reader)
        {
            var actorId = reader.ReadInt32();
            var frame = reader.ReadInt32();
            var type = (InputCommandType)reader.ReadByte();
            var opcode = reader.ReadByte();
            var payloadLen = reader.ReadInt32();
            var payload = payloadLen > 0 ? reader.ReadBytes(payloadLen) : Array.Empty<byte>();
            return new PlayerInputCommand(actorId, frame, type, opcode, payload);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(ActorId);
            writer.Write(Frame);
            writer.Write((byte)Type);
            writer.Write(OpCode);
            writer.Write(Payload.Length);
            if (Payload.Length > 0)
            {
                writer.Write(Payload);
            }
        }

        public override string ToString()
        {
            return $"[Cmd] Actor:{ActorId} Frame:{Frame} Type:{Type} Op:{OpCode}";
        }
    }

    /// <summary>
    /// 帧状态快照（用于校验）
    /// </summary>
    public readonly struct FrameSnapshot
    {
        public int Frame { get; }
        public int ActorCount { get; }
        public uint StateHash { get; }

        public FrameSnapshot(int frame, int actorCount, uint stateHash)
        {
            Frame = frame;
            ActorCount = actorCount;
            StateHash = stateHash;
        }

        public static FrameSnapshot Read(BinaryReader reader)
        {
            return new FrameSnapshot(reader.ReadInt32(), reader.ReadInt32(), reader.ReadUInt32());
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Frame);
            writer.Write(ActorCount);
            writer.Write(StateHash);
        }
    }

    /// <summary>
    /// 录像文件头
    /// </summary>
    public sealed class RecordFileHeader
    {
        public const int MAGIC = 0x414B5243; // "AKRC" - AbilityKit Record
        public const int VERSION = 1;

        public int Magic { get; set; } = MAGIC;
        public int Version { get; set; } = VERSION;
        public DateTime RecordTime { get; set; } = DateTime.Now;
        public int StartFrame { get; set; }
        public int EndFrame { get; set; }
        public int TotalCommands { get; set; }
        public string MapName { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public string GameMode { get; set; } = "Moba";
        public byte[] Metadata { get; set; } = Array.Empty<byte>();

        public void Write(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(RecordTime.Ticks);
            writer.Write(StartFrame);
            writer.Write(EndFrame);
            writer.Write(TotalCommands);
            writer.Write(MapName ?? "");
            writer.Write(PlayerName ?? "");
            writer.Write(GameMode ?? "");
            writer.Write(Metadata.Length);
            if (Metadata.Length > 0) writer.Write(Metadata);
        }

        public static RecordFileHeader Read(BinaryReader reader)
        {
            var magic = reader.ReadInt32();
            if (magic != MAGIC)
                throw new InvalidDataException($"Invalid record file magic: 0x{magic:X8}");
            
            var version = reader.ReadInt32();
            if (version != VERSION)
                throw new InvalidDataException($"Unsupported record version: {version}");

            return new RecordFileHeader
            {
                Magic = magic,
                Version = version,
                RecordTime = new DateTime(reader.ReadInt64()),
                StartFrame = reader.ReadInt32(),
                EndFrame = reader.ReadInt32(),
                TotalCommands = reader.ReadInt32(),
                MapName = reader.ReadString(),
                PlayerName = reader.ReadString(),
                GameMode = reader.ReadString(),
                Metadata = reader.ReadBytes(reader.ReadInt32())
            };
        }
    }

    /// <summary>
    /// 录像文件格式
    /// </summary>
    public sealed class LockstepInputRecordFile
    {
        public RecordFileHeader Header { get; set; } = new();
        public System.Collections.Generic.List<PlayerInputCommand> Commands { get; } = new();
        public System.Collections.Generic.List<FrameSnapshot> Snapshots { get; } = new();

        public void WriteToStream(Stream stream)
        {
            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            Header.TotalCommands = Commands.Count;
            Header.EndFrame = Commands.Count > 0 ? Commands[^1].Frame : 0;
            Header.Write(writer);

            writer.Write(Commands.Count);
            foreach (var cmd in Commands) cmd.Write(writer);

            writer.Write(Snapshots.Count);
            foreach (var snap in Snapshots) snap.Write(writer);
        }

        public static LockstepInputRecordFile ReadFromStream(Stream stream)
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            var header = RecordFileHeader.Read(reader);

            var file = new LockstepInputRecordFile { Header = header };

            var cmdCount = reader.ReadInt32();
            for (int i = 0; i < cmdCount; i++)
                file.Commands.Add(PlayerInputCommand.Read(reader));

            var snapCount = reader.ReadInt32();
            for (int i = 0; i < snapCount; i++)
                file.Snapshots.Add(FrameSnapshot.Read(reader));

            return file;
        }
    }

    /// <summary>
    /// 录制模式
    /// </summary>
    public enum RecordMode
    {
        None,
        Recording,
        Replaying
    }

    /// <summary>
    /// 录制配置
    /// </summary>
    public sealed class RecordConfig
    {
        public RecordMode Mode { get; set; } = RecordMode.None;
        public string OutputPath { get; set; } = "Records";
        public string InputFilePath { get; set; } = "";
        public bool AutoRecord { get; set; } = false;
        public int SnapshotIntervalFrames { get; set; } = 300; // 每300帧保存一次状态快照
    }
}
