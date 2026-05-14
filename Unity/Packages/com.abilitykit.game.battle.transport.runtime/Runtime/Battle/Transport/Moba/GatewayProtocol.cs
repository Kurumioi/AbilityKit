using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AbilityKit.Game.Battle.Transport.Moba
{
    /// <summary>
    /// Gateway 协议编解码器
    /// 封装与 Server/Orleans Gateway 的通信协议
    /// </summary>
    public static class GatewayProtocol
    {
        public static byte[] EncodeGuestLoginReq(string guestId = null)
        {
            if (string.IsNullOrEmpty(guestId))
            {
                return new byte[0];
            }

            var sb = new StringBuilder();
            sb.Append("{\"guestId\":\"");
            sb.Append(guestId);
            sb.Append("\"}");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static GuestLoginResponse DecodeGuestLoginResp(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new GuestLoginResponse { Success = false, Message = "Empty payload" };
            }

            try
            {
                var json = Encoding.UTF8.GetString(payload);
                var response = new GuestLoginResponse();

                int tokenStart = json.IndexOf("\"sessionToken\"");
                if (tokenStart >= 0)
                {
                    int colon = json.IndexOf(':', tokenStart);
                    int quote1 = json.IndexOf('"', colon);
                    int quote2 = json.IndexOf('"', quote1 + 1);
                    if (colon > 0 && quote1 >= 0 && quote2 >= 0)
                    {
                        response.SessionToken = json.Substring(quote1 + 1, quote2 - quote1 - 1);
                        response.Success = !string.IsNullOrEmpty(response.SessionToken);
                    }
                }

                int accountStart = json.IndexOf("\"accountId\"");
                if (accountStart >= 0)
                {
                    int colon = json.IndexOf(':', accountStart);
                    int quote1 = json.IndexOf('"', colon);
                    int quote2 = json.IndexOf('"', quote1 + 1);
                    if (colon > 0 && quote1 >= 0 && quote2 >= 0)
                    {
                        response.AccountId = json.Substring(quote1 + 1, quote2 - quote1 - 1);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                return new GuestLoginResponse { Success = false, Message = ex.Message };
            }
        }

        public static byte[] EncodeCreateRoomReq(string sessionToken, string region = "dev", string serverId = "default",
            string roomType = "moba", string title = "Test Room", bool isPublic = true, int maxPlayers = 10)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"sessionToken\":\"").Append(sessionToken).Append("\",");
            sb.Append("\"region\":\"").Append(region).Append("\",");
            sb.Append("\"serverId\":\"").Append(serverId).Append("\",");
            sb.Append("\"roomType\":\"").Append(roomType).Append("\",");
            sb.Append("\"title\":\"").Append(title).Append("\",");
            sb.Append("\"isPublic\":").Append(isPublic.ToString().ToLower()).Append(",");
            sb.Append("\"maxPlayers\":").Append(maxPlayers);
            sb.Append("}");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static CreateRoomResponse DecodeCreateRoomResp(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new CreateRoomResponse { Success = false, Message = "Empty payload" };
            }

            try
            {
                var json = Encoding.UTF8.GetString(payload);
                var response = new CreateRoomResponse();

                int roomIdStart = json.IndexOf("\"roomId\"");
                if (roomIdStart >= 0)
                {
                    int colon = json.IndexOf(':', roomIdStart);
                    int quote1 = json.IndexOf('"', colon);
                    int quote2 = json.IndexOf('"', quote1 + 1);
                    if (colon > 0 && quote1 >= 0 && quote2 >= 0)
                    {
                        response.RoomId = json.Substring(quote1 + 1, quote2 - quote1 - 1);
                        response.Success = !string.IsNullOrEmpty(response.RoomId);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                return new CreateRoomResponse { Success = false, Message = ex.Message };
            }
        }

        public static byte[] EncodeJoinRoomReq(string sessionToken, string region = "dev",
            string serverId = "default", string roomId = "")
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"sessionToken\":\"").Append(sessionToken).Append("\",");
            sb.Append("\"region\":\"").Append(region).Append("\",");
            sb.Append("\"serverId\":\"").Append(serverId).Append("\",");
            sb.Append("\"roomId\":\"").Append(roomId).Append("\"");
            sb.Append("}");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static JoinRoomResponse DecodeJoinRoomResp(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return new JoinRoomResponse { Success = false, Message = "Empty payload" };
            }

            try
            {
                var json = Encoding.UTF8.GetString(payload);
                var response = new JoinRoomResponse();

                int numericIdStart = json.IndexOf("\"numericRoomId\"");
                if (numericIdStart >= 0)
                {
                    int colon = json.IndexOf(':', numericIdStart);
                    if (colon > 0)
                    {
                        int comma = json.IndexOf(',', colon);
                        int brace = json.IndexOf('}', colon);
                        int end = Math.Min(comma > 0 ? comma : int.MaxValue, brace > 0 ? brace : int.MaxValue);
                        string numStr = json.Substring(colon + 1, end - colon - 1).Trim();
                        if (ulong.TryParse(numStr, out ulong numericId))
                        {
                            response.NumericRoomId = numericId;
                        }
                    }
                }

                int roomIdStart = json.IndexOf("\"roomId\"");
                if (roomIdStart >= 0)
                {
                    int colon = json.IndexOf(':', roomIdStart);
                    int quote1 = json.IndexOf('"', colon);
                    int quote2 = json.IndexOf('"', quote1 + 1);
                    if (colon > 0 && quote1 >= 0 && quote2 >= 0)
                    {
                        response.RoomId = json.Substring(quote1 + 1, quote2 - quote1 - 1);
                    }
                }

                response.Success = response.NumericRoomId > 0 || !string.IsNullOrEmpty(response.RoomId);
                return response;
            }
            catch (Exception ex)
            {
                return new JoinRoomResponse { Success = false, Message = ex.Message };
            }
        }

        public static byte[] EncodeSubmitFrameInput(string roomId, ulong worldId, int frame,
            uint playerId, uint inputOpCode, byte[] inputPayload = null)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(roomId);
                    writer.Write(worldId);
                    writer.Write(frame);
                    writer.Write((int)1);
                    writer.Write(playerId);
                    writer.Write(inputOpCode);
                    byte[] payload = inputPayload ?? new byte[0];
                    writer.Write(payload.Length);
                    if (payload.Length > 0) writer.Write(payload);
                }
                return ms.ToArray();
            }
        }

        public static void DecodeFramePushed(byte[] payload, out int frame, out List<Tuple<uint, uint, byte[]>> inputs)
        {
            frame = 0;
            inputs = new List<Tuple<uint, uint, byte[]>>();

            if (payload == null || payload.Length < 16) return;

            using (var ms = new MemoryStream(payload))
            {
                using (var reader = new BinaryReader(ms))
                {
                    var roomId = reader.ReadString();
                    var worldId = reader.ReadUInt64();
                    frame = reader.ReadInt32();
                    var inputCount = reader.ReadInt32();

                    for (int i = 0; i < inputCount; i++)
                    {
                        var playerId = reader.ReadUInt32();
                        var opCode = reader.ReadUInt32();
                        var payloadLen = reader.ReadInt32();
                        var inputPayload = payloadLen > 0 ? reader.ReadBytes(payloadLen) : new byte[0];
                        inputs.Add(new Tuple<uint, uint, byte[]>(playerId, opCode, inputPayload));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Moba 操作码
    /// </summary>
    public static class MobaOpCode
    {
        public const uint Move = 3003;
        public const uint Skill1 = 3011;
        public const uint Skill2 = 3012;
        public const uint Skill3 = 3013;
        public const uint Attack = 3004;
        public const uint Stop = 3005;
    }
}
