using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using AbilityKit.Protocol.Moba.Generated.Gateway;
using AbilityKit.Protocol.Moba.Generated.GatewayFrameSync;
using FrameInputItem = AbilityKit.Protocol.Moba.Generated.GatewayFrameSync.WireInputItem;
using FramePushed = AbilityKit.Protocol.Moba.Generated.GatewayFrameSync.WireFramePushedPush;

namespace AbilityKit.Game.Battle.Transport.Moba
{
    /// <summary>
    /// 网络协议编解码器
    /// 封装与服务器的通信协议
    /// 使用 MemoryPack 进行高效二进制序列化
    /// 
    /// 协议类型统一从 AbilityKit.Protocol.Moba 引用：
    /// - WireGuestLoginReq/Res
    /// - WireCreateRoomReq/Res
    /// - WireJoinRoomReq/Res
    /// - WireFramePushedPush (帧推送)
    /// - WireInputItem (帧输入项)
    /// </summary>
    public static class NetworkProtocol
    {
        // ========== 登录协议 ==========

        public static byte[] EncodeGuestLoginReq(string guestId)
        {
            var request = new WireGuestLoginReq { GuestId = guestId ?? string.Empty };
            return MemoryPackSerializer.Serialize(request);
        }

        public static GuestLoginResponse DecodeGuestLoginResp(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new GuestLoginResponse { Success = false, Message = "Empty payload" };
            }

            try
            {
                var response = MemoryPackSerializer.Deserialize<WireGuestLoginRes>(payload);
                return new GuestLoginResponse
                {
                    Success = !string.IsNullOrEmpty(response.SessionToken),
                    SessionToken = response.SessionToken,
                    AccountId = response.AccountId,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new GuestLoginResponse { Success = false, Message = $"Deserialize error: {ex.Message}" };
            }
        }

        // ========== 房间协议 ==========

        public static byte[] EncodeCreateRoomReq(string sessionToken, string region = "dev", string serverId = "default",
            string roomType = "moba", string title = "Test Room", bool isPublic = true, int maxPlayers = 10)
        {
            var request = new WireCreateRoomReq
            {
                SessionToken = sessionToken,
                Region = region,
                ServerId = serverId,
                RoomType = roomType,
                Title = title,
                IsPublic = isPublic,
                MaxPlayers = maxPlayers
            };
            return MemoryPackSerializer.Serialize(request);
        }

        public static CreateRoomResponse DecodeCreateRoomResp(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new CreateRoomResponse { Success = false, Message = "Empty payload" };
            }

            try
            {
                var response = MemoryPackSerializer.Deserialize<WireCreateRoomRes>(payload);
                return new CreateRoomResponse
                {
                    Success = !string.IsNullOrEmpty(response.RoomId),
                    RoomId = response.RoomId,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new CreateRoomResponse { Success = false, Message = $"Deserialize error: {ex.Message}" };
            }
        }

        public static byte[] EncodeJoinRoomReq(string sessionToken, string region = "dev",
            string serverId = "default", string roomId = "")
        {
            var request = new WireJoinRoomReq
            {
                SessionToken = sessionToken,
                Region = region,
                ServerId = serverId,
                RoomId = roomId
            };
            return MemoryPackSerializer.Serialize(request);
        }

        public static JoinRoomResponse DecodeJoinRoomResp(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new JoinRoomResponse { Success = false, Message = "Empty payload" };
            }

            try
            {
                var response = MemoryPackSerializer.Deserialize<WireJoinRoomRes>(payload);
                return new JoinRoomResponse
                {
                    Success = response.NumericRoomId > 0 || !string.IsNullOrEmpty(response.RoomId),
                    RoomId = response.RoomId,
                    NumericRoomId = response.NumericRoomId,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new JoinRoomResponse { Success = false, Message = $"Deserialize error: {ex.Message}" };
            }
        }

        // ========== 帧输入协议 ==========

        /// <summary>
        /// 帧推送数据（返回对象）
        /// </summary>
        public sealed class FramePushedData
        {
            public ulong RoomId { get; init; }
            public ulong WorldId { get; init; }
            public int Frame { get; init; }
            public List<FrameInputData> Inputs { get; init; } = new();
        }

        /// <summary>
        /// 帧输入数据
        /// </summary>
        public sealed class FrameInputData
        {
            public uint PlayerId { get; init; }
            public int OpCode { get; init; }
            public byte[] Payload { get; init; }
        }

        /// <summary>
        /// 编码帧输入提交
        /// </summary>
        public static byte[] EncodeSubmitFrameInput(ulong roomId, ulong worldId, int frame,
            uint playerId, int inputOpCode, byte[] inputPayload = null)
        {
            var wire = new WireSubmitFrameInputReq(
                roomId,
                worldId,
                playerId,
                frame,
                inputOpCode,
                inputPayload);

            return MemoryPackSerializer.Serialize(wire);
        }

        /// <summary>
        /// 解码帧推送数据
        /// </summary>
        public static FramePushedData DecodeFramePushed(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new FramePushedData();
            }

            try
            {
                var wire = MemoryPackSerializer.Deserialize<FramePushed>(payload);
                return new FramePushedData
                {
                    RoomId = wire.RoomId,
                    WorldId = wire.WorldId,
                    Frame = wire.Frame,
                    Inputs = wire.Inputs?.Select(i => new FrameInputData
                    {
                        PlayerId = i.PlayerId,
                        OpCode = i.OpCode,
                        Payload = i.Payload
                    }).ToList() ?? new List<FrameInputData>()
                };
            }
            catch
            {
                return new FramePushedData();
            }
        }

        /// <summary>
        /// 解码帧推送数据。
        /// </summary>
        public static void DecodeFramePushed(byte[] payload, out int frame, out List<Tuple<uint, int, byte[]>> inputs)
        {
            frame = 0;
            inputs = new List<Tuple<uint, int, byte[]>>();

            if (payload == null || payload.Length == 0) return;

            var wire = MemoryPackSerializer.Deserialize<FramePushed>(payload);
            frame = wire.Frame;
            if (wire.Inputs == null) return;

            foreach (var input in wire.Inputs)
            {
                inputs.Add(new Tuple<uint, int, byte[]>(input.PlayerId, input.OpCode, input.Payload));
            }
        }
    }
}
