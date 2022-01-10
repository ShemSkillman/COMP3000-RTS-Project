using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public class BuildingGridSearchHandler : GridSearchHandler
    {
        public List<Vector3> SearchForEmptyCellPositions(Vector3 sourcePosition, float radius, bool randomizePosWithinCell = true)
        {
            List<Vector3> emptyCellPositions = new List<Vector3>();

            ErrorMessage errorMessage;

            //only continue if a valid source search cell is found in the input position.
            if ((errorMessage = TryGetSearchCell(sourcePosition, out SearchCell sourceCell)) != ErrorMessage.none)
                return emptyCellPositions;

            //what cells are we searching next? the source cell and its direct neighbors.
            List<SearchCell> nextCells = new List<SearchCell>(sourceCell.Neighbors) { sourceCell };
            List<SearchCell> searchedCells = new List<SearchCell>(nextCells); //what cells have been already searched or are marked to be searched.

            int coveredSurface = 0; //the size of the covered surface in terms of cell size

            //as long as there cells to search
            while (nextCells.Count > 0)
            {
                float closestDistance = radius * radius;

                List<SearchCell> neighborCells = new List<SearchCell>(); //holds te neighbor cells of the current cells to search so they would be searched in the next round.

                //go through all the cells that are next to search
                foreach (SearchCell cell in nextCells)
                {
                    bool isEmpty = true;

                    //for each cell, search the stored entities (get either resources or faction entities)
                    foreach (Entity entity in cell.FactionEntities)
                    {
                        if (entity == null)
                            continue;

                        isEmpty = false;
                        break;
                    }

                    if (isEmpty)
                    {
                        if (randomizePosWithinCell)
                        {
                            emptyCellPositions.Add(cell.GetRandomPosWithinCell());
                        }
                        else
                        {
                            emptyCellPositions.Add(cell.GetCenterPos());
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

                coveredSurface += cellSize; //increase the search surface

                if (coveredSurface < radius) //as long as the covered search surface has not got beyond the allowed search radius
                {
                    //every search round, we go one cell size (or search cell) further.
                    nextCells = neighborCells; //the next cells to search are now the yet-unsearched neighbor cells
                }
                else //we have already gone through the allowed search radius
                    break;
            }

            return emptyCellPositions;
        }
    }
}
