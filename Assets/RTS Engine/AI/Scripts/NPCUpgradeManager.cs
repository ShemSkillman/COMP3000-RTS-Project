using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* NPCUpgradeManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Responsible for launching upgrade tasks for the NPC faction.
    /// </summary>
    public class NPCUpgradeManager : NPCComponent
    {
        //have timer that checks upgrade tasks
        //a field to prioritize unit or building upgrade over each other.

        //when a building or unit get upgraded, send replacement request to building creator/unit creator
        //components other than unit creator or building creator should have codes

        #region Class Properties
        private Dictionary<TaskLauncher, List<FactionEntityTask>> upgradeTasks = new Dictionary<TaskLauncher, List<FactionEntityTask>>(); //hold each task launcher instance with its task IDs dedicated for upgrades.

        //codes of the source faction entities who have pending upgrade tasks are held in this list
        private List<string> pendingUpgrades = new List<string>();

        [SerializeField, Tooltip("Allow component to launch upgrade tasks when they are available?")]
        private bool autoUpgrade = true; //if enabled, then this component will launch task upgrade automatically
        [SerializeField, Tooltip("How often does the NPC faction attempt to launch upgrade tasks?")]
        private FloatRange upgradeReloadRange = new FloatRange(5.0f, 10.0f); //the timer reload (in seconds) for which upgrade tasks are checked and possibily launched
        private float upgradeTimer;

        //the acceptance range adds some randomness to NPC factions launching upgrade tasks.
        //each time a random float between 0.0f and 1.0f will be generated and if it is below a random value chosen from the below range...
        //...then the upgrade will be chosen. So this means that 0.0f -> upgrade will never be launched and 1.0f -> upgrade will always be launched.
        [SerializeField, Tooltip("Between 0.0 and 1.0, randomizes upgrade decisions where the higher the value, the higher chance to launch an upgrade.")]
        private FloatRange acceptanceRange = new FloatRange(0.5f, 0.8f);

        [SerializeField, Tooltip("Allow other NPC components to launch upgrade tasks?")]
        private bool upgradeOnDemand = true; //can other components request launching an upgrade task?
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Initializes the NPCUpgradeManager instance, called from the NPCManager instance responsible for this component.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the current game.</param>
        /// <param name="npcMgr">NPCManager instance that manages this NPCComponent instance.</param>
        /// <param name="factionMgr">FactionManager instance of the faction that this component manages.</param>
        public override void Init(GameManager gameMgr, NPCManager npcMgr, FactionManager factionMgr)
        {
            base.Init(gameMgr, npcMgr, factionMgr);

            //initially, this component is active if it can auto upgrade.
            if (autoUpgrade)
                Activate();
            else
                Deactivate();

            //go through the currently spawned task launchers
            foreach (TaskLauncher taskLauncher in factionMgr.GetTaskLaunchers())
                AddTaskLauncher(taskLauncher); //attempt to add the task launchers and see if they contain upgrade tasks.

            //start the timer:
            upgradeTimer = upgradeReloadRange.getRandomValue();

            //start listening to the delegate events
            CustomEvents.TaskLauncherAdded += OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved += OnTaskLauncherRemoved;

            CustomEvents.BuildingUpgraded += OnFactionEntityUpgraded;
            CustomEvents.UnitUpgraded += OnFactionEntityUpgraded;

            CustomEvents.TaskLaunched += OnTaskLaunched;
            CustomEvents.TaskCompleted += OnTaskStopped;
            CustomEvents.TaskCanceled += OnTaskStopped;
        }

        /// <summary>
        /// Called when the object holding this component is disabled/destroyed.
        /// </summary>
        private void OnDestroy()
        {
            //stop listening to the delegate events:
            CustomEvents.TaskLauncherAdded -= OnTaskLauncherAdded;
            CustomEvents.TaskLauncherRemoved -= OnTaskLauncherRemoved;

            CustomEvents.BuildingUpgraded -= OnFactionEntityUpgraded;
            CustomEvents.UnitUpgraded -= OnFactionEntityUpgraded;

            CustomEvents.TaskLaunched -= OnTaskLaunched;
            CustomEvents.TaskCompleted -= OnTaskStopped;
            CustomEvents.TaskCanceled -= OnTaskStopped;
        }

        /// <summary>
        /// Allow component to look for upgrade tasks and launch them automatically.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
        }

        /// <summary>
        /// Disallow component from looking for upgrade tasks and launching them automatically.
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();
        }
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Called when a unit/building type is upgraded.
        /// </summary>
        /// <param name="upgrade">The Upgrade instance component that manages the faction entity upgrade.</param>
        /// <param name="targetID">The index of the target faction entity type that has been chosen as the upgrade target.</param>
        private void OnFactionEntityUpgraded (Upgrade upgrade, int targetID)
        {
            //does the building/unit belong to this NPC faction?
            if (upgrade.Source.FactionID == factionMgr.FactionID)
                RemoveUpgradeTask(upgrade.Source.GetCode());
        }

        /// <summary>
        /// Called when a new TaskLauncher instance is initiated.
        /// </summary>
        /// <param name="taskLauncher">The TaskLauncher instance that is added.</param>
        /// <param name="taskID">Ignore.</param>
        /// <param name="taskQueueID">Ignore.</param>
        private void OnTaskLauncherAdded(TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1)
        {
            if (taskLauncher.FactionEntity.FactionID == factionMgr.FactionID) //if the task launcher belongs to this faction
                AddTaskLauncher(taskLauncher);
        }

        /// <summary>
        /// Called when a TaskLauncher instance is destroyed.
        /// </summary>
        /// <param name="taskLauncher">The TaskLauncher instance that is removed.</param>
        /// <param name="taskID">Ignore.</param>
        /// <param name="taskQueueID">Ignore.</param>
        private void OnTaskLauncherRemoved(TaskLauncher taskLauncher, int taskID = -1, int taskQueueID = -1) //called when a task launcher has been removed
        {
            if (taskLauncher.FactionEntity.FactionID == factionMgr.FactionID) //if the task launcher belongs to this faction
                RemoveTaskLauncher(taskLauncher);
        }

        /// <summary>
        /// Called when a task is launched.
        /// </summary>
        /// <param name="taskLauncher">TaskLauncher instance where the task is launched.</param>
        /// <param name="taskID">Index of the task.</param>
        /// <param name="taskQueueID">Index of the task on pending queue.</param>
        private void OnTaskLaunched (TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            if (taskLauncher.FactionEntity.FactionID == factionMgr.FactionID //only if task launcher belongs to NPC faction
                && taskLauncher.GetTask(taskID).GetTaskType() == TaskTypes.upgrade) //if this is an upgrade task, then add the upgrade source code to the pending upgrades list
                pendingUpgrades.Add(taskLauncher.GetTask(taskID).GetUpgradeSourceCode());
        }

        /// <summary>
        /// Called when a task is either canceled or completed.
        /// </summary>
        /// <param name="taskLauncher">TaskLauncher instance where the task is canceled or completed.</param>
        /// <param name="taskID">Index of the task.</param>
        /// <param name="taskQueueID">Index of the task on pending queue.</param>
        private void OnTaskStopped (TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            if (taskLauncher.FactionEntity.FactionID == factionMgr.FactionID //only if task launcher belongs to NPC faction
                && taskLauncher.GetTask(taskID).GetTaskType() == TaskTypes.upgrade) //if this is an upgrade task, then add the upgrade source code to the pending upgrades list
                pendingUpgrades.RemoveAll(code => code == taskLauncher.GetTask(taskID).GetUpgradeSourceCode());
        }
        #endregion

        #region Manipulating Task Launchers
        /// <summary>
        /// Removes registered upgrade tasks that have been completed.
        /// </summary>
        /// <param name="sourceCode">Code of the faction entity that has been upgraded.</param>
        private void RemoveUpgradeTask (string sourceCode)
        {
            foreach (TaskLauncher tl in upgradeTasks.Keys.ToList()) //go through the registered task launchers
                //remove the upgrade tasks of the faction entity's ugprade
                upgradeTasks[tl] = upgradeTasks[tl].Where(task => task.GetUpgradeSourceCode() != sourceCode).ToList();
        }

        /// <summary>
        /// Adds a TaskLauncher instance's upgrade tasks.
        /// </summary>
        /// <param name="taskLauncher">The TaskLauncher instance to remove.</param>
        private void AddTaskLauncher (TaskLauncher taskLauncher)
        {
            //collect the upgrade tasks and add them as the value.
            upgradeTasks.Add(taskLauncher, taskLauncher.GetAll().Where(task => task.GetTaskType() == TaskTypes.upgrade).ToList());

            if(autoUpgrade)
                Activate(); //enable component if we can auto upgrade without an external upgrade request
        }

        /// <summary>
        /// Removes a TaskLauncher instance's registered upgrade tasks.
        /// </summary>
        /// <param name="taskLauncher">The TaskLauncher instance to remove.</param>
        private void RemoveTaskLauncher (TaskLauncher taskLauncher)
        {
            upgradeTasks.Remove(taskLauncher);
        }
        #endregion

        #region Launching Upgrades
        /// <summary>
        /// Runs the upgrade timer to launch upgrade tasks for the NPC faction.
        /// </summary>
        protected override void OnActiveUpdate()
        {
            base.OnActiveUpdate();

            //upgrade timer:
            if (upgradeTimer > 0)
                upgradeTimer -= Time.deltaTime;
            else //if the timer is through
            {
                upgradeTimer = upgradeReloadRange.getRandomValue(); //reload timer

                //go through the upgrade tasks if there are any:
                if (upgradeTasks.Count > 0)
                {
                    //go through each registerd task launcher and its upgrade type tasks
                    foreach (TaskLauncher taskLauncher in upgradeTasks.Keys)
                    {
                        foreach (FactionEntityTask upgradeTask in upgradeTasks[taskLauncher])
                            //and attempt to launch upgrade request
                            OnUpgradeLaunchRequest(taskLauncher, upgradeTask.ID, true);
                    }
                }
                else //no more upgrade tasks available? disable this comp
                    Deactivate();
            }
        }

        /// <summary>
        /// Launches upgrade tasks if all requirements are met.
        /// </summary>
        /// <param name="taskLauncher">The TaskLauncher instance that has the upgrade task.</param>
        /// <param name="taskID">The upgrade's task ID.</param>
        /// <param name="auto">True if this has been called from the NPCUpgradeManager instance, otherwise false</param>
        /// <returns></returns>
        public bool OnUpgradeLaunchRequest(TaskLauncher taskLauncher, int taskID, bool auto)
        {
            if (taskLauncher == null //if the task launcher is invalid
                || auto && !autoUpgrade //or NPCUpgradeManager instance called this but auto upgrading is disabled
                || !auto && !upgradeOnDemand //or this was called by another NPC component but upgrading on demand is disabled
                || pendingUpgrades.Contains(taskLauncher.GetTask(taskID).GetUpgradeSourceCode()) //or the source upgrade faction entity already has an ongoing upgrade
                || Random.value > acceptanceRange.getRandomValue() //or this is not accepted due to generated random value
                ) //do not proceed
                return false;

            //finally attempt to launch the task
            ErrorMessage addTaskMsg = gameMgr.TaskMgr.AddTask(new TaskUIAttributes {
                taskLauncher = taskLauncher,
                ID = taskID,
                type = TaskTypes.upgrade
            });

            //TO BE ADDED: Handling insufficient resources. 

            //if adding the upgrade task was successful, this would return true, if not then false.
            return addTaskMsg == ErrorMessage.none;
        }
        #endregion
    }
}
