namespace AbilityKit.Game.Battle.Transport.Moba.Client
{
}
/// <summary>
/// 战斗启动配置接口
/// </summary>
public interface IBattleStartConfig
{
    int LocalPlayerId { get; }
    int TickRate { get; }
}
