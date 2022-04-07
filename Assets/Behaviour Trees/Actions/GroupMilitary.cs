using TheKiwiCoder;
using ColdAlliances.AI;

public class GroupMilitary : ActionNode
{
    protected override State PerformAction() {
        ArmyGroup attackers = context.combatManager.GetAttackers();
        ArmyGroup reserves = context.combatManager.GetReserves();
        ArmyGroup defenders = context.combatManager.GetDefenders();

        if (attackers.AttackUnits.Count > 0)
        {
            attackers.Add(reserves);
        }
        else
        {
            defenders.Add(reserves);
        }

        return State.Success;
    }
}
