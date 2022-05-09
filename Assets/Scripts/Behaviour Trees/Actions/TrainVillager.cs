using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class TrainVillager : ActionNode
{
    Queue<Building> buildings;
    Building currentBuilding;

    protected override State PerformAction() {
        if (context.factionMgr.GetBuildingCount("town_center") <= 0)
        {
            Print("Cannot train villagers because town center is destoyed.");
        }

        int villagerCountGoal = context.factionMgr.Slot.MaxPopulation / 2;
        int villagerCount = context.factionMgr.Villagers.Count;

        if (villagerCount >= villagerCountGoal)
        {
            Print("Villager count goal is met, there is no need to train more.");
            return State.Success;
        }

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
                        Print("Cannot train villagers because town center is destoyed.");
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
                currentBuilding.GetCode() == "town_center" &&
                currentBuilding.TaskLauncherComp.GetTaskQueueCount() < 1)
            {
                if (currentBuilding.TaskLauncherComp.GetTask(0).HasRequiredResources() &&
                currentBuilding.TaskLauncherComp.GetTask(0).UnitPopulationSlots <= context.factionMgr.Slot.GetFreePopulation())
                {
                    currentBuilding.TaskLauncherComp.Add(0);

                    Print("Training villager from town center.");
                    return State.Running;
                }
                else
                {
                    Print("Could not train villager from town center because there isn't enough population space/resources.");
                    return State.Success;
                }
            }

            currentBuilding = null;
        }

        Print("Town center is busy training villagers, no need to queue more for training.");
        return State.Success;
    }
}