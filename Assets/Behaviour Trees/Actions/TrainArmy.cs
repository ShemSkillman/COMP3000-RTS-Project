using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using RTSEngine;

public class TrainArmy : ActionNode
{
    Queue<Building> buildings;
    Building currentBuilding;
    int queuedPop = 0;
    protected override State PerformAction() {
        if (context.factionMgr.GetBuildingCategoryCount("military") <= 0)
        {
            Print("Cannot train military units because there are no military buildings.");
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
                        blackboard.isMilitaryBuildingNeeded = true;

                        Print("Cannot train military units because there are no military buildings.");
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
                currentBuilding.GetCategory() == "military" &&
                currentBuilding.TaskLauncherComp.GetTaskQueueCount() < currentBuilding.TaskLauncherComp.GetMaxTasksAmount() &&
                queuedPop < 4)
            {
                if (currentBuilding.TaskLauncherComp.GetTask(0).HasRequiredResources() &&
                currentBuilding.TaskLauncherComp.GetTask(0).UnitPopulationSlots <= context.factionMgr.Slot.GetFreePopulation())
                {
                    currentBuilding.TaskLauncherComp.Add(0);

                    queuedPop += currentBuilding.TaskLauncherComp.GetTask(0).UnitPopulationSlots;

                    Print("Training " + currentBuilding.TaskLauncherComp.GetTask(0).UnitCode + " unit from " + currentBuilding.GetName());

                    return State.Running;
                }
                else
                {
                    Print("Could not train " + currentBuilding.TaskLauncherComp.GetTask(0).UnitCode + " unit from " + currentBuilding.GetName() + 
                        " because there isn't enough population space/resources.");
                    blackboard.isMilitaryBuildingNeeded = false;

                    return State.Success;
                }
            }

            currentBuilding = null;
            queuedPop = 0;
        }

        blackboard.isMilitaryBuildingNeeded = true;

        Print("All existing military buildings are busy training units.");
        return State.Success;
    }
}
