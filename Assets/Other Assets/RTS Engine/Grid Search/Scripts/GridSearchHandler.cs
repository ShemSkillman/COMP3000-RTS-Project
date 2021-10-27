using System.Collections.Generic;
using UnityEngine;

/* GridSearchHandler script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Generates and handles SearchCell instances to be used in search operations in the map where this components operates.
    /// </summary>
    public class GridSearchHandler : MonoBehaviour
    {
        [SerializeField, Tooltip("Defines the lower-left corner of the search grid where search cells will be generated.")]
        private SearchCellPosition lowerLeftCorner = new SearchCellPosition { x = 0, y = 0 };
        [SerializeField, Tooltip("Defines the upper-right corner of the search grid where search cells will be generated.")]
        private SearchCellPosition upperRightCorner = new SearchCellPosition { x = 100, y = 100 };

        [SerializeField, Tooltip("The size of each individual cell."), Min(1)]
        private int cellSize = 10;
        /// <summary>
        /// Gets the fixed size of each cell in the grid.
        /// </summary>
        public int CellSize { get { return cellSize; } }

        //holds all generated cells according to their positions.
        private Dictionary<SearchCellPosition, SearchCell> gridDict = new Dictionary<SearchCellPosition, SearchCell>();

        /// <summary>
        /// Initializes the GridSearchHandler component. Called by the active GameManager instance in the game.
        /// </summary>
        /// <param name="gameMgr">Active GameManager instance in the game.</param>
        public void Init(GameManager gameMgr)
        {
            //subscribe to following events:
            CustomEvents.UnitCreated += OnEntityCreated;
            CustomEvents.BuildingPlaced += OnEntityCreated;
            CustomEvents.ResourceAdded += OnEntityCreated;

            CustomEvents.BuildingInstanceUpgraded += OnEntityRemoved;
            CustomEvents.UnitInstanceUpgraded += OnEntityRemoved;

            CustomEvents.FactionEntityDead += OnEntityRemoved;
            CustomEvents.ResourceDestroyed += OnEntityRemoved;

            GenerateCells(); //generate the grid search cells
        }

        /// <summary>
        /// Called when the object holding this component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            //unsubscribe from following events:
            CustomEvents.UnitCreated -= OnEntityCreated;
            CustomEvents.BuildingPlaced -= OnEntityCreated;
            CustomEvents.ResourceAdded -= OnEntityCreated;

            CustomEvents.BuildingInstanceUpgraded -= OnEntityRemoved;
            CustomEvents.UnitInstanceUpgraded -= OnEntityRemoved;

            CustomEvents.FactionEntityDead -= OnEntityRemoved;
            CustomEvents.ResourceDestroyed -= OnEntityRemoved;
        }

        /// <summary>
        /// Called whenever an Entity instance is created.
        /// </summary>
        /// <param name="entity">Entity instance that is created and initialized.</param>
        private void OnEntityCreated (Entity entity)
        {
            if (TryGetSearchCell(entity.transform.position, out SearchCell cell) == ErrorMessage.none) //if there's a cell that can accept this entity
                cell.Add(entity); //add it
        }

        /// <summary>
        /// Called whenever an Entity instance is dead or upgraded.
        /// </summary>
        /// <param name="entity">Entity instance that was removed.</param>
        private void OnEntityRemoved (Entity entity)
        {
            if (TryGetSearchCell(entity.transform.position, out SearchCell cell) == ErrorMessage.none) //if there's a cell that can accept this entity
                cell.Remove(entity); //remove it
        }

        /// <summary>
        /// Generates the SearchCell instances according to the lowerLeftCorner, upperRightCorner and cellSize values.
        /// </summary>
        private void GenerateCells ()
        {
            if (cellSize < 0)
            {
                Debug.LogError("[GridSearchHandler] The search grid cell size must be >= 1");
                return;
            }

            gridDict.Clear(); //fresh new cells dictionary (gargabe collector will get rid of the already created instances if there were any).

            //according to the start and end position coordinates, create the required search cells
            for(int x = lowerLeftCorner.x; x < upperRightCorner.x; x += cellSize)
                for (int y = lowerLeftCorner.y; y < upperRightCorner.y; y += cellSize)
                {
                    //each search cell instance is added to the dictionary after it is created for easier direct access using coordinates in the future.
                    SearchCellPosition nextPosition = new SearchCellPosition
                    {
                        x = x,
                        y = y
                    };

                    gridDict.Add(nextPosition, new SearchCell());
                }

            foreach (SearchCellPosition position in gridDict.Keys) //go through all generated cells, init them and assign their neighbors
                gridDict[position].Init(position, FindNeighborCells(position), this);
        }

        /// <summary>
        /// Finds and returns the set of neighbors cells of one source cell. A neighbor cell is one that shares at least one corner with the source cell.
        /// </summary>
        /// <param name="sourcePosition">SearchCellPosition instance of the position of the cell to search neighbors for.</param>
        /// <returns>IEnumerable instance of SearchCell instances that represent the neighbors of the source cell.</returns>
        public IEnumerable<SearchCell> FindNeighborCells (SearchCellPosition sourcePosition)
        {
            List<SearchCell> neighbors = new List<SearchCell>(); //stores the found neighbor cells

            int maxNeighborAmount = 8; //maximum amount of potential neighboring cells
            SearchCellPosition nextPosition = new SearchCellPosition();

            while(maxNeighborAmount > 0)
            {
                switch(maxNeighborAmount)
                {
                    case 1: //right
                        nextPosition = new SearchCellPosition { x = sourcePosition.x + cellSize, y = sourcePosition.y };
                        break;
                    case 2: //left
                        nextPosition = new SearchCellPosition { x = sourcePosition.x - cellSize, y = sourcePosition.y };
                        break;
                    case 3: //up
                        nextPosition = new SearchCellPosition { x = sourcePosition.x, y = sourcePosition.y + cellSize };
                        break;
                    case 4: //down
                        nextPosition = new SearchCellPosition { x = sourcePosition.x, y = sourcePosition.y - cellSize };
                        break;

                    case 5: //upper-right
                        nextPosition = new SearchCellPosition { x = sourcePosition.x + cellSize, y = sourcePosition.y + cellSize };
                        break;
                    case 6: //upper-left
                        nextPosition = new SearchCellPosition { x = sourcePosition.x - cellSize, y = sourcePosition.y + cellSize };
                        break;
                    case 7: //lower-right
                        nextPosition = new SearchCellPosition { x = sourcePosition.x + cellSize, y = sourcePosition.y - cellSize };
                        break;
                    case 8: //lower-left
                        nextPosition = new SearchCellPosition { x = sourcePosition.x - cellSize, y = sourcePosition.y - cellSize };
                        break;
                }

                //see if the potential neightbor cell exists:
                if (gridDict.TryGetValue(nextPosition, out SearchCell neighborCell))
                    neighbors.Add(neighborCell); //if it exists, add it to the results list

                maxNeighborAmount--; //counter
            }

            return neighbors; //return the enumerable component of the resulting neighbor cells list.
        }

        /// <summary>
        /// Finds a SearchCell instance that is responsible for tracking entities in a certain position.
        /// </summary>
        /// <param name="position">Vector3 position.</param>
        /// <param name="cell">SearhCell instance to assign if one is found.</param>
        /// <returns>ErrorMessage.none if a search cell is found for the given position, otherwise the failure's error code.</returns>
        public ErrorMessage TryGetSearchCell (Vector3 position, out SearchCell cell)
        {
            cell = null;

            SearchCellPosition nextPosition = new SearchCellPosition //find the coordinates of the potential search cell where the input position is in
            {
                x = ( ((int)position.x - lowerLeftCorner.x) / cellSize) * cellSize + lowerLeftCorner.x,
                y = ( ((int)position.z - lowerLeftCorner.y) / cellSize) * cellSize + lowerLeftCorner.y
            };

            if(gridDict.TryGetValue(nextPosition, out cell)) //if a valid search cell is found using the computed position, assign the search cell.
                return ErrorMessage.none;

            Debug.LogError($"[GridSearchHandler] No search cell has been defined to contain position: {position}!");
            return ErrorMessage.searchCellNotFound;
        }

        /// <summary>
        /// Search for entities inside a rect defined by a lower left and upper right corner (in terms of world position on the X and Z coordinates) that statisfy a filter.
        /// </summary>
        /// <typeparam name="T">Type of the search results that extends the Entity type.</typeparam>
        /// <param name="lowerLeftCorner">World position on the X and Z coordinates representing the lower left corner of the search rect.</param>
        /// <param name="upperRightCorner">World position on the X and Z coordinates representing the upper right corner of the search rect.</param>
        /// <param name="resources">If true, then only Resources instances will be considered, otherwise only FactionEntity will be considered during the search.</param>
        /// <param name="filter">Function that allows to filter the search result. Only entities that satisfy the filter will be returned in the results list.</param>
        /// <param name="resultList">The search results that satisfy the filter's conditions and are in the search rect will populate this list.</param>
        /// <returns>ErrorMessage.none if the search was completed successfully, otherwise the failure's error code.</returns>
        public ErrorMessage SearchRect<T>(Vector2 lowerLeftCorner, Vector2 upperRightCorner, bool resources, System.Func<T, bool> filter, out List<T> resultList) where T : Entity
        {
            resultList = new List<T>();
            ErrorMessage errorMessage;

            for(float x = lowerLeftCorner.x; x < upperRightCorner.x; x += cellSize)
                for(float y = lowerLeftCorner.y; y < upperRightCorner.y; y += cellSize)
                {
                    if((errorMessage = TryGetSearchCell(new Vector3(x, 0, y), out SearchCell nextCell)) != ErrorMessage.none)
                        return errorMessage;

                    //for each cell, search the stored entities (get either resources or faction entities)
                    foreach (Entity entity in resources ? nextCell.Resources : nextCell.FactionEntities)
                    {
                        if (entity == null)
                            continue;

                        if (entity.transform.position.x >= lowerLeftCorner.x && entity.transform.position.z >= lowerLeftCorner.y
                            && entity.transform.position.x <= upperRightCorner.x && entity.transform.position.z <= upperRightCorner.y
                            && filter(entity as T))
                            resultList.Add(entity as T);
                    }
                }

            return ErrorMessage.none;
        }

        /// <summary>
        /// Searches for the closest potential target that extends type Entity and satisfies a set of conditions.
        /// </summary>
        /// <typeparam name="T">Type of the potential target that extends the Entity type.</typeparam>
        /// <param name="sourcePosition">Vector3 position that represents where the search will start from.</param>
        /// <param name="radius">The radius of the search.</param>
        /// <param name="resources">If true, then only Resources instances will be considered, otherwise only FactionEntity will be considered during the search.</param>
        /// <param name="IsTargetValid">Delegate that takes an instance of the searched type and returns a RTSEngine.ErrorMessage which allows to define the search conditions.</param>
        /// <param name="potentialTarget">Potential search target instnce in case one is found, otherwise null.</param>
        /// <returns>ErrorMessage.none if the search was completed error-free, otherwise failure'S error code.</returns>
        public ErrorMessage Search<T>(Vector3 sourcePosition, float radius, bool resources, System.Func<T, ErrorMessage> IsTargetValid, out T potentialTarget) where T : Entity
        {
            potentialTarget = null;
            ErrorMessage errorMessage;

            //only continue if a valid source search cell is found in the input position.
            if ((errorMessage = TryGetSearchCell(sourcePosition, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            //what cells are we searching next? the source cell and its direct neighbors.
            List<SearchCell> nextCells = new List<SearchCell>(sourceCell.Neighbors) { sourceCell };
            List<SearchCell> searchedCells = new List<SearchCell>(nextCells); //what cells have been already searched or are marked to be searched.

            int coveredSurface = 0; //the size of the covered surface in terms of cell size

            //as long as there cells to search
            while(nextCells.Count > 0)
            {
                float closestDistance = radius*radius;

                List<SearchCell> neighborCells = new List<SearchCell>(); //holds te neighbor cells of the current cells to search so they would be searched in the next round.

                //go through all the cells that are next to search
                foreach(SearchCell cell in nextCells)
                {
                    //for each cell, search the stored entities (get either resources or faction entities)
                    foreach(Entity entity in resources ? cell.Resources : cell.FactionEntities)
                    {
                        if (entity == null)
                            continue;

                        if((entity.transform.position - sourcePosition).sqrMagnitude <= closestDistance //the entity this is a closer potential target
                            && IsTargetValid(entity as T) == ErrorMessage.none) //and it satifies the validity conditions.
                        {
                            //assign new potential target
                            potentialTarget = entity as T;
                            closestDistance = (entity.transform.position - sourcePosition).sqrMagnitude;
                        }
                    }

                    //go through each searched cell's neighbors and see which ones haven't been searched yet or marked for search yet and add them.
                    foreach (SearchCell neighborCell in cell.Neighbors)
                        if (!searchedCells.Contains(neighborCell))
                        {
                            neighborCells.Add(neighborCell);
                            searchedCells.Add(neighborCell);
                        }
                }

                //after going through all the current cells to search
                if (potentialTarget != null) //if we have a potential target
                    return ErrorMessage.none; //search is done, return it.
                else //no potential target is found?
                {
                    coveredSurface += cellSize; //increase the search surface

                    if (coveredSurface < radius) //as long as the covered search surface has not got beyond the allowed search radius
                    {
                        //every search round, we go one cell size (or search cell) further.
                        nextCells = neighborCells; //the next cells to search are now the yet-unsearched neighbor cells
                    }
                    else //we have already gone through the allowed search radius
                        break;
                }
            }

            return ErrorMessage.searchTargetNotFound;
        }

        //TODO: Requires refactoring so that the Search and this method share internal code.
        /// <summary>
        /// Checks whether a given position is reserved by a unit's target position marker or not.
        /// </summary>
        /// <param name="position">Position to test.</param>
        /// <param name="radius">The free radius required around the position in order to claim the position as not reserved.</param>
        /// <param name="layer">Currently 1 for air units and 0 for ground units. To be changed.</param>
        /// <returns>ErrorMessage.none if the position is not reserved by a target position marker, otherwise either failure's error code in case of failure to check or ErrorMessage.positionReserved in case the position is reserved.</returns>
        public ErrorMessage IsPositionReserved (Vector3 position, float radius, int layer)
        {
            ErrorMessage errorMessage;

            //only continue if a valid source search cell is found in the input position.
            if ((errorMessage = TryGetSearchCell(position, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            //what cells are we searching next? the source cell and its direct neighbors.
            List<SearchCell> nextCells = new List<SearchCell>(sourceCell.Neighbors) { sourceCell };
            List<SearchCell> searchedCells = new List<SearchCell>(nextCells); //what cells have been already searched or are marked to be searched.

            int coveredSurface = 0; //the size of the covered surface in terms of cell size

            float sqrRadius = radius * radius; //since we're comparing squarred distances we need the squarred value of the radius

            //as long as there cells to search
            while(nextCells.Count > 0)
            {
                List<SearchCell> neighborCells = new List<SearchCell>(); //holds te neighbor cells of the current cells to search so they would be searched in the next round.

                //go through all the cells that are next to search
                foreach(SearchCell cell in nextCells)
                {
                    //for each cell, go through the tracked target position markers.
                    foreach (UnitTargetPositionMarker marker in cell.UnitTargetPositionMarkers)
                    {
                        if (marker.Enabled
                            && marker.Layer == layer //make sure the marker is active the search layer matches.
                            && (marker.Position - position).sqrMagnitude <= sqrRadius) //if a target position is found inside the range that we're testing
                            return ErrorMessage.positionReserved; //then the position is reserved
                    }

                    //go through each searched cell's neighbors and see which ones haven't been searched yet or marked for search yet and add them.
                    foreach (SearchCell neighborCell in cell.Neighbors)
                        if (!searchedCells.Contains(neighborCell))
                        {
                            neighborCells.Add(neighborCell);
                            searchedCells.Add(neighborCell);
                        }
                }

                //after going through all the current cells to search
                coveredSurface += cellSize; //increase the search surface

                if (coveredSurface < radius) //as long as the covered search surface has not got beyond the allowed search radius
                    //every search round, we go one cell size (or search cell) further.
                    nextCells = neighborCells; //the next cells to search are now the yet-unsearched neighbor cells
                else //we have already gone through the allowed search radius
                    break;
            }

            return ErrorMessage.none; //no target position marker is present in the searched range then the position is not reserved.
        }

#if UNITY_EDITOR
        [Header("Gizmos")]
        public Color gizmoColor = Color.yellow;
        [Min(1.0f)]
        public float gizmoHeight = 1.0f;

        private void OnDrawGizmosSelected()
        {
            if (cellSize <= 0)
                return;

            Gizmos.color = gizmoColor;
            Vector3 size = new Vector3(cellSize, gizmoHeight, cellSize);

            for(int x = lowerLeftCorner.x; x < upperRightCorner.x; x += cellSize)
                for (int y = lowerLeftCorner.y; y < upperRightCorner.y; y += cellSize)
                {
                    Gizmos.DrawWireCube(new Vector3(x + cellSize/2.0f, 0.0f, y + cellSize/2.0f), size);
                }
        }
#endif
    }
}
