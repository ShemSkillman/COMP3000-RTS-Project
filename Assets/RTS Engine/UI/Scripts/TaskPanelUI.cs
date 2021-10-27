using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

using UnityEngine.Assertions;

using RTSEngine.EntityComponent;

/* TaskPanelUI script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [System.Serializable]
    public class TaskPanelUI
    {
        [SerializeField]
        private TaskUI taskUIPrefab = null; //the main task UI prefab from which all task buttons will be created.

        [System.Serializable]
        public struct Category
        {
            public GridLayoutGroup parent;
            [Min(1)]
            public int preCreatedAmount;
        }
        [SerializeField]
        private Category[] taskPanelCategories = new Category[0];

        //Task panel categories:
        //[SerializeField]
        //private GridLayoutGroup[] taskPanelCategories = new GridLayoutGroup[0]; //a list of grid layout groups that present task panel categories.
        //if you want to have one task panel category then have one element only in the array.
        //the ID of each task panel category is its index in this array

        //In progress tasks:
        [SerializeField]
        private GridLayoutGroup inProgressTaskPanel = null;


        //tasks attributes used for each task panel category and the multiple selection panel
        public class TaskList
        {
            public List<TaskUI> all; //all created TaskUI instances are stored in this list
            public Transform parent; //the parent transform of all tasks in the list
        }
        private TaskList[] taskLists = new TaskList[0]; //each task panel has its own list of tasks.

        private bool isLocked = false; //if true, the task panel can not be updated.

        //Holds the active tasks of the IEntityComponent organized by their unique codes.
        private Dictionary<string, EntityComponentTaskUITracker> componentTasks = new Dictionary<string, EntityComponentTaskUITracker>();

        //initilaize the task panel UI component
        GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            //initialise the task list for task panel categories, pending tasks and multiple selection:
            taskLists = new TaskList[taskPanelCategories.Length+2];
            for (int i = 0; i < taskLists.Length; i++)
            {
                taskLists[i] = new TaskList()
                {
                    all = new List<TaskUI>(),
                    //set the parent of the tasks list (either one of the task panel categories or pending task) or the mutiple selection panel for the last task list
                    parent = (i < taskPanelCategories.Length)
                    ? taskPanelCategories[i].parent.transform
                    : ((i == taskLists.Length - 2)
                        ? inProgressTaskPanel.transform
                        : gameMgr.UIMgr.GetMultipleSelectionPanel().transform)
                };

                if (i >= taskPanelCategories.Length)
                    continue;

                //for the task panel categories, add the pre-created slots
                while (taskLists[i].all.Count < taskPanelCategories[i].preCreatedAmount)
                    CreateTask(taskLists[i]);
            }

            isLocked = false; //by default, the task panel is not locked.

            //custom events to update/hide UI elements:
            CustomEvents.UnitWanderToggled += OnUnitWanderToggled;

            CustomEvents.APCAddUnit += OnAPCUpdated;
            CustomEvents.APCRemoveUnit += OnAPCUpdated;

            CustomEvents.BuildingPlaced += OnBuildingPlacementStopped;
            CustomEvents.BuildingStopPlacement += OnBuildingPlacementStopped;
            CustomEvents.BuildingStartPlacement += OnBuildingPlacementStarted;
            BuildingPlacement.PlacementDenied += OnBuildingPlacementDenied;

            CustomEvents.TaskLaunched += OnTaskLauncherStatusUpdated;
            CustomEvents.TaskCanceled += OnTaskLauncherStatusUpdated;
            CustomEvents.TaskCompleted += OnTaskLauncherStatusUpdated;

            CustomEvents.EntityComponentTaskReloadRequest += OnEntityComponentTaskReloadRequest;

            CustomEvents.FactionResourceUpdated += OnFactionResourceUpdated;
        }

        //called to disable this component
        public void Disable ()
        {
            //stop listening to the custom events
            CustomEvents.UnitWanderToggled -= OnUnitWanderToggled;

            CustomEvents.APCAddUnit -= OnAPCUpdated;
            CustomEvents.APCRemoveUnit -= OnAPCUpdated;

            CustomEvents.BuildingPlaced -= OnBuildingPlacementStopped;
            CustomEvents.BuildingStopPlacement -= OnBuildingPlacementStopped;
            CustomEvents.BuildingStartPlacement -= OnBuildingPlacementStarted;
            BuildingPlacement.PlacementDenied -= OnBuildingPlacementDenied;

            CustomEvents.TaskLaunched -= OnTaskLauncherStatusUpdated;
            CustomEvents.TaskCanceled -= OnTaskLauncherStatusUpdated;
            CustomEvents.TaskCompleted -= OnTaskLauncherStatusUpdated;

            CustomEvents.EntityComponentTaskReloadRequest -= OnEntityComponentTaskReloadRequest;

            CustomEvents.FactionResourceUpdated -= OnFactionResourceUpdated;
        }

        //called each time a unit wandering behavior is toggled
        private void OnUnitWanderToggled (Unit unit)
        {
            //show wander tasks only if this one unit is selected
            if (SelectionManager.IsSelected(unit.GetSelection(), true, true))
                Update();
        }

        //called each time a unit is added/removed to/from an APC
        private void OnAPCUpdated (APC apc, Unit unit)
        {
            //show APC tasks only if the apc is the only entity selected
            if (SelectionManager.IsSelected(apc.FactionEntity.GetSelection(), true, true))
                Update();
        }

        //called each time a building placement stops or when a building is placed
        private void OnBuildingPlacementStopped (Building building)
        {
            if (building.FactionID == GameManager.PlayerFactionID) //if the building belongs to the local player
            {
                isLocked = false;
                Update(); //update tasks to re-display builder units tasks
            }
        }

        //called each time a building placement starts
        private void OnBuildingPlacementStarted (Building building)
        {
            if (building.FactionID == GameManager.PlayerFactionID) //if this is the player faction
            {
                gameMgr.UIMgr.HideTooltip();
                Hide();
                isLocked = true;
            }
        }

        //called each time a building placement starts
        /// <summary>
        /// Called each time a building placement request is denied (due to insufficient resources for example).
        /// </summary>
        /// <param name="factionID">ID of the faction for which the placement request is denied.</param>
        /// <param name="building">Building prefab instance for which a placement attempt has been made.</param>
        private void OnBuildingPlacementDenied (int factionID, Building building)
        {
            if (factionID == GameManager.PlayerFactionID) //if this is the player faction
            {
                isLocked = false;
                Update();
            }
        }

        //called each time a task launcher status is updated (task added, cancelled or completed)
        private void OnTaskLauncherStatusUpdated (TaskLauncher taskLauncher, int taskID, int taskQueueID)
        {
            //only show the task launcher tasks if the task launcher is the only player entity selected
            if(SelectionManager.IsSelected(taskLauncher.FactionEntity.GetSelection(), true, true))
            {
                Update();

                if (taskLauncher.GetTask(taskID).IsAvailable == false) //if the task is no longer available
                    gameMgr.UIMgr.HideTooltip(); //hide the tooltip
            }
        }

        /// <summary>
        /// Called each time a IEntityComponent instance requires to refresh its active task on the panel.
        /// </summary>
        /// <param name="componentTask"></param>
        /// <param name="taskCode"></param>
        private void OnEntityComponentTaskReloadRequest (IEntityComponent component, string taskCode)
        {
            if (!isLocked && component.Entity.GetSelection().IsSelected) //as long as the panel is not locked and the source entity is selected
                UpdateEntityComponentTasks(component);
        }

        /// <summary>
        /// Called whenever a faction's resources are updated.
        /// </summary>
        /// <param name="resourceType">ResourceTypeInfo instance that defines the resource type that got updated.</param>
        /// <param name="factionID">The ID of the faction that had its resources updated.</param>
        /// <param name="amount">Amount added (if value is positive) or removed (if value is negative) to the faction's resources.</param>
        private void OnFactionResourceUpdated (ResourceTypeInfo resourceType, int factionID, int amount)
        {
            if (factionID == GameManager.PlayerFactionID) //if the player faction's resources are updated
                //task launcher or building placement tasks might need to be updated if their requirements are just met or no longer met.
                Update();
        }

        #region Adding/Removing Tasks
        /// <summary>
        /// Adds or gets a task in the requested task panel category.
        /// </summary>
        /// <param name="categoryID">ID of the task panel category.</param>
        /// <param name="type">Type of the task to add.</param>
        /// <returns>TaskUI instance of the added task.</returns>
        public TaskUI AddTask (int categoryID, TaskUI.Types type, bool forceIndex = false, int index = 0)
        {
            //if the task type is multiple selection, get the last element in the tasks list array, else get the task lists category index.
            TaskList currTaskList = (type == TaskUI.Types.multipleSelectionIndiv || type == TaskUI.Types.multipleSelectionMul) ? taskLists[taskLists.Length - 1]
                : ((type == TaskUI.Types.inProgress) ? taskLists[taskLists.Length - 2] : taskLists[categoryID]);

            if(forceIndex) //if we want to get a specific task slot from the panel category
            {
                if (index >= 0 && index < currTaskList.all.Count
                    && !currTaskList.all[index].enabled)
                    return currTaskList.all[index];
                else
                {
                    Debug.LogError($"[TaskPanelUI] Requested task slot of index {index} in task panel category {categoryID} is either invalid or already being used!");
                    return null;
                }

            }

            int i = 0;
            while(i < currTaskList.all.Count)
            {
                if (!currTaskList.all[i].enabled)
                    return currTaskList.all[i];
                i++;
            }

            return CreateTask(currTaskList);
        }

        public TaskUI CreateTask (TaskList taskList)
        {
            TaskUI nextTask = Object.Instantiate(taskUIPrefab.gameObject).GetComponent<TaskUI>(); //else just create and init new task UI

            nextTask.Init(gameMgr);
            taskList.all.Add(nextTask); //add a new task to the list

            nextTask.transform.SetParent(taskList.parent, true); //set its parent
            nextTask.transform.localScale = Vector3.one;

            return nextTask;
        }

        public TaskUI Add (TaskUIAttributes attributes, int categoryIndex, TaskUI.Types type = TaskUI.Types.idle)
        {
            TaskUI nextTask = AddTask(categoryIndex, type);

            nextTask.Reload(attributes, type); //initialize the task.

            return nextTask;
        }

        //a method that hides all task panel and in progress task panel tasks or the multiple selection tasks
        public void Hide(bool multipleSelection = false)
        {
            //disable all component tasks if we're hiding the task panel:
            if (!multipleSelection)
                componentTasks.Clear();

            //determine the start and finish values of the for loop counter depending on whether we want to hide multiple selection or normal tasks
            //multiple selection -> only last element of the taskLists array
            //task panel tasks/in progress tasks -> rest of the elements
            int start = (multipleSelection) ? taskLists.Length-1 : 0;
            int finish = (multipleSelection) ? taskLists.Length : taskLists.Length - 1;
            for (int i = start; i < finish; i++)
            {
                foreach (TaskUI task in taskLists[i].all)
                    if (task.enabled) //if not already disabled
                        task.Disable(); //hide task
            }
        }
        #endregion

        //update the task panel:
        public void Update ()
        {
            if (isLocked == true) //can not update the task panel if it is locked
                return;

            Hide(); //hide currently active tasks

            foreach (Entity entity in gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.none, true, true))
                UpdateAllEntityComponentTasks(entity);

            List<Unit> selectedUnits = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.unit, false, true).Cast<Unit>().ToList(); //get selected units from player faction
            List<Building> selectedBuildings = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.building, false, true).Cast<Building>().ToList(); //get selected buildings from player faction

            if(selectedUnits.Count + selectedBuildings.Count == 1) //if only one faction entity is selected
            {
                FactionEntity factionEntity = selectedUnits.Count == 1 ? selectedUnits[0] as FactionEntity : selectedBuildings[0] as FactionEntity; //get it

                if (factionEntity.EntityHealthComp.IsDead() == true) //if dead, then do not show any tasks
                    return;

                UpdateAPCTasks(factionEntity, factionEntity.APCComp); //show APC tasks only if one faction entity is selected
                UpdateTaskLauncherTasks(factionEntity, factionEntity.TaskLauncherComp); //show task launcher tasks only if one faction entity is selected
            }

            if(selectedUnits.Count > 0) //units are selected
            {
                //see if all selected units have the following components
                Builder builderComp = selectedUnits[0].BuilderComp;
                ResourceCollector collectorComp = selectedUnits[0].CollectorComp;
                Healer healerComp = selectedUnits[0].HealerComp;
                Converter converterComp = selectedUnits[0].ConverterComp;

                //make sure all selected units have the same components
                foreach(Unit u in selectedUnits)
                {
                    if (u.HealthComp.IsDead() == true) //if one of the unit is dead
                        return; //do not continue

                    if (u.BuilderComp == null)
                        builderComp = null;
                    if (u.CollectorComp == null)
                        collectorComp = null;
                    if (u.HealerComp == null)
                        healerComp = null;
                    if (u.ConverterComp == null)
                        converterComp = null;
                }

                if (builderComp != null)
                {
                    UpdateUnitComponentTask(selectedUnits[0], builderComp.taskUI, TaskTypes.build);

                    if(selectedBuildings.Count == 0) //only if no buildings are selected can we show the buildings to place
                        UpdateBuilderTasks(selectedUnits[0], builderComp);
                }
                if (collectorComp != null)
                    UpdateUnitComponentTask(selectedUnits[0], collectorComp.taskUI, TaskTypes.collectResource);
                if (healerComp != null)
                    UpdateUnitComponentTask(selectedUnits[0], healerComp.taskUI, TaskTypes.heal);
                if (converterComp != null)
                    UpdateUnitComponentTask(selectedUnits[0], converterComp.taskUI, TaskTypes.convert);

                if (selectedUnits.Count == 1 && selectedBuildings.Count == 0) //if there's only one unit and no buildings selected
                    UpdateWanderTasks(selectedUnits[0], selectedUnits[0].WanderComp);
            }
        }

        public void UpdateAPCTasks (FactionEntity sourceEntity, APC APCComp)
        {
            if (sourceEntity == null || APCComp == null)
                return;

            if (APCComp.IsEmpty() == false) //if there are units stored inside the APC
            {
                if (APCComp.CanEject(true) == true) //if we're allowed to eject all units at once
                    Add(new TaskUIAttributes {
                        type = TaskTypes.APCEjectAll,
                        source = sourceEntity,
                        icon = APCComp.GetEjectAllUnitsIcon() },
                        APCComp.GetEjectTaskCategory(true));

                if (APCComp.CanEject(false) == true && APCComp.GetCount() > 0) //if we're allowed to eject single units and there are actual units stored.
                    for (int unitID = 0; unitID < APCComp.GetCount(); unitID++)
                        Add(new TaskUIAttributes
                        {
                            ID = unitID,
                            source = sourceEntity,
                            type = TaskTypes.APCEject,
                            icon = APCComp.GetStoredUnit(unitID).GetIcon()
                        },
                        APCComp.GetEjectTaskCategory(false));
            }
            if (APCComp.IsFull() == false && APCComp.CanCallUnits() == true) //if there are still free slots and the APC can call units
                Add(new TaskUIAttributes
                {
                    type = TaskTypes.APCCall,
                    source = sourceEntity,
                    icon = APCComp.GetCallUnitsIcon()
                },
                 APCComp.GetCallUnitsTaskCategory());
        }

        public void UpdateTaskLauncherTasks (FactionEntity sourceEntity, TaskLauncher taskLauncher)
        {
            if (taskLauncher == null || taskLauncher.Initiated == false || taskLauncher.GetTasksCount() == 0) //if the task launcher is invalid or the source can't manage a task
                return;
            
            for(int taskID = 0; taskID < taskLauncher.GetTasksCount(); taskID++) //go through all tasks
            {
                FactionEntityTask task = taskLauncher.GetTask(taskID);
                if (task.IsEnabled() == true)
                {
                    //the task is only launchable if the faction has enough resources to launch it
                    bool launchable = task.HasRequiredResources()
                        //and if it's a unit creation task, make sure that the faction limit has not been reached.
                        && (task.GetTaskType() != TaskTypes.createUnit || !sourceEntity.FactionMgr.HasReachedLimit(task.UnitCode, task.UnitCategory));

                    TaskUI taskUI = Add(new TaskUIAttributes
                    {
                        ID = taskID,
                        type = task.GetTaskType(),
                        icon = !launchable && task.GetMissingReqData().icon != null ? task.GetMissingReqData().icon : task.GetIcon(),
                        source = sourceEntity,
                        taskLauncher = taskLauncher,
                        //set the color to the original one of the sprite if the task is launchable or it isn't and 
                        color = launchable || task.GetMissingReqData().icon != null ? Color.white : task.GetMissingReqData().color

                    }, task.GetTaskPanelCategory());

                }
            }

            UpdateInProgressTasks(taskLauncher); //show the in progress tasks
        }

        #region IEntityComponent Task Handling
        /// <summary>
        /// Goes through the IEntityComponent components attached to an entity and attempts to display their active tasks.
        /// </summary>
        /// <param name="entity">Entity instance that has the IEntityComponent components attached to it.</param>
        private void UpdateAllEntityComponentTasks (Entity entity)
        {
            foreach (IEntityComponent entityComponent in entity.ComponentTasks)
                UpdateEntityComponentTasks(entityComponent);
        }

        /// <summary>
        /// Update displaying tasks produced by one IEntityComponent component.
        /// </summary>
        /// <param name="entityComponent">Instance that implements IEntityComponent interface whose tasks will be updated.</param>
        private void UpdateEntityComponentTasks (IEntityComponent entityComponent)
        {
            entityComponent.OnTaskUIRequest(out IEnumerable<TaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes); //request the active tasks from each component

            if (disabledTaskCodes != null)
                foreach (string code in disabledTaskCodes) //the IEntityComponent reports the codes of the tasks to be disabled here.
                    DisableEntityComponentTask(code);

            if (taskUIAttributes != null)
                foreach (TaskUIAttributes attr in taskUIAttributes) //attempt to add them to the task panel
                    AddEntityComponentTask(entityComponent, attr);
        }

        /// <summary>
        /// Attempts to draw a new task on the panel for a IEntityComponent component.
        /// </summary>
        /// <param name="component">IEntityComponent instance to add a task for.</param>
        /// <param name="attributes">Attributes of the task to add.</param>
        public void AddEntityComponentTask (IEntityComponent component, TaskUIAttributes attributes)
        {
            switch(attributes.entityComp.displayType) //depending on the display type of the task to add, check the fail conditions:
            {
                case EntityComponentTaskUI.DisplayType.heteroMultipleSelection:

                    if (!component.Entity.GetSelection().IsSelected)
                    {
                        DisableEntityComponentTask(attributes.entityComp.code);
                        return;
                    }
                    break;

                case EntityComponentTaskUI.DisplayType.singleSelection:

                    if (!component.Entity.GetSelection().IsSelectedOnly)
                    {
                        DisableEntityComponentTask(attributes.entityComp.code);
                        return;
                    }
                    break;

                case EntityComponentTaskUI.DisplayType.homoMultipleSelection:

                    if (gameMgr.SelectionMgr.Selected.GetEntitiesList(component.Entity.Type, true, true).Count == 0)
                    {
                        DisableEntityComponentTask(attributes.entityComp.code);
                        return;
                    }
                    break;
            }

            //at this point, the fail conditions are checked and we are allowed to move on to adding/refreshing the task:

            //see if there's a tracker already for the task:
            if (componentTasks.TryGetValue(attributes.entityComp.code, out EntityComponentTaskUITracker nextTracker))
                //there's one already, then refresh the task and update the tracked components
                nextTracker.AddComponent(component);
            else //if there's no tracker already, create and init one:
            {
                //create a new task:
                TaskUI newTask = AddTask(attributes.entityComp.panelCategory, TaskUI.Types.entityComponent, attributes.entityComp.forceSlot, attributes.entityComp.slotIndex);
                //create a new tracker for the task:
                nextTracker = new EntityComponentTaskUITracker(component, newTask, attributes.entityComp.panelCategory);

                //add the new tracker to the dictionary:
                componentTasks.Add(attributes.entityComp.code, nextTracker);
            }

            //refresh the task:
            nextTracker.ReloadTask(attributes);
        }

        /// <summary>
        /// Disables an active EntityComponentTaskUITracker instance that tracks a task.
        /// </summary>
        /// <param name="taskCode">Code of the task to be disabled..</param>
        /// <returns>True if a tracker is successfully found and removed, otherwise false.</returns>
        public bool DisableEntityComponentTask (string taskCode)
        {
            //see if there's an active tracker that tracks the task with the given attributes.
            if (componentTasks.TryGetValue(taskCode, out EntityComponentTaskUITracker tracker))
            {
                tracker.Disable();

                componentTasks.Remove(taskCode);

                return true;
            }
            return false;
        }
        #endregion

        public void UpdateInProgressTasks (TaskLauncher taskLauncher)
        {
            if (taskLauncher == null || taskLauncher.GetTaskQueueCount() == 0) //if the task launcher is invalid or there are no tasks in the queue
                return;

            for(int progressTaskID = 0; progressTaskID < taskLauncher.GetTaskQueueCount(); progressTaskID++)
            {
                Add(new TaskUIAttributes
                {
                    ID = progressTaskID,
                    type = TaskTypes.cancelPendingTask,
                    taskLauncher = taskLauncher,
                    source = taskLauncher.FactionEntity,
                    icon = taskLauncher.GetPendingTaskIcon(progressTaskID)
                }, 0, TaskUI.Types.inProgress);
            }
        }

        public void UpdateUnitComponentTask (FactionEntity sourceEntity, EntityComponentTaskUI componentTaskUI, TaskTypes type)
        {
            if (componentTaskUI.enabled == false)
                return;

            Add(new TaskUIAttributes
            {
                source = sourceEntity,
                type = type,
                icon = componentTaskUI.icon,
                unitComponentTask = true
            },  componentTaskUI.panelCategory);
        }

        public void UpdateBuilderTasks (Unit sourceUnit, Builder builderComp)
        {
            if (sourceUnit == null || builderComp == null)
                return;

            int buildingID = -1;
            foreach(Building building in gameMgr.PlacementMgr.GetBuildings()) //go through all the placeable buildings 
            { 
                buildingID++;

                if (!builderComp.CanPlaceBuilding(building)) //if the next building can't be constructed by the selected builders
                    continue; //move to then next one

                //the task is only launchable if the building can be placed.
                bool placable = gameMgr.PlacementMgr.CanPlaceBuilding(building, false);

                TaskUI taskUI = Add(new TaskUIAttributes
                {
                    ID = buildingID,
                    icon = !placable && building.GetMissingReqData().icon != null ? building.GetMissingReqData().icon : building.GetIcon(),
                    source = sourceUnit,
                    type = TaskTypes.placeBuilding,
                    //´set the color of the icon depending on whether the building is placable or not
                    color = placable || building.GetMissingReqData().icon != null ? Color.white : building.GetMissingReqData().color
                },  building.GetTaskPanelCategory());

            }
        }

        public void UpdateWanderTasks (Unit sourceUnit, Wander wanderComp)
        {
            if (sourceUnit == null || wanderComp == null)
                return;

            Add(new TaskUIAttributes
            {
                type = TaskTypes.toggleWander,
                icon = wanderComp.GetIcon(),
                source = sourceUnit
            },  wanderComp.GetTaskPanelCategory());
        }
    }
}
