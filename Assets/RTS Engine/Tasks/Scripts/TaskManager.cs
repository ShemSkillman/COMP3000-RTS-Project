using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    //Unit Creation Task attributes:
    [System.Serializable]
    public struct UnitCreationTaskAttributes
    {
        //one prefab of this list will be randomly chosen to be created. Simply have one element here if you wish to use one prefab.
        public List<Unit> prefabs;
    }

    public enum TaskTypes {
        none,
        movement,
        placeBuilding, build,
        generateResource, collectResource,
        convert,
        heal,
        APCEject, APCEjectAll, APCCall, 
        createUnit, destroyBuilding, cancelPendingTask, 
        attackTypeSelection, attack, 
        toggleWander,
        upgrade,
        destroy,
        deselectIndiv, deselectMul,
        customTask,
        lockAttack,
    };

	public class TaskManager : MonoBehaviour {

        public TaskStatusManager Status { private set; get; }

        //the component handling the unit component tasks
        public UnitComponentTaskManager UnitComponent {
            private set
            {
                unitComponentTasks = value;
            }
            get
            {
                return unitComponentTasks;
            }
        }
        [SerializeField]
        private UnitComponentTaskManager unitComponentTasks = new UnitComponentTaskManager();

        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            Status = new TaskStatusManager();
            unitComponentTasks.Init();
        }

        //checks whether a faction entity is able to launch a task or not
        public ErrorMessage CanAddTask (FactionEntity factionEntity)
        {
            if (factionEntity == null) //no source entity specified, return success
                return ErrorMessage.none;

            if (factionEntity.EntityHealthComp.IsDead() == true) //if the task holder is dead -> can't launch task
                return ErrorMessage.sourceDead;

            if(factionEntity.Type == EntityTypes.building) //if the task holder is a building and it's not built
            {
                if (((Building)factionEntity).IsBuilt == false)
                    return ErrorMessage.buildingNotBuilt;
            }

            return ErrorMessage.none;
        }

        //checks whether a task can be added or not and if not, provide the reason in the return value
        public ErrorMessage CanAddTaskToLauncher(TaskLauncher taskLauncher, FactionEntityTask task)
        {
            if (Status.IsTaskEnabled(task.GetCode(), taskLauncher.FactionEntity.FactionID, task.IsAvailable) == false) //if the task is disabled
                return ErrorMessage.componentDisabled;

            if (taskLauncher.FactionEntity.EntityHealthComp.CurrHealth < taskLauncher.GetMinHealth()) //if the task holder does not have enough health to launch task
                return ErrorMessage.sourceLowHealth;

            if (taskLauncher.GetMaxTasksAmount() <= taskLauncher.GetTaskQueueCount()) //if the maximum amount of pending tasks has been reached
                return ErrorMessage.sourceMaxCapacityReached;

            if (task.HasRequiredResources() == false)
                return ErrorMessage.lowResources;
            
            if(task.GetTaskType() == TaskTypes.createUnit) //if this is a unit creation task..
            {
                int nextPopulation = task.UnitPopulationSlots + gameMgr.GetFaction(taskLauncher.FactionEntity.FactionID).GetCurrentPopulation();
                if (nextPopulation > gameMgr.GetFaction(taskLauncher.FactionEntity.FactionID).GetMaxPopulation()) //check the population slots
                    return ErrorMessage.maxPopulationReached;

                if (taskLauncher.FactionEntity.FactionMgr.HasReachedLimit(task.UnitCode, task.UnitCode)) //did the unit type to create reach its limit,
                    return ErrorMessage.factionLimitReached;
            }

            return CanAddTask(taskLauncher.FactionEntity);
        }

        //called to add a task to a task launcher
        public ErrorMessage AddTask(TaskUIAttributes attributes, bool playerCommand = false)
        {
            ErrorMessage addTaskMsg = ErrorMessage.none;
            FactionEntity sourceFactionEntity = attributes.source as FactionEntity;

            if (attributes.unitComponentTask == false && attributes.taskLauncher != null) //adding a task to task launcher
            {
                //if this task is simply cancelling a pending task, then execute it directly and don't proceed:
                if (attributes.type == TaskTypes.cancelPendingTask)
                {
                    attributes.taskLauncher.CancelInProgressTask(attributes.ID);
                    return ErrorMessage.none; //instant success
                }

                //-> dealing with a task that gets added to the task queue
                addTaskMsg = CanAddTaskToLauncher(attributes.taskLauncher, attributes.taskLauncher.GetTask(attributes.ID)); //check if the task can be added
                if (addTaskMsg != ErrorMessage.none) //if it's not successful
                {
                    if (playerCommand == true) //if this is a player command
                    {
                        gameMgr.AudioMgr.PlaySFX(attributes.taskLauncher.GetLaunchDeclinedAudio(), false);
                        ErrorMessageHandler.OnErrorMessage(addTaskMsg, attributes.source); //display error
                    }
                    return addTaskMsg; //then return failure reason and stop
                }
                attributes.taskLauncher.Add(attributes.ID);

                return ErrorMessage.none;
            }
            else if ((addTaskMsg = CanAddTask(sourceFactionEntity)) != ErrorMessage.none) //not a task launcher but we still check whether the source is valid enough to launch task
            {
                if (playerCommand == true) //if this is a player command
                        ErrorMessageHandler.OnErrorMessage(addTaskMsg, attributes.source); //display error

                return addTaskMsg; //then return failure reason and stop
            }


            switch (attributes.type)
            {
                case TaskTypes.deselectIndiv: //deselecting individual units

                    if (gameMgr.SelectionMgr.MultipleSelectionKeyDown == true) //if the player is holding the multiple selection key then...
                        gameMgr.SelectionMgr.Selected.Remove(attributes.source); //deselect the clicked entity
                    else
                        gameMgr.SelectionMgr.Selected.Add(attributes.source, SelectionTypes.single); //not holding the multiple key then select this unit only
                    break;

                case TaskTypes.deselectMul: //deselecting multiple units

                    if (gameMgr.SelectionMgr.MultipleSelectionKeyDown == true) //if the player is holding the multiple selection key then...
                        gameMgr.SelectionMgr.Selected.Remove(attributes.sourceList); //deselect the clicked entity
                    else
                        gameMgr.SelectionMgr.Selected.Add(attributes.sourceList.ToArray()); //not holding the multiple key then select this unit only
                    break;
                case TaskTypes.APCEject: //APC release task.

                    sourceFactionEntity.APCComp.Eject(sourceFactionEntity.APCComp.GetStoredUnit(attributes.ID), false);
                    break;
                case TaskTypes.APCEjectAll: //APC release task.

                    sourceFactionEntity.APCComp.EjectAll(false);
                    break;
                case TaskTypes.APCCall: //apc calling units

                    sourceFactionEntity.APCComp.CallUnits();
                    break;
                case TaskTypes.placeBuilding:

                    gameMgr.PlacementMgr.StartPlacingBuilding(attributes.ID);

                    break;

                case TaskTypes.toggleWander:

                    ((Unit)sourceFactionEntity).WanderComp.Toggle(); //toggle the wandering behavior
                    break;
                case TaskTypes.cancelPendingTask:

                    attributes.taskLauncher.CancelInProgressTask(attributes.ID);
                    break;
                default:
                    if (attributes.unitComponentTask == true)
                        UnitComponent.SetAwaitingTaskType(attributes.type, attributes.icon);
                    break;
            }

            return ErrorMessage.none;
        }
    }
}