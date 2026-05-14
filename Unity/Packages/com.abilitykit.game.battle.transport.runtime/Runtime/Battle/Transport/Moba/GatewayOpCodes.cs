namespace AbilityKit.Game.Battle.Transport.Moba
{
}
/// <summary>
/// Gateway 操作码
/// 与 Server/Orleans Gateway 保持一致
/// 
/// 注意: 帧同步和状态同步相关的 OpCode 优先使用 AbilityKit.Protocol.Moba 中的定义：
/// - GatewayFrameSync.OpCodes.FramePushed (9001)
/// - StateSync.OpCodes.SnapshotPushed (9002)
/// 
/// 此处保留兼容性定义
/// </summary>
public static class GatewayOpCodes
{
    public const uint Hello = 1;
    public const uint GuestLogin = 100;
    public const uint CreateSessionForAccount = 122;
    public const uint RenewSession = 120;
    public const uint Logout = 121;

    public const uint CreateRoom = 110;
    public const uint JoinRoom = 111;
    public const uint LeaveRoom = 112;
    public const uint ListRooms = 113;
    public const uint CloseRoom = 114;

    public const uint TimeSync = 1300;

    public const uint SubmitFrameInput = 2001;
    public const uint FramePushed = 9001;
    public const uint SnapshotPushed = 9002;

    public const uint KickPush = 9000;
}
