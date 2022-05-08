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
                context.buildingManager.GetRepairerCountForBuilding(currentBuilding) < Mathf.CeilToInt((currentBuilding.HealthComp.MaxHealth - currentBuilding.HealthComp.CurrHealth) / 200f))
            {
                EnemyMilitaryTargetPicker picker = new EnemyMilitaryTargetPicker(context.factionMgr.FactionID);
                if (context.gameMgr.GridSearch.Search(currentBuilding.transform.position,
                    15f,
                    false,
                    picker.IsValidTarget,
                    out FactionEntity potentialTarget) != ErrorMessage.none)
                {
                    if (context.buildingManager.RepairBuilding(currentBuilding))
                    {
                        Print("Sending villager to repair " + currentBuilding.GetName() + " on " + currentBuilding.HealthComp.CurrHealth.ToString() + "/" +
                            currentBuilding.HealthComp.MaxHealth.ToString() + " health.");
                        return State.Running;
                    }
                    else
                    {
                        Print("No villagers are available to repair " + currentBuilding.GetName() + " on " + 
                            currentBuilding.HealthComp.CurrHealth.ToString() + "/" +
                            currentBuilding.HealthComp.MaxHealth.ToString() + " health.");
                        return State.Failure;
                    }
                }                
            }

            currentBuilding = null;
        }

        Print("No damaged buildings that are safe to repair could be found.");
        return State.Success;
    }
}
