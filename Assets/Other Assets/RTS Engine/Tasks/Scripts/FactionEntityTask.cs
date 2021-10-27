using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

using RTSEngine.EntityComponent;

namespace RTSEngine
{
    [System.Serializable]
    public class FactionEntityTask
    {
        public int ID { private set; get; }

        [SerializeField]
        private string code = "new_task_code"; //a unique code must be assigned to each task
        public string GetCode() { return code; }

        private FactionEntity factionEntity; //the faction ID that this task belongs to.

        [SerializeField]
        private FactionTypeInfo[] factionTypes = new FactionTypeInfo[0]; //when assigned, this faction entity task will only be available for faction types inside the array

        [SerializeField]
        private string description = "describe your task here"; //description shown in the task panel when hovering over the task button.
        public string GetDescription() { return description; }

        [SerializeField]
        private TaskTypes type = TaskTypes.createUnit; //the actual task type which the task manager will be referring to.
        public TaskTypes GetTaskType() { return type; }

#if UNITY_EDITOR
        public enum AllowedTaskTypes { createUnit, destroy, custom, upgrade, lockAttack };

        [SerializeField]
        private AllowedTaskTypes allowedType = AllowedTaskTypes.createUnit; //so that only allowed task types are entered in the inspector
#endif

        //UI related:
        [SerializeField]
        private int taskPanelCategory = 0; //if you are using different categories in the task panel then assign this for each task.
        public int GetTaskPanelCategory() { return taskPanelCategory; }

        [SerializeField]
        private Sprite icon = null; //the icon shown in the tasks panel
        public Sprite GetIcon() { return icon; }

        [SerializeField]
        private float reloadTime = 3.0f; //how long does the task last?
        public float GetReloadTime() { return reloadTime; }

        [SerializeField]
        private ResourceInput[] requiredResources = new ResourceInput[0]; //Resources required to complete this task.
        public bool HasRequiredResources() { return gameMgr.ResourceMgr.HasRequiredResources(requiredResources, factionEntity.FactionID); }
        public ResourceInput[] GetRequiredResources() { return requiredResources.Clone() as ResourceInput[]; }

        [SerializeField, Tooltip("How would the task's icon look in case requirements are not met?")]
        private MissingTaskRequirementData missingReqData = new MissingTaskRequirementData { color = Color.red, icon = null };
        /// <summary>
        /// Gets the color and icon defined to display that the player doesn't have the requirements to launch a task.
        /// </summary>
        /// <returns>MissedTaskRequirementsData struct instance where the icon and color are defined.</returns>
        public MissingTaskRequirementData GetMissingReqData () { return missingReqData; }

        //this resources will be added to the task launcher's faction when the task is completed
        [SerializeField]
        private ResourceInput[] completeResources = new ResourceInput[0];
        public ResourceInput[] GetCompleteResources() { return completeResources.Clone() as ResourceInput[]; }

        [SerializeField, Tooltip("What audio clip to play when the task is completed?")]
        private AudioClipFetcher completeAudio = new AudioClipFetcher(); //audio clip played when the task is completed.
        [SerializeField, Tooltip("What audio clip to play when a pending task is cancelled?")]
        private AudioClipFetcher cancelAudio = new AudioClipFetcher(); //audio clip played when this pending task is cancelled.
        [SerializeField, Tooltip("What audio clip to play when the task is launched?")]
        private AudioClipFetcher launchAudio = new AudioClipFetcher(); //audio clip played when this task is launched

        public enum UseMode { multiple, onceThisInstance, onceAllInstances } //use mode regarding this task
        //multiple -> task can be launched multiple times
        //onceThisInstance -> task can be only launched and completed once on the task launcher attached that controls this task
        //onceAllInstances -> task can be only launched and completed once on all instances that have the same task type
        [SerializeField]
        private UseMode useMode = UseMode.multiple;

        //state of the task: is it available to be launched?
        [SerializeField]
        private bool _isAvailable = true;
        public bool IsAvailable
        {
            get { return _isAvailable; }
            set { _isAvailable = value; }
        }

        //Unit creation:
        [SerializeField]
        private UnitCreationTaskAttributes unitCreationAttributes = new UnitCreationTaskAttributes(); //will be shown only in case the task type is a unit creation.
        //if this is a unit creation task, the unit's code and category are copied into the following properties
        public string UnitCode { private set; get; }
        public string UnitCategory { private set; get; }
        public int UnitPopulationSlots { private set; get; }

        //unit/building upgrade:
        [SerializeField]
        private Upgrade upgrade = null; //will be shown only in  case the task type is a unit upgrade task.
        public string GetUpgradeSourceCode() { return upgrade.Source.GetCode(); }
        [SerializeField]
        private int upgradeTargetID = 0; //the upgrade target ID, since a unit/building upgrade component allows multiple upgrade possibilities
        //when enabled, the upgrade will only affect the source instance only
        //for one instance upgrade to work, the task holder with the task launcher that includes the upgrade task must be the same upgrade source
        [SerializeField]
        private bool oneInstanceUpgrade = false;

