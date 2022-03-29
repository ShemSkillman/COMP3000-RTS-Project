using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;
using ColdAlliances.AI;

public class ArmyOffense : ActionNode
{
    protected override State PerformAction() {
        ArmyGroup army = context.combatManager.GetArmyGroup();
        if (army == null)
        {
            return State.Success;
        }

        if (army.ArmyPop() >= 5 && army.IsIdle())
        {
            EnemyTargetPicker targetPicker = new EnemyTargetPicker(context.factionMgr.FactionID);

            Vector3 armyPos = army.GetLocation();

            if (context.gameMgr.GridSearch.Search<FactionEntity>(armyPos,
                                        1000f,
                                        false,
                                        targetPicker.IsValidTarget,
                                        out FactionEntity potentialTarget) == ErrorMessage.none)
            {

                context.gameMgr.AttackMgr.LaunchAttack(army.AttackUnits, potentialTarget, potentialTarget.GetEntityCenterPos(), false);
            }
        }

        return State.Success;
    }
}