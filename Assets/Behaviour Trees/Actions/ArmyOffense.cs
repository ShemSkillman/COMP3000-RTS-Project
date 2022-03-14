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

        if (context.combatManager.GetArmyGroup().AttackUnits.Count > 4)
        {
            List<Building> buildings = context.factionMgr.GetEnemyBuildings();
            Vector3 armyPos = army.GetLocation();
            Building closestBuilding = null;
            float closestDist = Mathf.Infinity;
            foreach (Building b in buildings)
            {
                float dist = Vector3.Distance(b.transform.position, armyPos);
                if (dist < closestDist)
                {
                    closestBuilding = b;
                    closestDist = dist;
                }
            }

            context.gameMgr.AttackMgr.LaunchAttack(army.AttackUnits, closestBuilding, closestBuilding.GetEntityCenterPos(), false);
        }

        return State.Success;
    }
}