        //unlocking attack types:
        [SerializeField]
        private string[] unlockAttackTypes = new string[0]; //all attack types attached to the entity with a code in this array will be unlocked when the task is completed
        [SerializeField]
        private string[] lockAttackTypes = new string[0]; //all attack types attached to the entity with a code in this array will be locked when the task is completed
        //the attack unlock/lock task works only on attack sources which have the MultipleAttackManager attached to them and it only affects the attack source instance
        [SerializeField]
        private bool switchAttack = false; //switch the attack type after this is done?
        [SerializeField]
        private string targetAttackType = "target_attack_code"; //if the above bool is true, the attack type with the following code will be enabled

        /// <summary>
        /// Allows to define a task using its code to lock/unlock it locally or globally in the game.
        /// </summary>
        [System.Serializable]
        public struct TaskLockStatus
        {
            [Tooltip("Code of the task to unlock or lock.")]
            public string code;
            [Tooltip("Enable to unlock the task, disable to lock it.")]
            public bool unlock;
            [Tooltip("Enable to lock/unlock the task for all task launchers of the faction in game or disable to update only tasks in this task launcher instance.")]
            public bool localOnly;
        }

        [SerializeField, Tooltip("Tasks to lock/unlock once this task is completed.")]
        private TaskLockStatus[] updateTasksOnComplete = new TaskLockStatus[0];

        //unlocking other tasks:
        [SerializeField]
        private string[] tasksToUnlock = new string[0]; //an array of task codes that get unlocked once the task is completed.
        [SerializeField]
        private string[] tasksToLock = new string[0]; //an array of task codes that get locked once the task is completed.

        //Events: Besides the custom delegate events, you can directly use the event triggers below to further customize the behavior of the tasks:
        [SerializeField]
        private UnityEvent launchEvent = null;
        [SerializeField]
        private UnityEvent startEvent = null;
        [SerializeField]
        private UnityEvent completeEvent = null;
        [SerializeField]
        private UnityEvent cancelEvent = null;

        //other components:
        GameManager gameMgr;

        //called to initiate the faction entity task
        public bool Init(GameManager gameMgr, FactionEntity factionEntity, int ID)
        {
            this.gameMgr = gameMgr;
            this.ID = ID;

            this.factionEntity = factionEntity; //assign the faction entity

            bool factionTypeMatch = false;

            foreach (FactionTypeInfo factionType in factionTypes) //go through the assigned faction types (if any are assigned)
                if (factionType == gameMgr.GetFaction(factionEntity.FactionID).GetTypeInfo()) //if the faction code matches
                {
                    factionTypeMatch = true; //found the faction type here.
                    break;
                }

            if (factionTypes.Length > 0 && factionTypeMatch == false) //if there are faction types assigned and the faction type inited does not match
                return false; //false -> asks the task launcher to remove the task from the list so it can't be used

            //continue since this task can be used with the given faction type, but now check if it's available or not
            IsAvailable = this.gameMgr.TaskMgr.Status.IsTaskEnabled(code, factionEntity.FactionID, IsAvailable);

            reloadTime /= gameMgr.GetSpeedModifier(); //apply the speed modifier on the reload time

            if(type == TaskTypes.createUnit)
            {
                //set the population slots, unit code and category properties
                UnitPopulationSlots = unitCreationAttributes.prefabs[0].GetPopulationSlots();
                UnitCode = unitCreationAttributes.prefabs[0].GetCode();
                UnitCategory = unitCreationAttributes.prefabs[0].GetCode();
            }

            return true;
        }

        //is this task enabled?
        public bool IsEnabled()
        {
            return gameMgr.TaskMgr.Status.IsTaskEnabled(code, factionEntity.FactionID, IsAvailable);
        }

        //called to launch the task:
        public bool Launch()
        {
            if (factionEntity.FactionID == GameManager.PlayerFactionID && launchAudio != null) //if this is the local player faction ID and there a launch audio
                gameMgr.AudioMgr.PlaySFX(launchAudio.Fetch(), false); //Play the audio clip

            gameMgr.ResourceMgr.UpdateRequiredResources(requiredResources, false, factionEntity.FactionID); //take required resources

            if (type == TaskTypes.createUnit)
            {
                gameMgr.GetFaction(factionEntity.FactionID).UpdateCurrentPopulation(UnitPopulationSlots); //update population slots
                factionEntity.FactionMgr.UpdateLimitsList(UnitCode, UnitCategory, true);
            }

            launchEvent.Invoke(); //invoke the task launch methods

            //task can only be used once
            if (useMode != UseMode.multiple)
            {
                IsAvailable = false; //make it unavailable.
                if (useMode == UseMode.onceAllInstances) //if this was marked as usable once for all instances.
                    gameMgr.TaskMgr.Status.ToggleTask(code, factionEntity.FactionID, false);
            }

            return false;
        }

        /// <summary>
        /// Called when the FactionEntityTask instance is the first in the task launcher's queue and has started its timer.
        /// </summary>
        public void Start()
        {
            startEvent.Invoke();
        }

