using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using ColdAlliances.AI;
using RTSEngine;

public class ArmyDefence : ActionNode
{
    const float defenderRangeBuffer = 25f;

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

        float searchRadius = furthestBuildingDist + defenderRangeBuffer;

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
                CheckDefendersLocation(defenders, baseCenter, searchRadius);
            }
        }
        else
        {
            CheckDefendersLocation(defenders, baseCenter, searchRadius);
        }        

        return State.Success;
    }

    private void CheckDefendersLocation(ArmyGroup defenders, Vector3 baseCenter, float searchRadius)
    {
        if (Vector3.Distance(defenders.GetLocation(), baseCenter) > searchRadius / 2)
        {
            context.gameMgr.MvtMgr.Move(defenders.AttackUnits, baseCenter, 0, null, InputMode.movement, false);
        }
    }
}
