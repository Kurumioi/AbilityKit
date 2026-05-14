using System;

namespace AbilityKit.Game.Battle.Transport.Moba
{
}
/// <summary>
/// Guest 登录请求
/// </summary>
public sealed class GuestLoginRequest
{
    public string GuestId { get; set; } = string.Empty;
}

/// <summary>
/// Guest 登录响应
/// </summary>
public sealed class GuestLoginResponse
{
    public bool Success { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 创建房间请求
/// </summary>
public sealed class CreateRoomRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int MaxPlayers { get; set; }
}

/// <summary>
/// 创建房间响应
/// </summary>
public sealed class CreateRoomResponse
{
    public bool Success { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 加入房间请求
/// </summary>
public sealed class JoinRoomRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
}

/// <summary>
/// 加入房间响应
/// </summary>
public sealed class JoinRoomResponse
{
    public bool Success { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ulong NumericRoomId { get; set; }
}

/// <summary>
/// 提交帧输入请求
/// </summary>
public sealed class SubmitFrameInputRequest
{
    public string RoomId { get; set; } = string.Empty;
    public ulong WorldId { get; set; }
    public int Frame { get; set; }
    public uint PlayerId { get; set; }
    public uint InputOpCode { get; set; }
    public byte[] InputPayload { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// 提交帧输入响应
/// </summary>
public sealed class SubmitFrameInputResponse
{
    public bool Accepted { get; set; }
    public int ServerFrame { get; set; }
    public int ReasonCode { get; set; }
}
