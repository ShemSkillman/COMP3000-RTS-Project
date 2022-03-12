using RTSEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColdAlliances.AI
{
    public class AIBuildingManager : MonoBehaviour
    {
        GameManager gameMgr;
        FactionManager factionMgr;

        List<ConstructionTask> constructionTasks = new List<ConstructionTask>();
        public NPCBuildingPlacer BuildingPlacer { private set; get; }

        public void Init(GameManager gameMgr, FactionManager factionMgr, NPCBuildingPlacer buildingPlacer)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;

            BuildingPlacer = buildingPlacer;
        }

        public void ConstructBuilding(Building building)
        {
            if (BuildingPlacer.OnBuildingPlacementRequest(building, factionMgr.Slot.CapitalBuilding.gameObject, true, out Building placedBuilding))
            {
                ConstructionTask task = new ConstructionTask(placedBuilding);
                AssignVillagerToBuild(task);

                constructionTasks.Add(task);
            }
        }

        private void AssignVillagerToBuild(ConstructionTask task)
        {
            IdleBuilderTargetPicker targetPicker = new IdleBuilderTargetPicker(factionMgr.Slot.GetTypeInfo().Villager.GetCode());

            if (gameMgr.GridSearch.Search(task.InConstruction.transform.position,
                                        1000f,
                                        false,
                                        targetPicker.IsValidTarget,
                                        out FactionEntity potentialTarget) == ErrorMessage.none)
            {
                Unit newVillager = potentialTarget as Unit;
                newVillager.BuilderComp.SetTarget(task.InConstruction);

                task.Builder = newVillager;
            }
        }

        public int GetBuildingInConstructionCount(Building building)
        {
            int counter = 0;

            foreach (ConstructionTask task in constructionTasks)
            {
                if (task.InConstruction.GetCode() == building.GetCode())
                {
                    counter++;
                }
            }

            return counter;
        }

        class ConstructionTask
        {
            public Building InConstruction { get; private set; }
            public Unit Builder { get; set; }

            public ConstructionTask(Building inConstruction, Unit builder = null)
            {
                InConstruction = inConstruction;
                Builder = builder;
            }

            public static bool IsConstructionTaskInvalid(ConstructionTask task)
            {
                return task.InConstruction.IsBuilt || task.InConstruction.HealthComp.IsDestroyed;
            }
        }
    }
}