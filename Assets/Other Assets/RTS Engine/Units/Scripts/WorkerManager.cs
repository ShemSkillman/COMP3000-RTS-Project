using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Worker Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class WorkerManager : MonoBehaviour
    {
        [System.Serializable]
        public class WorkerPosition
        {
            [SerializeField]
            private Transform position = null; //a transform from which we get the position the unit will be collecting resources/constructing a building
            public bool IsValidPosition() { return position != null; }
            public Vector3 GetPosition() { return position.position; }

            public Unit CurrUnit { set; get; } //the Unit that is occupying this working position
        }
        [SerializeField]
        private WorkerPosition[] workerPositions = new WorkerPosition[0]; //all possible working positions to gather resources/construct a building are stored here.
        public int GetAvailableSlots () { return workerPositions.Length; }

        public int currWorkers { private set; get; } //the current amount of workers.

        public void Init()
        {
            currWorkers = 0;
        }

        public Vector3 Add(Unit unit)
        {
            int ID = -1; //will hold the ID of the nearest working position to the unit
            float lastDistance = 0.0f; //holds the distance between the working position and the unit

            //go through all worker positions and look for a free one
            for (int i = 0; i < workerPositions.Length; i++)
            {
                if (workerPositions[i].CurrUnit == null) //if there's no unit occupying this
                {
                    if (workerPositions[i].IsValidPosition() == false) //if there's no worker position then return this ID instantely 
                    {
                        ID = i;
                        break;
                    }
                    else if (ID == -1 || Vector3.Distance(unit.transform.position, workerPositions[i].GetPosition()) < lastDistance) //if this is a closer work position
                    {
                        ID = i; //assign this one
                        lastDistance = Vector3.Distance(unit.transform.position, workerPositions[i].GetPosition()); //register the distance
                    }
                }
            }

            if (ID != -1) //if we found a valid working pos for the unit
            {
                //assign the unit here
                workerPositions[ID].CurrUnit = unit;
                currWorkers++;
                unit.LastWorkerPosID = ID;
                //if we're using legacy mvt then return the object's position as the result, if not return the registered pos
                return (workerPositions[ID].IsValidPosition() == false) ? transform.position : workerPositions[ID].GetPosition();
            }
            else //no valid working pos was found
            {
                //Cancel the unit movement and cancel building & resource gathering
                unit.MovementComp.Stop();
                unit.CancelJob(new Unit.jobType[] { Unit.jobType.collecting, Unit.jobType.building });

                return Vector3.zero;
            }
        }

        //a method to remove one worker
        public void Remove(Unit unit)
        {
            //go through all worker positions and look for the worker
            for (int i = 0; i < workerPositions.Length; i++)
                if (workerPositions[i].CurrUnit == unit) //if this is the unit we're looking for
                {
                    workerPositions[i].CurrUnit = null;
                    currWorkers--;
                }
        }

        //method that gets the first worker from the list
        public Unit Get()
        {
            //go through all worker positions and look for the first worker
            for (int i = 0; i < workerPositions.Length; i++)
                if (workerPositions[i].CurrUnit != null) //if this is a valid unit
                    return workerPositions[i].CurrUnit;

            return null;
        }

        //a method that gets all workers in this worker manager:
        public Unit[] GetAll ()
        {
            List<Unit> unitsList = new List<Unit>();
            foreach (WorkerPosition wp in workerPositions)
                if (wp.CurrUnit != null)
                    unitsList.Add(wp.CurrUnit);

            return unitsList.ToArray();
        }
        
        //get the work position using its ID
        public Vector3 GetWorkerPos(int ID)
        {
            //if there's a valid worker position with the given ID return it back, if not then give the resource's position
            if (ID >= 0 && ID < workerPositions.Length && workerPositions[ID].IsValidPosition() == true)
                return workerPositions[ID].GetPosition();
            else
                return transform.position;
        }
    }
}
