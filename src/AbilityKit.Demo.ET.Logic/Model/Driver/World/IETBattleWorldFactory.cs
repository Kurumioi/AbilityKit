namespace ET.Logic
{
    public interface IETBattleWorldFactory
    {
        ETBattleWorldCreateResult Create(in ETBattleWorldCreateContext context);
    }
}
