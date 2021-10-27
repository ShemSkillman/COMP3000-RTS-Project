using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/* NPCTaskManager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Types of NPC faction tasks
    /// </summary>
    public enum NPCTaskType {constructBuilding, collectResource};

    /// <summary>
    /// Holds information regarding a NPC task
    /// </summary>
    public struct NPCTask
    {
        public NPCTaskType type; //task type

        //attributes for each npc task type:
        public Entity target;
    }

    /// <summary>
    /// Holds a list of NPCTask instances
    /// </summary>
    public struct NPCTaskQueue
    {
        public List<NPCTask> tasks;
    }

    /// <summary>
    /// Responsible for launching and keeping up with NPC tasks.
    /// </summary>
    public class NPCTaskManager : NPCComponent
    {
        [SerializeField, Min(1), Tooltip("Priority queues define how urgent each NPC task is, this is the amount of priority queues, priority queue i is more urgent than priority queue j if i < j.")]
        private int priorityQueuesAmount = 5; //the amount of different priority queues available.

        private NPCTaskQueue[] queues = new NPCTaskQueue[0]; //a list of NPCTaskQueue instances

        //moving between different queues settings:
        [SerializeField, Tooltip("Time (in seconds) required before tasks are promoted into a higher-priority queue.")]
        private FloatRange promotionTimeRange = new FloatRange(10.0f, 12.0f);
        private float promotionTimer;

        //executing tasks in first queue:
        [SerializeField, Tooltip("Time (in seconds) required before an attempt to execute tasks in highest priority is made.")]
        private FloatRange executeTaskTimeRange = new FloatRange(3.0f, 4.0f);
        private float executeTaskTimer;

        /// <summary>
        /// Total amount of current tasks.
        /// </summary>
        public int Count { private set; get; }

        /// <summary>
        /// Initializes the NPCTaskManager instance, called from the NPCManager instance responsible for this component.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the current game.</param>
        /// <param name="npcMgr">NPCManager instance that manages this NPCComponent instance.</param>
        /// <param name="factionMgr">FactionManager instance of the faction that this component manages.</param>
        public override void Init(GameManager gameMgr, NPCManager npcMgr, FactionManager factionMgr)
        {
            base.Init(gameMgr, npcMgr, factionMgr);

            Assert.IsTrue(priorityQueuesAmount >= 1,
                $"[NPCTaskManager] NPC Faction ID {factionMgr.FactionID} must have at least priority queue");

            Count = 0;
            //create priority queues:
            queues = new NPCTaskQueue[priorityQueuesAmount];
            //initialize each queue
            for(int i = 0; i < priorityQueuesAmount; i++)
                queues[i] = new NPCTaskQueue
                {
                    tasks = new List<NPCTask>()
                };

            promotionTimer = promotionTimeRange.getRandomValue(); //start the promotion timer
        }

        /// <summary>
        /// Runs the NPC task promotion timer.
        /// </summary>
        protected override void OnActiveUpdate()
        {
            base.OnActiveUpdate();

            if (promotionTimer > 0.0f)
                promotionTimer -= Time.deltaTime;
            else
            {
                promotionTimer = promotionTimeRange.getRandomValue(); //start the promotion timer

                //if there's more than one queue:
                if (queues.Length > 1)
                    //go through the queues (excluding the first one):
                    for (int i = 1; i < queues.Length; i++)
                    {
                        //move tasks to +1 higher priority level queues:
                        queues[i - 1].tasks.AddRange(queues[i].tasks);
                        //free tasks in current queue priority level:
                        queues[i].tasks.Clear();
                    }
            }

            if (executeTaskTimer > 0.0f)
                executeTaskTimer -= Time.deltaTime;
            else
            {
                executeTaskTimer = executeTaskTimeRange.getRandomValue(); //start the promotion timer

                int i = 0;
                //if a task is in queue 0 -> this means that task must be executed ASAP:
                //go through the queue 0 tasks:
                while (i < queues[0].tasks.Count)
                {
                    NPCTask nextTask = queues[0].tasks[i];

                    //task type?
                    switch (nextTask.type)
                    {
                        case NPCTaskType.constructBuilding:
                            //construct a building?
                            //making sure that the building is still under construction
                            if (npcMgr.GetNPCComp<NPCBuildingConstructor>().IsBuildingUnderConstruction(nextTask.target as Building))
                            {
                                //request the building constructor and force it to construct building.
                                npcMgr.GetNPCComp<NPCBuildingConstructor>().OnBuildingConstructionRequest(nextTask.target as Building, false, true);
                                i++;
                            }
                            else
                                //remove task:
                                RemoveTask(0, i);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a NPCTask instance to a task queue.
        /// </summary>
        /// <param name="task">NPCTask instance to be added.</param>
        /// <param name="initialPriority">Priority of the initial queue ti add the task to.</param>
        /// <returns></returns>
        public bool AddTask (NPCTask task, int initialPriority)
        {
            initialPriority = Mathf.Clamp(initialPriority, 0, queues.Length - 1);

            queues[initialPriority].tasks.Add(task);

            Activate(); //in case there were no tasks and this component was disabled.
            Count++;

            return true;
        }

        /// <summary>
        /// Removes a task from a queue by providing its index in the queue's list.
        /// </summary>
        /// <param name="queueID">ID of the queue to remove the task from.</param>
        /// <param name="taskID">ID of the task in the queue's list.</param>
        public void RemoveTask (int queueID, int taskID)
        {
            Assert.IsTrue(queueID >= 0 && queueID < queues.Length,
                $"[NPCTaskManager] Invalid queue ID!");

            Assert.IsTrue(taskID >= 0 && taskID < queues[queueID].tasks.Count,
                $"[NPCTaskManager] Invalid task ID!");

            queues[queueID].tasks.RemoveAt(taskID);
            Count--;
        }

        /// <summary>
        /// Removes a task from a queue by providing its NPCTask instance.
        /// </summary>
        /// <param name="queueID">ID of the queue to remove the task from.</param>
        /// <param name="task">NPCTask instance to remove.</param>
        /// <returns>True if the task has been successfully removed, otherwise false.</returns>
        public bool RemoveTask (int queueID, NPCTask task)
        {
            Assert.IsTrue(queueID >= 0 && queueID < queues.Length,
                $"[NPCTaskManager] Invalid queue ID!");

            if(queues[queueID].tasks.Remove(task))
            {
                Count--;
                return true;
            }

            return false;
        }
    }
}
