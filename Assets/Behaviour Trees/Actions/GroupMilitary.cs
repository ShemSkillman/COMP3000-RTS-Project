using TheKiwiCoder;
using ColdAlliances.AI;

public class GroupMilitary : ActionNode
{
    float attackTime = 0f;
    float attackCountDown = -1f;

    protected override State PerformAction() {
        ArmyGroup attackers = context.combatManager.GetAttackers();
        ArmyGroup reserves = context.combatManager.GetReserves();
        ArmyGroup defenders = context.combatManager.GetDefenders();

        if (attackCountDown < 0)
        {
            attackCountDown = context.combatManager.GetRandomAttackCountDown();
        }

        if (reserves.ArmyPop() >= 4)
        {
            defenders.Add(reserves);
        }

        if (context.gameMgr.GameTime - attackTime > attackCountDown)
        {
            attackCountDown = context.combatManager.GetRandomAttackCountDown();
            attackTime = context.gameMgr.GameTime;

            attackers.Add(defenders, context.combatManager.AttackForcePercentage);
        }

        if (context.factionMgr.Slot.GetCurrentPopulation() >= context.factionMgr.Slot.MaxPopulation * 0.9f)
        {
            attackers.Add(defenders);
        }

        return State.Success;
    }
}
