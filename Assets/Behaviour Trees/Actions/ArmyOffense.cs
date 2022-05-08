using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;
using ColdAlliances.AI;

public class ArmyOffense : ActionNode
{
    protected override State PerformAction() {
        ArmyGroup army = context.combatManager.GetAttackers();
        if (army == null)
        {
            Print("No attackers to send to attack.");
            return State.Success;
        }

        if (army.IsIdle())
        {
            EnemyTargetPicker targetPicker = new EnemyTargetPicker(context.factionMgr.FactionID);

            Vector3 armyPos = army.GetLocation();

            if (context.gameMgr.GridSearch.Search<FactionEntity>(armyPos,
                                        1000f,
                                        false,
                                        targetPicker.IsValidTarget,
                                        out FactionEntity potentialTarget) == ErrorMessage.none)
            {
                Print("Sending attackers to attack hostile " + potentialTarget.GetName());
                context.gameMgr.AttackMgr.LaunchAttack(army.AttackUnits, potentialTarget, potentialTarget.GetEntityCenterPos(), false);
            }
            else
            {
                Print("Attackers have no hostiles to attack.");
            }
        }
        else
        {
            Print("Attackers are busy attacking! No need to assign them to a hostile.");
        }

        return State.Success;
    }
}