using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine
{
    /// <summary>
    /// Defines the start position of a search cell using the coordinates of the lower-left corner of the cell.
    /// </summary>
    [System.Serializable]
    public struct SearchCellPosition
    {
        public int x;
        public int y;
    }

    /// <summary>
    /// Stores a collection of entities that are placed inside the cell to allow for a more efficient search process.
    /// </summary>
    public class SearchCell
    {
        //the lower-left corner position of the cell instance.
        private SearchCellPosition position;
        /// <summary>
        /// Gets the lower-left corner position of the search cell.
        /// </summary>
        public SearchCellPosition Position { get { return position; } }

        //set of the neighboring cells, a cell is a neighbor if it shares at least one corner.
        private IEnumerable<SearchCell> neighbors = null;

        /// <summary>
        /// Gets the set of neighboring cells.
        /// </summary>
        public IEnumerable<SearchCell> Neighbors { get { return neighbors; } }

        //the "resources" and "factionEntities" lists hold all of the Entity instances that are placed in the search cell.
        //an entity is inside a search cell if its position (on the x and z axis) is bound by the search cell
        private List<Entity> resources = new List<Entity>();
        /// <summary>
        /// Gets the resources that are positioned within the search cell.
        /// </summary>
        public IEnumerable<Entity> Resources { get { return resources; } }

        private List<FactionEntity> factionEntities = new List<FactionEntity>();
        /// <summary>
        /// Gets the faction entities (units and buildings) that are positioned within the search cell.
        /// </summary>
        public IEnumerable<Entity> FactionEntities { get { return factionEntities; } }

        //holds the coroutine that periodically checks whether moving units inside the search cell are still in the cell or have left it
        private IEnumerator unitPositionCheckCoroutine;
        //a list of units in the cell that are actively moving
        private List<Unit> movingUnits = new List<Unit>();

        //holds all the unit target positions inside the bounds of the search cell.
        private List<UnitTargetPositionMarker> unitTargetPositionMarkers = new List<UnitTargetPositionMarker>();
        /// <summary>
        /// Gets the tracked UnitTargetPositionMarker instances inside the search cell.
        /// </summary>
        public IEnumerable<UnitTargetPositionMarker> UnitTargetPositionMarkers { get { return unitTargetPositionMarkers; } }

        //other components:
        private GridSearchHandler handler;

        /// <summary>
        /// Initializes a new search cell instance by assigning its position coordinates and neighbors to it.
        /// </summary>
        /// <param name="position">Lower-left corner position of the cell.</param>
        /// <param name="neighbors">IEnumerable of SearchCell instances that represent the neighboring cells.</param>
        /// <param name="handler">Current active GridSearchHandler instance.</param>
        public void Init(SearchCellPosition position, IEnumerable<SearchCell> neighbors, GridSearchHandler handler)
        {
            this.position = position;
            this.neighbors = neighbors;
            this.handler = handler;
        }

        /// <summary>
        /// Called whenever a unit that is tracked by the search cell starts moving.
        /// </summary>
        /// <param name="unit">Unit instance that started its movement.</param>
        private void OnUnitStartMoving (Unit unit)
        {
            if (!movingUnits.Contains(unit)) //if the unit is not tracked in the moving units set
                movingUnits.Add(unit); //add it

            if(unitPositionCheckCoroutine == null) //if there's no ongoing unit position check coroutine, start one.
            {
                unitPositionCheckCoroutine = UnitPositionCheck(0.1f);
                handler.StartCoroutine(unitPositionCheckCoroutine);
            }
        }

        /// <summary>
        /// Called whenever a unit that is tracked by the search cell stops moving.
        /// </summary>
        /// <param name="unit">Unit instance that stopped its movement.</param>
        private void OnUnitStopMoving (Unit unit)
        {
            movingUnits.Remove(unit); //remove it from being the unit position track list
        }

        /// <summary>
        /// Checks whether moving units that belong to the search cell have left the cell or not.
        /// </summary>
        /// <param name="waitTime">How often to test whether moving units are the in cell or not?</param>
        private IEnumerator UnitPositionCheck(float waitTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(waitTime);

                int i = 0;
                while (i < movingUnits.Count)
                {
                    if (!IsIn(movingUnits[i].transform.position)) //if one of the moving units has left the cell
                    {
                        Unit nextUnit = movingUnits[i];

                        if(handler.TryGetSearchCell(nextUnit.transform.position, out SearchCell newCell) == ErrorMessage.none) //find a new cell for the unit.
                            newCell.Add(nextUnit);

                        Remove(nextUnit); //remove unit from current search cell

                        continue;
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// Check if a Vector3 position is inside the search cell's boundaries.
        /// </summary>
        /// <param name="testPosition">Vector3 position to test.</param>
        /// <returns>True if the input position is inside the search cell's boundaries, otherwise false.</returns>
        public bool IsIn (Vector3 testPosition)
        {
            return testPosition.x >= position.x && testPosition.x < position.x + handler.CellSize
                && testPosition.z >= position.y && testPosition.z < position.y + handler.CellSize;
        }

        /// <summary>
        /// Adds an Entity instance to be tracked by the search cell.
        /// </summary>
        /// <param name="newEntity">Entity instance to add.</param>
        public void Add(Entity newEntity)
        {
            if (newEntity.Type == EntityTypes.resource) //if this is a resource, add it to its own list
            {
                if (!resources.Contains(newEntity))
                    resources.Add(newEntity);
            }
            else if (!factionEntities.Contains(newEntity)) //if this is a unit or a building
            {
                factionEntities.Add(newEntity as FactionEntity);

                //in case this is a unit:
                if (newEntity.Type == EntityTypes.unit)
                {
                    Unit currUnit = newEntity as Unit;

                    //unsubscribe from movement events:
                    currUnit.MovementComp.UnitMovementStart += OnUnitStartMoving;
                    currUnit.MovementComp.UnitMovementStop += OnUnitStopMoving;

                    if (currUnit.MovementComp.IsMoving) //if the unit is already moving
                        OnUnitStartMoving(currUnit); //track it.
                }
            }
        }

        /// <summary>
        /// Adds a new UnitTargetPositionMarker instance to the tracked lists of unit target position markers inside this search cell.
        /// </summary>
        /// <param name="newMarker">The new UnitTargetPositionMarker instance to add.</param>
        public void Add(UnitTargetPositionMarker newMarker)
        {
            if (!unitTargetPositionMarkers.Contains(newMarker)) //as long as the new marker hasn't been already added
                unitTargetPositionMarkers.Add(newMarker);
        }

        /// <summary>
        /// Removes an Entity instance from being tracked by the search cell.
        /// </summary>
        /// <param name="entity">Entity instance to remove.</param>
        public void Remove(Entity entity)
        {
            if (entity.Type == EntityTypes.resource) //in case of a resource, handle it with its own list.
                resources.Remove(entity);
            else //in case this is a unit or building
            {
                factionEntities.Remove(entity as FactionEntity);

                //if this is a unit
                if (entity.Type == EntityTypes.unit)
                {
                    Unit currUnit = entity as Unit;

                    movingUnits.Remove(currUnit);

                    //unsubscribe from movement events:
                    currUnit.MovementComp.UnitMovementStart -= OnUnitStartMoving; 
                    currUnit.MovementComp.UnitMovementStop -= OnUnitStopMoving;

                    if (unitPositionCheckCoroutine != null && movingUnits.Count == 0) //if there are no more moving units and the check coroutine is runing
                    {
                        //stop coroutine as there are no longer units moving inside this cell.
                        handler.StopCoroutine(unitPositionCheckCoroutine);
                        unitPositionCheckCoroutine = null;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a UnitTargetPositionMarker instance from the tracked list of markers inside this search cell.
        /// </summary>
        /// <param name="marker">The UnitTargetPositionMarker instance to remove.</param>
        public void Remove(UnitTargetPositionMarker marker)
        {
            unitTargetPositionMarkers.Remove(marker);
        }
    }
}
