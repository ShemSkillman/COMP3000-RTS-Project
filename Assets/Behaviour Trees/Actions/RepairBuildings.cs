using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class RepairBuildings : ActionNode
{
    Queue<Building> buildings;
    Building currentBuilding;

    protected override State PerformAction() {
        Dictionary<Building, bool> visitedBuildings = new Dictionary<Building, bool>();

        while (true)
        {
            if (currentBuilding == null)
            {
                if (buildings == null || buildings.Count < 1)
                {
                    buildings = new Queue<Building>(context.factionMgr.GetBuildings());

                    if (buildings.Count < 1)
                    {
                        return State.Success;
                    }
                }

                currentBuilding = buildings.Dequeue();
            }

            if (currentBuilding != null)
            {
                if (visitedBuildings.ContainsKey(currentBuilding))
                {
                    break;
                }
                else
                {
                    visitedBuildings[currentBuilding] = true;
                }
            }

            if (currentBuilding != null &&
                currentBuilding.IsBuilt &&
                !currentBuilding.HealthComp.IsDestroyed &&
                currentBuilding.HealthComp.CurrHealth < currentBuilding.HealthComp.MaxHealth &&
                context.buildingManager.GetRepairerCountForBuilding(currentBuilding) < Mathf.CeilToInt((currentBuilding.HealthComp.MaxHealth - currentBuilding.HealthComp.CurrHealth) / 200))
            {
                if (context.buildingManager.RepairBuilding(currentBuilding))
                {
                    return State.Running;
                }
                else
                {
                    return State.Failure;
                }
            }

            currentBuilding = null;
        }

        return State.Success;
    }
}
