using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using ColdAlliances.AI;
using RTSEngine;

public class ArmyDefence : ActionNode
{
    protected override State PerformAction() {

        ArmyGroup defenders = context.combatManager.GetDefenders();
        if (defenders.AttackUnits.Count < 1)
        {
            return State.Success;
        }

        Vector3 baseCenter = context.factionMgr.GetBaseCenter();

        List<Building> buildings = new List<Building>(context.factionMgr.GetBuildings());

        float furthestBuildingDist = 0;
        foreach (Building b in buildings)
        {
            float dist = Vector3.Distance(baseCenter, b.transform.position);
            if (dist > furthestBuildingDist)
            {
                furthestBuildingDist = dist;
            }
        }

        float searchRadius = furthestBuildingDist + 30;

        if (defenders.IsIdle())
        {
            EnemyTargetPicker targetPicker = new EnemyTargetPicker(context.factionMgr.FactionID);

            Vector3 armyPos = defenders.GetLocation();

            if (context.gameMgr.GridSearch.Search<FactionEntity>(baseCenter,
                                        searchRadius,
                                        false,
                                        targetPicker.IsValidTarget,
                                        out FactionEntity potentialTarget) == ErrorMessage.none)
            {

                context.gameMgr.AttackMgr.LaunchAttack(defenders.AttackUnits, potentialTarget, potentialTarget.GetEntityCenterPos(), false);
            }
            else
            {
                if (Vector3.Distance(armyPos, baseCenter) > searchRadius / 2)
                {
                    context.gameMgr.MvtMgr.Move(defenders.AttackUnits, baseCenter, 0, null, InputMode.movement, false);
                }
            }
        }
        

        return State.Success;
    }
}
