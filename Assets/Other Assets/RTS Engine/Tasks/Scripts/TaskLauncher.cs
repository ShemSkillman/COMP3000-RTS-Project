using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.AI;
using UnityEngine.Events;

/* Task Launcher script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    [RequireComponent(typeof(FactionEntity))]
    public class TaskLauncher : MonoBehaviour
    {

#if UNITY_EDITOR
        //used to keep track of the custom editor tabs
        public int editorElementID = 0;
        public int editorTabID = 0;
#endif

        [SerializeField]
        private bool isActive = true; //is the task launcher component active?
        public bool IsActive () { return isActive; }
        public bool Initiated { private set; get; } //has the task launcher been initiated?

        [SerializeField]
        private string code = "new_task_launcher"; //each task launcher must have a unique code
        public string GetCode() { return code; }

        public FactionEntity FactionEntity {private set; get;} //the faction entity component attached to the same game object

        [SerializeField, Tooltip("Tasks that this task launcher can start.")]
        private List<FactionEntityTask> tasksList = new List<FactionEntityTask>();
        //a dictionary of the task launcher's task where each task's code is the key and the actual task instance is the value.
        private Dictionary<string, FactionEntityTask> tasksDic = new Dictionary<string, FactionEntityTask>();

        public int GetTasksCount () { return tasksDic.Count; }
        public IEnumerable<FactionEntityTask> GetAll () { return tasksDic.Values; }
        public FactionEntityTask GetTask (int index) {
            if (index < 0 || index >= tasksList.Count)
                return null;
            return tasksList[index]; }

        /// <summary>
        /// Gets the FactionEntityTask instance of task of a certain code.
        /// </summary>
        /// <param name="code">Code of the task to get.</param>
        /// <returns>FactionEntityTask instance that manages the task with the given code.</returns>
        public FactionEntityTask GetTask (string code)
        {
            if (tasksDic.TryGetValue(code, out FactionEntityTask task))
                return task;

            return null;
        }

        [SerializeField]
        private int minHealth = 70; //minimum health required in order to launch/complete a task. 
        public int GetMinHealth () { return minHealth; }

        [SerializeField]
        private int maxTasks = 4; //The maximum amount of tasks that this component can handle at the same time.
        public int GetMaxTasksAmount() { return maxTasks; }
        
        private List<int> tasksQueue = new List<int>(); //this is the task's queue which holds all the pending tasks indexes
        public int GetTaskQueueCount () { return tasksQueue.Count; }
        public Sprite GetPendingTaskIcon (int ID) //gets the icon of the in progress task
        {
            if (ID < 0 || ID >= tasksQueue.Count)
                return null;

            return tasksList[tasksQueue[ID]].GetIcon();
        }
        public float GetPendingTaskProgress (int ID) { //gets the progress of a pending task.
            if (ID < 0 || ID >= tasksQueue.Count)
                return 0.0f;

            return 1.0f - taskQueueTimer / tasksList[tasksQueue[ID]].GetReloadTime();
        }

        private float taskQueueTimer = 0.0f; //this is the task's timer. when it's done, one task out of the queue is done.

        [SerializeField]
        private AudioClip launchDeclinedAudio = null; //Audio clip played when launching a task is declined.
        public AudioClip GetLaunchDeclinedAudio() { return launchDeclinedAudio; }

        //other components
        GameManager gameMgr;

        //initialize the task launcher component
        public void Init(GameManager gameMgr, FactionEntity factionEntity)
        {
            ///assign the components
            this.gameMgr = gameMgr;
            FactionEntity = factionEntity;

            for (int i = 0; i < tasksList.Count; i++) //init the tasks
            {
                if (tasksList[i].Init(gameMgr, FactionEntity, i)) //if the task successfully initializes then it can be used by the task launcher:
                    tasksDic.Add(tasksList[i].GetCode(), tasksList[i]);
            }

            CustomEvents.OnTaskLauncherAdded(this); //trigger the custom event

            Initiated = true;
        }

        public void Disable ()
        {
            if (!Initiated)
                return;

            //Launch the delegate event:
            CustomEvents.OnTaskLauncherRemoved(this);

            //If there are pending tasks, stop them and give the faction back the resources of these tasks:
            CancelAllInProgressTasks();
        }

        //a method to determine whether the task holder is capable of launching a new task/updating a pending task
        public bool CanManageTask ()
        {
            if (FactionEntity.Type == EntityTypes.building) //if the task holder is a building
                if (((Building)FactionEntity).IsBuilt == false) //and the building hasn't been constructed yet 
                    return false; //can't manage tasks.
            
            //for both units and buildings, check if they're not dead and that they have enough health to proceed.
            return !FactionEntity.EntityHealthComp.IsDead() && FactionEntity.EntityHealthComp.CurrHealth >= minHealth;
        }

        //called to add a task to the queue:
        public void Add (int taskID)
        {
            if (taskID < 0 || taskID >= tasksList.Count) //invalid task ID? 
                return;

            tasksQueue.Add(taskID); //add the new task to the queue

            //if the task queue of the task launcher was empty
            if (tasksQueue.Count == 1)
                StartNextTask(); //start the task instantly

            tasksList[taskID].Launch(); //launch actual task

            CustomEvents.OnTaskLaunched(this, taskID, tasksQueue.Count - 1); //trigger custom delegate event
        }

        /// <summary>
        /// Starts the first task in the pending tasks queue.
        /// </summary>
        public void StartNextTask ()
        {
            if (tasksQueue.Count == 0) //if the tasks queue is empty
                return;

            taskQueueTimer = tasksList[tasksQueue[0]].GetReloadTime(); //start the timer for the next one.
            tasksList[tasksQueue[0]].Start();

            CustomEvents.OnTaskStarted(this, tasksQueue[0], 0); //trigger custon event
        }

        private void Update()
        {
            //as long as this componeent is active, the faction entity can manage tasks and there are actual pending tasks in the queue
            if (isActive && tasksQueue.Count > 0 && CanManageTask())
                UpdatePendingTask();
        }

        //method called when the task launcher has at least one pending task to update:
        void UpdatePendingTask()
        {
            //if the task timer is still going and we are not using the god mode
            if (taskQueueTimer > 0 && GodMode.Enabled == false)
            {
                taskQueueTimer -= Time.deltaTime;
            }
            else //task timer is done
            {
                OnTaskCompleted(); //complete the task
            }
        }

        //called to complete the first task in the queue
        public void OnTaskCompleted ()
        {
            if (tasksQueue.Count == 0) //if there are no tasks in the queue then do not proceed
                return;

            int completedTaskID = tasksQueue[0]; //get the first pending task and remove it from the queue
            tasksQueue.RemoveAt(0); //remove from task queue

            tasksList[completedTaskID].Complete(); //complete the task

            CustomEvents.OnTaskCompleted(this, completedTaskID, 0); //custom delegate event

            StartNextTask(); //start the next task in queue
        }

        //a method that cancels all in progress tasks when called:
        public void CancelAllInProgressTasks ()
        {
            while(tasksQueue.Count > 0)
                CancelInProgressTask(0);
        }

        //a method that cancels an in progress task
        public void CancelInProgressTask (int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= tasksQueue.Count) //invalid task index
                return;

            int taskID = tasksQueue[queueIndex]; //get the actual task ID
            tasksQueue.RemoveAt(queueIndex); //remove from queue

            tasksList[taskID].Cancel(); //cancel the task

            CustomEvents.OnTaskCanceled(this, taskID, queueIndex);

            if (queueIndex == 0) //if the first task in the queue was the one that got cancelled and we still have more in queue
                StartNextTask();
        }
    }
}
