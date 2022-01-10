using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NPCBuildingPlacer script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    /// Holds all required data for a NPC faction to start/continue placing a building
    /// </summary>
    public struct NPCPendingBuilding
    {
        public Building prefab; //prefab of the building that is being placed
        public Building instance; //the actual building that will be placed.
        public Vector3 buildAroundPos; //building will be placed around this position.
        public float buildAroundDistance; //how close does the building is to its center?
        public bool rotate; //can the building rotate to look at the object its rotating around?
    }

    /// <summary>
    /// Responsible for managing the placing the buildings for a NPC faction.
    /// </summary>
    public class NPCBuildingPlacer : NPCComponent
    {
        #region Component Properties

        //placement settings:
        [SerializeField, Tooltip("NPC faction will only start placing buildings after this delay.")]
        private FloatRange placementDelayRange = new FloatRange(7.0f, 20.0f); //actual placement will be only considered after this time.
        private float placementDelayTimer;

        [SerializeField, Tooltip("How fast will the building rotation speed when placing a building?")]
        private float rotationSpeed = 50.0f; //how fast will the building rotate around its build around position

        [SerializeField, Tooltip("Time before the NPC faction decides to try another position to place the building at.")]
        private FloatRange placementMoveReload = new FloatRange(8.0f, 12.0f); //whenever this timer is through, building will be moved away from build around position but keeps rotating
        private float placementMoveTimer;

        [SerializeField, Tooltip("Each time the NPC faction attempts another position to place a building, this value is added to the 'Placement Mvt Reload' field0")]
        private FloatRange placementMoveReloadInc = new FloatRange(1.5f, 2.5f); 
        //this will be added to the move timer each time the building moves.

        [SerializeField, Tooltip("The distance between the new and previous positions of a pending building."), Min(0.0f)]
        private FloatRange moveDistance = new FloatRange(0.5f, 1.5f); //this the distance that the building will move at.

        [SerializeField, Range(0.0f,1.0f), Tooltip("How often is the height of a building sampled from the terrain's height?")]
        private IEnumerator heightCheckCoroutine; //this coroutine is running as long as there's a building to be placed and it allows NPC factions to place buildings on different heights
        #endregion

        #region Initializing/Terminating
        /// <summary>
        /// Called when the object holding this component is disabled/destroyed.
        /// </summary>
        private void OnDisable()
        {
            
        }
        #endregion

        #region Building Placement Management
        /// <summary>
        /// Processes a building placement request and adds a new pending building to be placed.
        /// </summary>
        /// <param name="buildingPrefab">The Building prefab to be cloned and placed.</param>
        /// <param name="buildAround">Defines where the building will be placed around.</param>
        /// <param name="buildAroundRadius">How far should the building from its 'buildAround' position?</param>
        /// <param name="buildAroundDistance">Initial ditsance between the building and its 'buildAround' position.</param>
        /// <param name="rotate">Can the building be rotated while getting placed?</param>
        public bool OnBuildingPlacementRequest(Building buildingPrefab, GameObject buildAround, bool rotate, out Building placedBuilding)
        {
            placedBuilding = null;

            //if the building center or the build around object hasn't been specified:
            if (buildAround == null)
            {
                Debug.LogError("Build Around object for " + buildingPrefab.GetName() + " hasn't been specified in the Building Placement Request!");
                return false;
            }

            if (!gameMgr.ResourceMgr.HasRequiredResources(buildingPrefab.GetResources(), factionMgr.FactionID))
            {
                return false;
            }

            //take resources to place building.
            gameMgr.ResourceMgr.UpdateRequiredResources(buildingPrefab.GetResources(), false, factionMgr.FactionID);            

            //pick the building's spawn pos:
            Vector3 buildAroundPos = buildAround.transform.position;
            //for the sample height method, the last parameter presents the navigation layer mask and 0 stands for the built-in walkable layer where buildings can be placed
            buildAroundPos.y = gameMgr.TerrainMgr.SampleHeight(buildAround.transform.position, 0f, 0) + gameMgr.PlacementMgr.GetBuildingYOffset();
            Vector3 buildingSpawnPos = buildAroundPos;

            //create new instance of building and add it to the pending buildings list:
            NPCPendingBuilding newPendingBuilding = new NPCPendingBuilding
            {
                prefab = buildingPrefab,
                instance = Instantiate(buildingPrefab.gameObject, buildingSpawnPos, buildingPrefab.transform.rotation).GetComponent<Building>(),
                buildAroundPos = buildAroundPos,
                buildAroundDistance = 0f,
                rotate = rotate
            };

            //initialize the building instance for placement:
            newPendingBuilding.instance.InitPlacementInstance(gameMgr, factionMgr.FactionID);

            if (rotate)
            {
                float yRot = Random.Range(0, 360);
                newPendingBuilding.instance.transform.rotation = Quaternion.Euler(0, yRot, 0);
            }

            //we need to hide the building initially, when its turn comes to be placed, appropriate settings will be applied.
            newPendingBuilding.instance.ToggleModel(false); //Hide the building's model:

            if (PositionBuilding(newPendingBuilding))
            {
                placedBuilding = PlaceBuilding(newPendingBuilding);
                return true;
            }
            else
            {
                //Give back resources:
                gameMgr.ResourceMgr.UpdateRequiredResources(newPendingBuilding.instance.GetResources(), true, factionMgr.FactionID);

                //destroy the building instance that was supposed to be placed:
                Destroy(newPendingBuilding.instance.gameObject);
            }

            return false;
        }
        #endregion

        #region Building Placement

        int emptyCellIndex = 0;

        private bool PositionBuilding(NPCPendingBuilding pendingBuilding)
        {
            emptyCellIndex = 0;

            List<Vector3> emptyCellPositions = gameMgr.BuildingMgr.BuildingSearchGrid.SearchForEmptyCellPositions(pendingBuilding.buildAroundPos, 40f);

            if (emptyCellPositions.Count < 1)
            {
                return false;
            }
            else if (emptyCellIndex >= emptyCellPositions.Count)
            {
                emptyCellIndex = 0;
            }

            while(emptyCellIndex < emptyCellPositions.Count)
            {
                pendingBuilding.instance.transform.position = emptyCellPositions[emptyCellIndex];
                //HeightCheck();

                //Check if the building is in a valid position or not:
                pendingBuilding.instance.PlacerComp.CheckBuildingPos(2f);

                //can we place the building:
                if (pendingBuilding.instance.PlacerComp.CanPlace == true)
                {
                    return true;
                }
                else
                {
                    emptyCellIndex++;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the pending building's height to be always over the terrain.
        /// </summary>
        /// <param name="waitTime">How much time to wait before updating the pending building's height again?</param>
        /// <returns></returns>
        private void HeightCheck(NPCPendingBuilding pendingBuilding)
        {
            if (pendingBuilding.instance != null)
            {
                pendingBuilding.instance.transform.position = new Vector3(
                            pendingBuilding.instance.transform.position.x,
                            //thrid argument: navmesh layer, use the built-in walkable layer to sample height.
                            gameMgr.TerrainMgr.SampleHeight(pendingBuilding.instance.transform.position, pendingBuilding.instance.GetRadius(), 1) + gameMgr.PlacementMgr.GetBuildingYOffset(),
                            pendingBuilding.instance.transform.position.z);
            }                    
        }

        /// <summary>
        /// Places the pending building at its position.
        /// </summary>
        private Building PlaceBuilding(NPCPendingBuilding pendingBuilding)
        {
            //destroy the building instance that was supposed to be placed:
            Destroy(pendingBuilding.instance.gameObject);

            //ask the building manager to create a new placed building:
            return gameMgr.BuildingMgr.CreatePlacedInstance(pendingBuilding.prefab,
                pendingBuilding.instance.transform.position,
                pendingBuilding.instance.transform.rotation.eulerAngles.y,
                null, factionMgr.FactionID);
        }
        #endregion
    }
}
