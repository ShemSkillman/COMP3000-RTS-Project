using TheKiwiCoder;
using ColdAlliances.AI;

public class GroupMilitary : ActionNode
{
    float attackTime = 0f;
    float attackCountDown;
    bool initialized = false;

    protected override void OnStart()
    {
        base.OnStart();
        if (!initialized)
        {
            attackCountDown = context.combatManager.GetRandomAttackCountDown();
            initialized = true;
        }
    }

    protected override State PerformAction() {
        ArmyGroup attackers = context.combatManager.GetAttackers();
        ArmyGroup reserves = context.combatManager.GetReserves();
        ArmyGroup defenders = context.combatManager.GetDefenders();

        if (reserves.ArmyPop() >= 4)
        {
            defenders.Add(reserves);
        }

        float attackWaitTime = context.gameMgr.GameTime - attackTime;

        if (attackWaitTime > attackCountDown)
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
