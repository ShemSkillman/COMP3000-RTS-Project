using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

/* Building Placement script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class BuildingPlacement : MonoBehaviour
    {
        //buildings that can be placed by the local player
        [SerializeField]
        private List<Building> buildings = new List<Building>();
        public IEnumerable<Building> GetBuildings () { return buildings; }
        public Building GetBuilding (int id) { return buildings[id]; }
        public void AddBuildingRange(IEnumerable<Building> newBuildings) { buildings.AddRange(newBuildings); }

        [SerializeField]
        private float buildingYOffset = 0.01f; //The value added to the building position on the Y axis so that the building object is not stuck inside the terrain.
        public float GetBuildingYOffset () { return buildingYOffset; }

        [SerializeField]
        private float groundMaxDistance = 1.5f; //the maximum distance that a building and the ground can have (this distance must be respected on the center and the four corners of the building).
        public float GetGroundMaxDistnace () { return groundMaxDistance; }

        private Building currentBuilding; //Holds the current building that is getting placed
        public bool IsBuilding () { return currentBuilding != null; } //is the player currently attempting to place a building? 

        [SerializeField]
        private bool holdAndSpawnEnabled = false; //when true, the player will be able to hold a key (the next attribute) to keep placing a building type
        [SerializeField]
        private KeyCode holdAndSpawnKey = KeyCode.LeftShift;
        private int lastBuildingID; //keep track of the last building ID on the all buildings list so we can keep spawning it

        //building rotation:
        [SerializeField]
        private bool canRotate = true; //can the player rotate to be placed buildings?
        //keys used to rotate to be placed buildings 
        [SerializeField]
        private KeyCode positiveRotationKey = KeyCode.R; //increment rotation on the y axis
        [SerializeField]
        private KeyCode negativeRotationKey = KeyCode.R; //increment rotation on the y axis
        [SerializeField]
        private float rotationSpeed = 1f; //how fast is the rotation?

        //audio clips
        [SerializeField, Tooltip("What audio clip to play when the player places a building?")]
        private AudioClipFetcher placeBuildingAudio = new AudioClipFetcher(); //Audio played when a building is placed.

        //Scripts:
        GameManager gameMgr;

        #region Building Placement Events:
        /// <summary>
        /// Delegate used for building placement related events.
        /// </summary>
        /// <param name="border"></param>
        public delegate void BuildingPlacementEventHandler (int factionID, Building building);

        /// <summary>
        /// Event triggered when a building placement request is denied.
        /// </summary>
        public static event BuildingPlacementEventHandler PlacementDenied = delegate { };
        #endregion

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;
        }

        void Update()
        {
            if (!currentBuilding) //if the local player is not placing any building
                return;

            if (Input.GetMouseButtonUp(1)) //if the player preses the right mouse button.
            {
                Cancel(); //stop placing the building
                return;
            }

            MoveBuilding(); //keep moving the building by following the player's mouse

            RotateBuilding(); //rotate the building

            if (currentBuilding.PlacerComp.CanPlace && Input.GetMouseButtonUp(0)) //If the player can place the building at its current position
            {
                if (gameMgr.ResourceMgr.HasRequiredResources(currentBuilding.GetResources(), GameManager.PlayerFactionID) == true) //does the player's team have all the required resources to build this building
                    PlaceBuilding(); //place the building.
                else
                    //Inform the player that he doesn't have enough resources.
                    gameMgr.UIMgr.ShowPlayerMessage("Not enough resources for this building", UIManager.MessageTypes.error);
            }
        }

        //move the building being placed by the player by following their mouse movement
        void MoveBuilding()
        {
            //using a raycheck, we will make the current building follow the mouse position and stay on top of the terrain.
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), 
                out RaycastHit hit, Mathf.Infinity, gameMgr.TerrainMgr.GetGroundTerrainMask()))
            {
                //depending on the height of the terrain, we will place the building on it.
                Vector3 nextBuildingPos = hit.point;
                //make sure that the building position on the y axis stays inside the min and max height interval:
                nextBuildingPos.y += buildingYOffset;

                if (currentBuilding.transform.position != nextBuildingPos) //if the new building position is different than the current one
                    currentBuilding.PlacerComp.NewPos = true; //inform the building's comp that we have moved it so that it checks whether the new position is suitable or not.

                currentBuilding.transform.position = nextBuildingPos; //set the new building's pos.
            }
        }

        //rotate the to be placed building
        private void RotateBuilding ()
        {
            if (!canRotate) //if rotation is disabled
                return; //do not proceed

            Vector3 nextEulerAngles = currentBuilding.transform.rotation.eulerAngles;
            //only rotate if one of the keys is pressed down (check for direction) and rotate on the y axis only.
            nextEulerAngles.y += rotationSpeed * (Input.GetKey(positiveRotationKey) ? 1.0f : (Input.GetKey(negativeRotationKey) ? -1.0f : 0.0f));
            currentBuilding.transform.rotation = Quaternion.Euler(nextEulerAngles);
        }

        //abort placing the building:
        private void Cancel()
        {
            //Call the stop building placement custom event:
            CustomEvents.OnBuildingStopPlacement(currentBuilding);

            //abort the building process
            Destroy(currentBuilding.gameObject);
            currentBuilding = null;
        }

        //the method that allows us to place the building
        private void PlaceBuilding()
        {
            //Remove the resources needed to create the building.
            gameMgr.ResourceMgr.UpdateRequiredResources(currentBuilding.GetResources(), false, GameManager.PlayerFactionID);

            gameMgr.BuildingMgr.CreatePlacedInstance(
                buildings[lastBuildingID], 
                currentBuilding.transform.position, 
                currentBuilding.transform.rotation.eulerAngles.y, 
                currentBuilding.CurrentCenter, 
                GameManager.PlayerFactionID, 
                GodMode.Enabled == true); //create the placed building

            //remove instance that was being used to place building
            Destroy(currentBuilding.gameObject);
            currentBuilding = null;

            //Play building placement audio
            gameMgr.AudioMgr.PlaySFX(placeBuildingAudio.Fetch(), false);

            //if holding and spawning is enabled and the player is holding the right key to do that:
            if (holdAndSpawnEnabled == true && Input.GetKey(holdAndSpawnKey))
                //start placing the same building again
                StartPlacingBuilding(lastBuildingID);
        }

        /// <summary>
        /// Test whether a building can be placed by the player faction or not.
        /// </summary>
        /// <param name="building">Building prefab instance to test.</param>
        /// <param name="showMessage">When true, a UI message will be displayed to the player if the building can not be placed.</param>
        /// <returns></returns>
        public bool CanPlaceBuilding (Building building, bool showMessage)
        {
            //make sure the building hasn't reached its limits:
            if (gameMgr.GetFaction(GameManager.PlayerFactionID).FactionMgr.HasReachedLimit(building.GetCode(), building.GetCategory()))
            {
                if(showMessage)
                    gameMgr.UIMgr.ShowPlayerMessage("Building " + building.GetName() + " has reached its placement limit", UIManager.MessageTypes.error);
                return false;
            }
            //make sure that all required buildings to place this one are here:
            else if(!RTSHelper.TestFactionEntityRequirements(building.FactionEntityRequirements, gameMgr.GetFaction(GameManager.PlayerFactionID).FactionMgr))
            {
                if(showMessage)
                    gameMgr.UIMgr.ShowPlayerMessage("Faction entity requirements for " + building.GetName() + " are missing.", UIManager.MessageTypes.error);
                return false;
            }
            //make sure we have enough resources
            else if (!gameMgr.ResourceMgr.HasRequiredResources(building.GetResources(), GameManager.PlayerFactionID))
            {
                if(showMessage)
                    gameMgr.UIMgr.ShowPlayerMessage("Not enough resources to launch task!", UIManager.MessageTypes.error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempt to start placing a building by specifying its index in the 'buildings' list of this component.
        /// </summary>
        /// <param name="buildingID">The index of the building to start placing.</param>
        public void StartPlacingBuilding(int buildingID)
        {
            if(buildingID < 0 || buildingID >= buildings.Count) //if the building ID is invalid, do not continue
            {
                Debug.LogError("[Building Placement] Input building ID is invalid.");
                return;
            }

            Building building = buildings[buildingID]; //get the building

            if (!CanPlaceBuilding(building, true)) //if the building can not be placed, do not continue and display reason to player with UI message
            {
                PlacementDenied(GameManager.PlayerFactionID, building);
                return;
            }

            //Spawn the building for the player to place on the map:
            currentBuilding = Instantiate(building.gameObject, new Vector3(0, 0, 0), building.transform.rotation).GetComponent<Building>();
            lastBuildingID = buildingID; //set the last building ID

            currentBuilding.InitPlacementInstance(gameMgr, GameManager.PlayerFactionID); //initiliaze the placement instance.

            //Set the position of the new building (and make sure it's on the terrain)
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                Vector3 nextBuildingPos = hit.point;
                nextBuildingPos.y += buildingYOffset;
                currentBuilding.transform.position = nextBuildingPos;
            }

            gameMgr.SelectionMgr.Box.Disable(); //disable the selection box

            //Call the start building placement custom event:
            CustomEvents.OnBuildingStartPlacement(currentBuilding);
        }

        //replace an existing building in the all building list that the faction can spawn with another building (usually after having an age upgrade).
        public void ReplaceBuilding(string code, Building newBuilding)
        {
            if (newBuilding == null || code == newBuilding.GetCode()) //if the new building prefab has the same code to replace or it is invalid, do not proceed
                return;

            //go through all the buildings in the list
            int i = 0;
            while (i < buildings.Count)
            {
                if (buildings[i].GetCode() == code)
                { //when the building is found (code matches)
                    buildings.RemoveAt(i);//remove it
                    if (!buildings.Contains(newBuilding))
                    { //make sure we don't have the same building already in the list
                        buildings.Insert(i, newBuilding); //place the new building in the same position
                    }
                }
                i++;
            }
        }
    }
}
