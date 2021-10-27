using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;

/* NPCUnitRegulator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Regulates the creation of a unit type for NPC factions.
    /// </summary>
    public class NPCUnitRegulator : NPCRegulator<Unit>
    {
        #region Class Properties
        /// <summary>
        /// Holds information regarding the creation of the unit type.
        /// </summary>
        public NPCUnitRegulatorData Data { private set; get; }

        /// <summary>
        /// Amount of spawned instances of the regulated unit type to the total available population slots of the NPC faction ratio
        /// </summary>
        private float ratio = 0;

        /// <summary>
        /// Current target amount of spawned instances to create from the regulated unit type.
        /// </summary>
        public int TargetCount { private set; get; }
        
        //each TaskLauncher instance that has one or more tasks to create the unit type is stored here.
        private Dictionary<TaskLauncher, List<int>> unitCreators = new Dictionary<TaskLauncher, List<int>>();

        /// <summary>
        /// Gets the amount of active TaskLauncher instances that include one or more tasks to create the regulated Unit type.
        /// </summary>
        /// <returns>Amount of active TaskLauncher instance with at least a task to create the regulated Unit type.</returns>
        public int GetTaskLauncherCount () { return unitCreators.Count; }

        /// <summary>
        /// Gets the current TaskLauncher intances that can create the regulated unit type.
        /// </summary>
        /// <returns>IEnumerable intance of active TaskLauncher instances.</returns>
        public IEnumerable<TaskLauncher> GetTaskLaunchers () { return unitCreators.Keys; }

        /// <summary>
        /// Gets the task IDs of a TaskLauncher instance that can create the regulated Unit type.
        /// </summary>
        /// <param name="taskLauncher">TaskLauncher instance whose tasks will be searched.</param>
        /// <returns>IEnumerable instance of integers that represent the task IDs if there are any, otherwise null.</returns>
        public IEnumerable<int> GetUnitCreationTasks (TaskLauncher taskLauncher)
        {
            if (unitCreators.TryGetValue(taskLauncher, out List<int> taskIDList))
                return taskIDList;

            return null;
        }

        private NPCUnitCreator unitCreator_NPC; //the NPCUnitCreator instance used to create all instances of the units regulated here.
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// NPCUnitRegulator constructor.
        /// </summary>
        /// <param name="data">Holds information regarding how the unit type that will be regulated.</param>
        /// <param name="prefab">Actual Unit prefab to regulate.</param>
        /// <param name="gameMgr">GameManager instance of the currently active game.</param>
        /// <param name="npcMgr">NPCManager instance that manages the NPC faction to whome the regulator component belongs.</param>
        /// <param name="unitCreator_NPC">NPCUnitCreator instance of the NPC faction that's responsible for creating units.</param>
        public NPCUnitRegulator (NPCUnitRegulatorData data, Unit prefab, GameManager gameMgr, NPCManager npcMgr, NPCUnitCreator unitCreator_NPC) 
            : base(data, prefab, gameMgr, npcMgr)
        {
            this.Data = data;
            Assert.IsNotNull(this.Data,
                $"[NPCUnitRegulator] Initializing without NPCUnitRegulatorData instance is not allowed!");

            this.unitCreator_NPC = unitCreator_NPC;
            Assert.IsNotNull(this.unitCreator_NPC,
                $"[NPCUnitRegulator] Initializing without a reference to the NPCBuildingCreator instance is not allowed!");

            Assert.IsNotNull(prefab,
                $"[NPCUnitRegulator] Initializing without a reference to the unit's prefab is not allowed!");

            //pick the rest random settings from the given info.
            ratio = Data.GetRatio();

            //update the target amount
            UpdateTargetCount();

            //go through all spawned units to see if the units that should be regulated by this instance are created or not:
            foreach (Unit u in this.factionMgr.GetUnits())
                AddExisting(u);

            //go through all spawned task launchers of the faction and track the ones that include tasks to create the regulated unit type.
            foreach (TaskLauncher tl in this.factionMgr.GetTaskLaunchers())
                AddTaskLauncher(tl);

            //start listening to the required delegate events:
            CustomEvents.UnitCreated += Add;
            CustomEvents.UnitConversionComplete += OnUnitConversionComplete;
            CustomEvents.UnitDead += Remove;

            CustomEvents.TaskLaunched += OnTaskLaunched;
            CustomEvents.TaskCanceled += OnTaskCanceled;
            CustomEvents.TaskLauncherAdded += OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved += OnTaskLauncherRemoved;

            CustomEvents.MaxPopulationUpdated += OnMaxPopulationUpdated;
        }

        /// <summary>
        /// Disables the NPCUnitRegulator instance.
        /// </summary>
        public void Disable()
        {
            //stop listening to the delegate events:
            CustomEvents.UnitCreated -= Add;
            CustomEvents.UnitConversionComplete -= OnUnitConversionComplete;
            CustomEvents.UnitDead -= Remove;

            CustomEvents.TaskLaunched -= OnTaskLaunched;
            CustomEvents.TaskCanceled -= OnTaskCanceled;
            CustomEvents.TaskLauncherAdded -= OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved -= OnTaskLauncherRemoved;

            CustomEvents.MaxPopulationUpdated -= OnMaxPopulationUpdated;
        }
        #endregion

        #region Unit Event Callbacks
        /// <summary>
        /// Called whenever a unit is completely converted from one faction to another.
        /// </summary>
        /// <param name="sourceUnit">The Unit instance that launched the conversion.</param>
        /// <param name="targetUnit">The Unit instance that was converted.</param>
        private void OnUnitConversionComplete (Unit sourceUnit, Unit targetUnit)
        {
            Remove(targetUnit); //see if we can remove the converted unit from being regulated.
            Add(targetUnit); //or if the newly converted unit now belongs to this NPC faction, it will be tracked and regulated by this component.
        }
        #endregion

        #region Task Launcher Event Callbacks
        /// <summary>
        /// Called when a new TaskLauncher instance is initialized.
        /// </summary>
        /// <param name="taskLauncher">The TaskLauncher instance that has been added.</param>
        /// <param name="taskID">None.</param>
        /// <param name="taskQueueID">None.</param>
        private void OnTaskLauncherAdded(TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1)
        {
            AddTaskLauncher(taskLauncher);
        }

        /// <summary>
        /// Called when a TaskLauncher instance is destroyed.
        /// </summary>
        /// <param name="taskLauncher">The TaskLauncher instance that has been destroyed/removed.</param>
        /// <param name="taskID">None.</param>
        /// <param name="taskQueueID">None.</param>
        private void OnTaskLauncherRemoved(TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1) //called when a task launcher has been removed
        {
            RemoveTaskLauncher(taskLauncher);
        }

        /// <summary>
        /// Called whenever a FactionEntityTask instance is launched from a TaskLauncher instance.
        /// </summary>
        /// <param name="taskLauncher">TaskLauncher instance whose task is launched.</param>
        /// <param name="taskID">ID of the launched task.</param>
        /// <param name="taskQueueID">ID of the launched task in the waiting queue.</param>
        private void OnTaskLaunched(TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            //if the launched task is being tracked by this regulator.
            if (unitCreators.TryGetValue(taskLauncher, out List<int> taskIDList) && taskIDList.Contains(taskID))
                AddPending();
        }

        /// <summary>
        /// Called whenever a FactionEntityTask instance is canceled while in progress in a TaskLauncher instance.
        /// </summary>
        /// <param name="taskLauncher">TaskLauncher instance whose task is cancelled.</param>
        /// <param name="taskID">ID of the cancelled task.</param>
        /// <param name="taskQueueID">None.</param>
        private void OnTaskCanceled(TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            //if the canceled task is being tracked by this regulator.
            if (unitCreators.TryGetValue(taskLauncher, out List<int> taskIDList) && taskIDList.Contains(taskID))
                Remove();
        }
        #endregion

        #region Task Launcher Tracking
        /// <summary>
        /// Adds a TaskLauncher instance to be tracked by the NPCUnitRegulator if it includes tasks that allow the regulated unit type.
        /// </summary>
        /// <param name="taskLauncher">TaskLauncher instance to add.</param>
        public void AddTaskLauncher (TaskLauncher taskLauncher)
        {
            if (unitCreators.ContainsKey(taskLauncher) //if the task launcher is already registered
                || taskLauncher.FactionEntity.FactionID != factionMgr.FactionID) //or the task launcher doesn't belong to the same NPC faction.
                return;

            for(int taskID = 0; taskID < taskLauncher.GetTasksCount(); taskID++)
            {
                FactionEntityTask nextTask = taskLauncher.GetTask(taskID);
                //if the task's type is unit creation and it the unit to create code matches the code of unit type that is regulated by this component
                if (nextTask.GetTaskType() == TaskTypes.createUnit && nextTask.UnitCode == Code)
                {
                    if (unitCreators.TryGetValue(taskLauncher, out List<int> taskIDList)) //task launcher already is registered as key, just append value list
                        taskIDList.Add(taskID);
                    else //register task launcher as new key and task ID as the only element in the value list.
                        unitCreators.Add(taskLauncher, new List<int>(new int[] { taskID }));
                }
            }
        }

        /// <summary>
        /// Removes a TaskLauncher instance from being tracked by the NPCUnitRegulator instance.
        /// </summary>
        /// <param name="taskLauncher">TaskLauncher instance to remove.</param>
        public void RemoveTaskLauncher (TaskLauncher taskLauncher)
        {
            unitCreators.Remove(taskLauncher); //if the task launcher instance was tracked by this regulator then it will be removed.
        }
        #endregion

        #region Spawned Instances Manipulation
        /// <summary>
        /// Called when a Unit instance is successfully removed from being tracked and regulated by the NPCUnitRegulator instance.
        /// </summary>
        /// <param name="factionEntity">Unit instance that was removed.</param>
        protected override void OnSuccessfulRemove (Unit unit)
        {
            if(!HasReachedMaxAmount()) //and maximum allowed amount hasn't been reached yet
                unitCreator_NPC.Activate(); //activate unit creator to create more instances of this unit type.
        }

        /// <summary>
        /// Returns a list of the regulated unit type instances with idle instances placed in the beginning of the list.
        /// </summary>
        /// <returns>List where each element is the spawned Unit instance of the regulated unit type.</returns>
        public List<Unit> GetIdleUnitsFirst ()
        {
            return instances.OrderByDescending(unit => unit.IsIdle()).ToList();
        }
        #endregion

        #region Target Count Manipulation
        /// <summary>
        /// Called when the maximum population slots amount of a faction is updated.
        /// </summary>
        /// <param name="factionSlot">FactionSlot instance that manages the faction whose max population is updated.</param>
        /// <param name="value">The value by which the max population amount is updated.</param>
        public void OnMaxPopulationUpdated(FactionSlot factionSlot, int value)
        {
            //if this update belongs to the faction managed by this component:
            if (factionSlot.FactionMgr.FactionID == factionMgr.FactionID)
                UpdateTargetCount();
        }

        /// <summary>
        /// Updates the target amount of instances to have from the regulated unit type for the NPC faction.
        /// </summary>
        public void UpdateTargetCount()
        {
            //calculate new target amount for the regulated unit type instances and limit by max and min allowed amount
            TargetCount = Mathf.Clamp(
                (int)(factionMgr.Slot.GetMaxPopulation() * ratio),
                MinAmount,
                MaxAmount);

            //if the maximum amount hasn't been reached yet
            if (!HasReachedMaxAmount())
                unitCreator_NPC.Activate(); //activate the unit creator to push to create more instances of the regulated type
        }
        #endregion
    }
}