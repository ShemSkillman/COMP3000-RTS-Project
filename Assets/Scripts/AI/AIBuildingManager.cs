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
        List<RepairTask> repairTasks = new List<RepairTask>();
        public NPCBuildingPlacer BuildingPlacer { private set; get; }

        public void Init(GameManager gameMgr, FactionManager factionMgr, NPCBuildingPlacer buildingPlacer)
        {
            this.gameMgr = gameMgr;
            this.factionMgr = factionMgr;

            BuildingPlacer = buildingPlacer;
        }

        bool pollRequired = false;
        private void Update()
        {
            pollRequired = true;   
        }

        private void Poll()
        {
            if (!pollRequired)
            {
                return;
            }
            pollRequired = false;

            constructionTasks.RemoveAll(ConstructionTask.IsConstructionTaskInvalid);
            repairTasks.RemoveAll(RepairTask.IsRepairTaskInvalid);
        }

        public bool AssignVillagerToBuildingWithNoBuilder()
        {
            Poll();


            foreach (Building b in factionMgr.GetBuildings())
            {
                if (b.FactionID != factionMgr.FactionID || b.IsBuilt || b.HealthComp.IsDestroyed)
                {
                    continue;
                }

                if (GetBuilderCountForBuilding(b) < Mathf.CeilToInt(b.HealthComp.MaxHealth / 200))
                {
                    Unit builder = GetVillagerToBuild(b.transform.position);
                    if (builder == null)
                        return false;

                    AssignVillagerToBuild(b, builder);

                    return true;
                }
            }

            return false;
        }

        public bool ConstructBuilding(Building building)
        {
            Poll();

            Unit builder = GetVillagerToBuild(factionMgr.BasePosition);
            if (builder == null)
                return false;

            if (BuildingPlacer.OnBuildingPlacementRequest(building, factionMgr.BasePosition, true, out Building placedBuilding))
            {
                AssignVillagerToBuild(placedBuilding, builder);

                return true;
            }

            return false;
        }

        public bool RepairBuilding(Building building)
        {
            Poll();

            Unit builder = GetVillagerToBuild(building.transform.position);
            if (builder == null)
                return false;

            RepairTask task = new RepairTask(building);

            builder.BuilderComp.SetTarget(building);

            task.Builder = builder;
            repairTasks.Add(task);

            return true;
        }

        public Unit GetVillagerToBuild(Vector3 position)
        {
            IdleBuilderTargetPicker targetPicker = 
                new IdleBuilderTargetPicker(factionMgr.FactionID, factionMgr.Slot.GetTypeInfo().Villager.GetCode());

            if (gameMgr.GridSearch.Search(position,
                                        1000f,
                                        false,
                                        targetPicker.IsValidTarget,
                                        out FactionEntity potentialTarget) == ErrorMessage.none)
            {
                return potentialTarget as Unit;
            }

            return null;
        }

        private void AssignVillagerToBuild(Building building, Unit builder)
        {
            ConstructionTask task = new ConstructionTask(building);
            builder.BuilderComp.SetTarget(task.InConstruction);
            task.Builder = builder;
            constructionTasks.Add(task);
        }

        public int GetBuildingInConstructionCount(Building building)
        {
            Poll();

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

        public int GetRepairerCountForBuilding(Building building)
        {
            Poll();

            int counter = 0;

            foreach (RepairTask task in repairTasks)
            {
                if (task.InRepair == building)
                {
                    counter++;
                }
            }

            return counter;
        }

        public int GetBuilderCountForBuilding(Building building)
        {
            Poll();

            int counter = 0;

            foreach (ConstructionTask task in constructionTasks)
            {
                if (task.InConstruction == building)
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
                return task.InConstruction.IsBuilt || task.InConstruction.HealthComp.IsDestroyed || 
                    task.Builder.HealthComp.IsDead() || task.Builder.BuilderComp.GetTarget() != task.InConstruction;
            }
        }

        class RepairTask
        {
            public Building InRepair { get; private set; }
            public Unit Builder { get; set; }

            public RepairTask(Building inRepair, Unit builder = null)
            {
                InRepair = inRepair;
                Builder = builder;
            }

            public static bool IsRepairTaskInvalid(RepairTask task)
            {
                return task.InRepair.HealthComp.IsDestroyed ||
                    task.InRepair.HealthComp.CurrHealth >= task.InRepair.HealthComp.MaxHealth;
            }
        }
    }
}