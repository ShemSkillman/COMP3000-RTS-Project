using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* RTSHelper component created by Oussama Bouanani,  SoumiDelRio
 * This script is part of the RTS Engine */

namespace RTSEngine
{
    public static class RTSHelper
    {
        #region General Helper Methods
        //shuffle an input list in O(n) time
        public static void ShuffleList<T>(List<T> inputList)
        {
            if(inputList.Count > 0) //make sure the list already has elements
            {
                //go through the elements of the list:
                for(int i = 0; i < inputList.Count; i++)
                {
                    int swapID = Random.Range(0, inputList.Count); //pick an element to swap with
                    if(swapID != i) //if this isn't the same element
                    {
                        //swap elements:
                        T tempElement = inputList[swapID];
                        inputList[swapID] = inputList[i];
                        inputList[i] = tempElement;
                    }
                }
            }
        }

        //Swap two items:
        public static void Swap<T>(ref T item1, ref T item2)
        {
            T temp = item1;
            item1 = item2;
            item2 = temp;
        }

        //Create an index list: a list of ints where each element contains its index as the element (usually this is randomized to provide to randomize another list)
        public static List<int> GenerateIndexList (int length)
        {
            List<int> indexList = new List<int>();

            int i = 0;
            while (i < length) 
            {
                indexList.Add(i);
                i++;
            }

            return indexList;
        }

        //Check if a layer is inside a layer mask:
        public static bool IsInLayerMask (LayerMask mask, int layer)
        {
            return ((mask & (1 << layer)) != 0);
        }

        //a method to update the current rotation target
        public static Quaternion GetLookRotation(Transform transform, Vector3 targetPosition, bool reversed = false, bool fixYRotation = true)
        {
            if (reversed)
                targetPosition = transform.position - targetPosition;
            else
                targetPosition -= transform.position;

            if(fixYRotation == true)
                targetPosition.y = 0;
            if (targetPosition != Vector3.zero)
                return Quaternion.LookRotation(targetPosition);
            else
                return transform.rotation;
        }

        /// <summary>
        /// Sets the rotation of a Transform instance to the direction opposite from a Vector3 position.
        /// </summary>
        /// <param name="transform">Transform instance to set rotation for.</param>
        /// <param name="awayFrom">Vector3 position whose opposite direction the transform will look at.</param>
        public static void LookAwayFrom (Transform transform, Vector3 awayFrom)
        {
            transform.LookAt(2 * transform.position - awayFrom);
        }

        //a method that converts time in seconds to a string MM:SS
        public static string TimeToString (float time)
        {
            if (time <= 0.0f)
                return "00:00";

            int seconds = Mathf.RoundToInt (time);
            int minutes = Mathf.FloorToInt (seconds / 60.0f);

            seconds -= minutes * 60;

            string minutesText = (minutes < 10 ? "0" : "") + minutes.ToString();
            string secondsText = (seconds < 10 ? "0" : "") + seconds.ToString();

            return minutesText + ":" + secondsText;
        }
        #endregion

        #region RTS Engine Helper Methods
        /// <summary>
        /// Determines whether a FactionEntity instance belongs to the local player or not.
        /// </summary>
        /// <param name="factionEntity">FactionEntity instance to test.</param>
        /// <returns>True if the faction entity belongs to the local player, otherwise false.</returns>
        public static bool IsLocalPlayer (FactionEntity factionEntity)
        {
            return !GameManager.MultiplayerGame //if this is a singleplayer game -> this is always the local player.
                //in case this is a multiplayer game then make sure this is either the local player's faction or it is a free unit and it's the host player.
                || (factionEntity.FactionID == GameManager.PlayerFactionID
                    || (factionEntity.IsFree() && GameManager.PlayerFactionID == GameManager.HostFactionID));
        }

        /// <summary>
        /// Determines whether a FactionEntity instance is part of the local player's faction.
        /// </summary>
        /// <param name="factionEntity">FactionEntity instance to check.</param>
        /// <returns>True if the FactionEntity instance belongs to the local player's faction, otherwise false.</returns>
        public static bool IsPlayerFaction (FactionEntity factionEntity)
        {
            return factionEntity.FactionID == GameManager.PlayerFactionID;
        }

        /// <summary>
        /// Sorts a set of instances that extend the Entity class into a ChainedSortedList based on the entities code.
        /// </summary>
        /// <typeparam name="T">A type that extends Entity.</typeparam>
        /// <param name="entities">An IEnumerable of instances that extend the Entity class.</typeparam>
        /// <param name="filter">Determines what entities are eligible to be added to the chained sorted list and which are not.</param>
        /// <returns>ChainedSortedList instance of the sorted entities based on their code.</returns>
        public static ChainedSortedList<string, T> SortEntitiesByCode <T> (IEnumerable<T> entities, System.Func<T, bool> filter) where T : Entity
        {
            //this will hold the resulting chained sorted list.
            ChainedSortedList<string, T> sortedEntities = new ChainedSortedList<string, T>();

            //go through the input entities
            foreach(T entity in entities)
                if(filter(entity)) //only if the entity returns true according to the assigned filter
                    sortedEntities.Add(entity.GetCode(), entity); //and add them based on their code

            return sortedEntities;
        }

        /// <summary>
        /// Gets the direction of a list of units in regards to a target position.
        /// </summary>
        /// <param name="units">List of Unit instances.</param>
        /// <param name="targetPosition">Vector3 that represents the position the units will get their direction to.</param>
        /// <returns>Vector3 that represents the direction of the units towards the target position.</returns>
        public static Vector3 GetUnitsDirection (List<Unit> units, Vector3 targetPosition)
        {
            Vector3 direction = Vector3.zero;
            foreach (Unit u in units) //make a sum of each unit's direction towards the target position
                direction += (targetPosition - u.transform.position).normalized;

            direction /= units.Count; //normalize the summed directions by the amount of units.

            return direction;
        }

        //Tests whether a set of faction entities are spawned with a certain amount for a particular faction.
        public static bool TestFactionEntityRequirements (IEnumerable<FactionEntityRequirement> requirements, FactionManager factionManager)
        {
            foreach(FactionEntityRequirement req in requirements)
            {
                int requiredAmount = req.amount;

                foreach (FactionEntity factionEntity in factionManager.GetFactionEntities())
                {
                    if (req.codes.Contains(factionEntity.GetCode(), factionEntity.GetCategory()))
                        requiredAmount--;

                    if (requiredAmount <= 0)
                        break;
                }

                if (requiredAmount > 0)
                    return false;
            }

            return true;
        }

        #endregion
    }
}

