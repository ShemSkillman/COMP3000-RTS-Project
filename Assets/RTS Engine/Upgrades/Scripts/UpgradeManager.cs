using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;

/* Upgrade Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class UpgradeManager : MonoBehaviour
    {
        //holds unit upgrade tasks info that need to be synced when a new task launcher is added
        private struct UpgradedUnitTask
        {
            public int factionID;
            public string upgradedUnitCode;
            public Unit targetUnitPrefab;
            public Upgrade.NewTaskInfo newTaskInfo;
        }
        private List<UpgradedUnitTask> upgradedUnitTasks = new List<UpgradedUnitTask>();

        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }

        private void OnEnable()
        {
            //start listening to custom events:
            CustomEvents.TaskLauncherAdded += OnTaskLauncherAdded;
        }

        private void OnDisable()
        {
            //stop listening to custom events:
            CustomEvents.TaskLauncherAdded -= OnTaskLauncherAdded;
        }

        //method called when a unit/building upgrade is launched
        public void LaunchUpgrade (Upgrade upgrade, int upgradeID, FactionEntity upgradeLauncher, bool oneInstance)
        {
            int factionID = upgradeLauncher.FactionID;
            string sourceCode = upgrade.Source.GetCode();
            EntityTypes sourceType = upgrade.Source.Type;

            Assert.IsTrue(upgrade.GetTarget(upgradeID).Type == sourceType, "[UpgradeManager] The upgrade target doesn't have the same type as the upgrade source!");

            if (oneInstance) //if this is a one instance upgrade type
            {
                //if this is a one instance upgrade, make sure that the upgrade source code and the task holder are the same
                if (upgradeLauncher.Type != sourceType || upgradeLauncher.GetCode() != sourceCode)
                {
                    Debug.LogError("[UpgradeManager] Can not launch a one instance upgrade where the upgrade source and the source task launcher are different!");
                    return;
                }

                UpgradeInstance(upgradeLauncher, upgrade.GetTarget(upgradeID), factionID, 
                    gameMgr.GetFaction(factionID).PlayerControlled ? upgrade.GetUpgradeEffect() : null);
            }
            else if(upgrade.CanUpgradeSpawnedInstances()) //if we can upgrade all spawned instances of the source upgrade
            {
                List<FactionEntity> currEntities = upgradeLauncher.FactionMgr.GetFactionEntities().ToList();
                //go through the spawned instances list of the faction:
                foreach (FactionEntity instance in currEntities)
                {
                    //if this building/unit matches the instance to be upgraded
                    if (instance.GetCode() == sourceCode)
                        //upgrade it
                        UpgradeInstance(instance, upgrade.GetTarget(upgradeID), factionID, 
                            gameMgr.GetFaction(factionID).PlayerControlled ? upgrade.GetUpgradeEffect() : null);
                }
            }

            switch(sourceType) //depending on the type of the source
            {
                case EntityTypes.building:

                    if (!oneInstance && gameMgr.GetFaction(factionID).PlayerControlled) //if this is not a one instance upgrade then update the placable buildings list for the player faction
                        //search for the building instance inside the buildings list that the player is able to place.
                        gameMgr.PlacementMgr.ReplaceBuilding(sourceCode, (Building)upgrade.GetTarget(upgradeID));

                    //trigger the upgrade event:
                    CustomEvents.OnBuildingUpgraded(upgrade, upgradeID);

                    break;

                case EntityTypes.unit:

                    if (!oneInstance) //if this is not a one instance upgrade then update all source unit type creation tasks
                    {
                        //go through the active task launchers:
                        foreach (TaskLauncher tl in gameMgr.GetFaction(factionID).FactionMgr.GetTaskLaunchers())
                        {
                            //and sync the upgraded tasks
                            UpdateUnitCreationTask(tl, sourceCode, (Unit)upgrade.GetTarget(upgradeID), upgrade.GetNewTaskInfo());
                        }

                        //register the upgraded unit creation task:
                        UpgradedUnitTask uut = new UpgradedUnitTask()
                        {
                            factionID = factionID,
                            upgradedUnitCode = sourceCode,
                            targetUnitPrefab = (Unit)upgrade.GetTarget(upgradeID),
                            newTaskInfo = upgrade.GetNewTaskInfo()
                        };
                        //add it to the list:
                        upgradedUnitTasks.Add(uut);
                    }

                    //trigger the upgrade event:
                    CustomEvents.OnUnitUpgraded(upgrade, upgradeID);

                    break;
            }

            //trigger upgrades?
            LaunchTriggerUpgrades(upgrade.GetTriggerUpgrades(), upgradeLauncher);
        }

        //trigger unit/building upgrades locally:
        private void LaunchTriggerUpgrades(IEnumerable<Upgrade> upgrades, FactionEntity upgradeLauncher)
        {
            foreach (Upgrade u in upgrades)
                LaunchUpgrade(u, 0, upgradeLauncher, false); //will trigger the upgrade for the first target only!
        }

        //a method that upgrades a faction entity instance locally
        public void UpgradeInstance (FactionEntity instance, FactionEntity target, int factionID, EffectObj upgradeEffect)
        {
            switch(instance.Type)
            {
                case EntityTypes.building:

                    Building currBuilding = instance as Building;
                    Unit[] currBuilders = currBuilding.WorkerMgr.GetAll(); //get the current builders of this building if there are any
                    foreach (Unit unit in currBuilders) //and make them stop building the instance of the building since it will be destroyed.
                        unit.BuilderComp.Stop();

                    //create upgraded instance of the building
                    Building upgradedBuilding = gameMgr.BuildingMgr.CreatePlacedInstanceLocal(
                        (Building)target,
                        instance.transform.position,
                        target.transform.rotation.eulerAngles.y, 
                        ((Building)instance).CurrentCenter, 
                        factionID,
                        currBuilding.IsBuilt, //depends on the current state of the instance to destroy
                        ((Building)instance).FactionCapital);

                    foreach (Unit unit in currBuilders)
                        unit.BuilderComp.SetTarget(upgradedBuilding);

                    CustomEvents.OnBuildingInstanceUpgraded((Building)instance); //trigger custom event
                    break;
                    
                case EntityTypes.unit:

                    //create upgraded instance of the unit
                    gameMgr.UnitMgr.CreateUnit(
                        (Unit)target, 
                        instance.transform.position,
                        instance.transform.rotation,
                        instance.transform.position,
                        factionID, 
                        null, false, true);

                    CustomEvents.OnUnitInstanceUpgraded((Unit)instance); //trigger custom event
                    break;

            }

            //if there's a valid upgrade effect assigned:
            if (upgradeEffect != null)
                //show the upgrade effect for the player:
                gameMgr.EffectPool.SpawnEffectObj(upgradeEffect, instance.transform.position, upgradeEffect.transform.rotation);

            instance.EntityHealthComp.DestroyFactionEntity(true); //destroy the instance
        }

        //called whenever a task launcher is added
        private void OnTaskLauncherAdded (TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            SyncUnitCreationTasks(taskLauncher); //sync the upgraded unit creation tasks
        }

        //sync all upgraded unit creation tasks for a task launcher:
        private void SyncUnitCreationTasks (TaskLauncher taskLauncher)
        {
            //go through the registered upgraded unit tasks
            foreach(UpgradedUnitTask uut in upgradedUnitTasks)
            {
                //if this task launcher belongs to the faction ID that has the upgraded unit creation task:
                if (uut.factionID == taskLauncher.FactionEntity.FactionID)
                {
                    //sync the unit creation tasks.
                    UpdateUnitCreationTask(taskLauncher, uut.upgradedUnitCode, uut.targetUnitPrefab, uut.newTaskInfo);
                }
            }
        }

        //update an upgraded unit creation task's info:
        private void UpdateUnitCreationTask (TaskLauncher taskLauncher, string upgradedUnitCode, Unit targetUnitPrefab, Upgrade.NewTaskInfo newTaskInfo)
        {
            //go through the tasks:
            for(int i = 0; i < taskLauncher.GetTasksCount(); i++)
                taskLauncher.GetTask(i).Update(upgradedUnitCode, targetUnitPrefab, newTaskInfo);
        }
    }
}

