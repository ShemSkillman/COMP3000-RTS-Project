using TheKiwiCoder;
using ColdAlliances.AI;
using UnityEngine;

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
            Print("Adding newly trained units to defenders group.");
            defenders.Add(reserves);

            return State.Running;
        }

        float attackWaitTime = context.gameMgr.GameTime - attackTime;

        if (attackWaitTime > attackCountDown && defenders.AttackUnits.Count > 0)
        {
            attackCountDown = context.combatManager.GetRandomAttackCountDown();
            attackTime = context.gameMgr.GameTime;

            attackers.Add(defenders, context.combatManager.AttackForcePercentage);

            string percentage = Mathf.RoundToInt(context.combatManager.AttackForcePercentage * 100).ToString() + "%";
            Print("Moving " + percentage + " of the defending army to attackers.");

            return State.Running;
        }

        if (context.factionMgr.Slot.GetCurrentPopulation() >= context.factionMgr.Slot.MaxPopulation * 0.9f)
        {
            attackers.Add(defenders);

            Print("Transfered all defenders to attacker group in order to overwhelm the enemy.");

            return State.Running;
        }

        Print("No need to re-group military units.");

        return State.Success;
    }
}