        //update task when it's upgraded:
        public void Update(string upgradedUnitCode, Unit targetUnitPrefab, Upgrade.NewTaskInfo newTaskInfo)
        {
            //does the current task create the unit that is getting upgraded
            if (type == TaskTypes.createUnit && unitCreationAttributes.prefabs[0].GetCode() == upgradedUnitCode)
            {
                //update task info:
                unitCreationAttributes.prefabs.Clear();
                unitCreationAttributes.prefabs.Add(targetUnitPrefab);

                description = newTaskInfo.description;
                icon = newTaskInfo.icon;
                reloadTime = newTaskInfo.reloadTime;
                requiredResources = newTaskInfo.newResources.Clone() as ResourceInput[];
            }

        }

        //a method called to allow this task to take effect
        public void Complete()
        {
            if (factionEntity.FactionID == GameManager.PlayerFactionID && completeAudio != null) //if this is the local player faction ID and there's task completed audio
                gameMgr.AudioMgr.PlaySFX(completeAudio.Fetch(), false); //Play the audio clip

            completeEvent.Invoke(); //invoke the complete unity event.

            switch (type) //type of the task that has been completed.
            {
                case TaskTypes.createUnit:
                    //Randomly pick a prefab to produce from the list
                    Unit unitPrefab = unitCreationAttributes.prefabs[Random.Range(0, unitCreationAttributes.prefabs.Count)];

                    Vector3 spawnPosition = factionEntity.transform.position;
                    Building createdBy = null;

                    //get the unit spawn position:
                    if (factionEntity.Type == EntityTypes.building) //if this is a building, then see if it has a dedicated spawn position
                    {
                        createdBy = (Building)factionEntity;
                        spawnPosition = createdBy.GetSpawnPosition(unitPrefab.GetComponent<NavMeshAgent>().areaMask);
                    }

                    gameMgr.UnitMgr.CreateUnit(unitPrefab, spawnPosition, unitPrefab.transform.rotation, spawnPosition, factionEntity.FactionID, createdBy, false, false); //finally create the unit

                    break;

                case TaskTypes.destroy:
                    factionEntity.EntityHealthComp.DestroyFactionEntity(false); //destroy the faction entity
                    break;

                case TaskTypes.upgrade:
                    gameMgr.UpgradeMgr.LaunchUpgrade(upgrade, upgradeTargetID, factionEntity, oneInstanceUpgrade); //launch unit upgrade
                    break;

                case TaskTypes.lockAttack:

                    //unlock attack types:
                    foreach (string attackCode in unlockAttackTypes)
                        foreach (AttackEntity attackEntity in factionEntity.AllAttackComp)
                            if (attackCode == attackEntity.GetCode())
                                attackEntity.IsLocked = false;

                    //lock attack types.
                    foreach (string attackCode in lockAttackTypes)
                        foreach (AttackEntity attackEntity in factionEntity.AllAttackComp)
                            if (attackCode == attackEntity.GetCode())
                                attackEntity.IsLocked = true;

                    //switch to another attack type?
                    if (switchAttack && factionEntity.MultipleAttackMgr)
                        factionEntity.MultipleAttackMgr.SetTargetLocal(targetAttackType);

                    break;

            }

            foreach(TaskLockStatus tls in updateTasksOnComplete) //go through the tasks that need to be updated when this one is completed
            {
                if (tls.localOnly) //update task in this task launcher only
                {
                    if (factionEntity.TaskLauncherComp.GetTask(tls.code) != null) //only if the task exists on the same task launcher.
                        factionEntity.TaskLauncherComp.GetTask(tls.code).IsAvailable = tls.unlock;
                }
                else //update all tasks of same code in all faction's task launchers
                    gameMgr.TaskMgr.Status.ToggleTask(tls.code, factionEntity.FactionID, tls.unlock);
            }

            //add the complete resourcs to th task launcher's faction
            gameMgr.ResourceMgr.UpdateRequiredResources(completeResources, true, factionEntity.FactionID);
        }

        //cancel an in progress task of this type
        public void Cancel()
        {
            switch (type)
            {
                case TaskTypes.createUnit:

                    //update the population slots
                    gameMgr.GetFaction(factionEntity.FactionID).UpdateCurrentPopulation(-UnitPopulationSlots);

                    //update the limits list:
                    factionEntity.FactionMgr.UpdateLimitsList(UnitCode, UnitCategory, false);
                    break;
            }

            gameMgr.ResourceMgr.UpdateRequiredResources(requiredResources, true, factionEntity.FactionID); //Give back the task resources.

            cancelEvent.Invoke(); //trigger unity event.

            //if the task was supposed to be used once but is cancelled:
            if (useMode != UseMode.multiple)
            {
                IsAvailable = true; //make it available again.
                if (useMode == UseMode.onceAllInstances) //if this was marked as usable once for all instances.
                    gameMgr.TaskMgr.Status.ToggleTask(code, factionEntity.FactionID, true);
            }

            if (factionEntity.FactionID == GameManager.PlayerFactionID && factionEntity.GetSelection().IsSelected) //if this is the local player and the faction entity is selected
                gameMgr.AudioMgr.PlaySFX(cancelAudio.Fetch(), false);
        }
    }
}
