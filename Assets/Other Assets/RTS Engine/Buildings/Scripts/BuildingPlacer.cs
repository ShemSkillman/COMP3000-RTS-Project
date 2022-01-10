using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;
#if RTSENGINE_FOW
using FoW;
#endif

namespace RTSEngine
{
    [RequireComponent(typeof(Building))]
    public class BuildingPlacer : MonoBehaviour
    {
        private Building building; //the main building component for which this component opeartes
        public bool IsActive { private set; get; }

        //placement-related attributes:
        [SerializeField]
        private bool placeOutsideBorder = false; //Can this building be placed outside the border?
        public bool PlaceOutsideBorder => placeOutsideBorder;

        public bool NewPos { set; get; } //Did the player move the building while placing it? We need to know this so that we can minimize the times certain methods, that check if the 
        //new position of the building is correct or not, are called.
        public bool CanPlace { private set; get; } //Can the player place the building at its current position?

        //Building only near resources:
        [SerializeField]
        private bool placeNearResource = false; //if true then this building will only be placable in range of a resource of the type below.
        [SerializeField]
        public ResourceTypeInfo resourceType; //the resource type info that this building will be placed in range
        [SerializeField]
        public float resourceRange = 4.0f; //the maximum distance between the building and the resource

        //manager component:
        GameManager gameMgr;

        #region Events
        /// <summary>
        /// Event triggered each time the building's placement status is updated.
        /// The placement status can be retrieved from the "CanPlace" property of the BuildingPlacer component.
        /// </summary>
        public static event CustomEvents.BuildingEventHandler BuildingPlacementStatusUpdated = delegate {};
        #endregion

        //a method that initiliazes this component
        public void Init(GameManager gameMgr, Building building)
        {
            this.gameMgr = gameMgr;
            this.building = building; //get the main buiding component.

            //default settings for placing the building.
            building.Placed = false; //mark as not placed.
            CanPlace = false; //building can not be placed by default

            IsActive = true; //mark this component as active

            building.BoundaryCollider.isTrigger = false; //the boundaries collider is not trigger while it's getting placed
        }

        //a method called when the player places the building
        public void PlaceBuilding()
        {
            if (!IsActive) //if this component is not active, do not place building
                return;

            //Building is now placed:
            building.Placed = true;

            if (building.NavObstacle) //enable the nav mesh obstacle comp, if it exists
                building.NavObstacle.enabled = true;

            building.BoundaryCollider.isTrigger = true; //allow the boundaries collider to be trigger now

            building.GetSelection().gameObject.GetComponent<Collider>().enabled = false; //Disable the selection collider so that it won't get auto selected as soon as it's spawned
            building.GetSelection().gameObject.SetActive(true); //and activate the object.

            gameMgr.MinimapIconMgr?.Assign(building.GetSelection()); //ask the minimap icon manager to create the a minimap icon for this building

            building.DisableSelectionPlane(); //hide the building's plane

            if (building.BorderComp) //if the building includes a border comp, then enable it as well
                building.BorderComp.enabled = true;

            building.HealthComp.CurrHealth = 0; //Set the building's health to 0 so that builders can start adding health to it

            building.GetSelection().gameObject.GetComponent<Collider>().enabled = true; //enable the selection's collider so that it becomes selectable

            CustomEvents.OnBuildingPlaced(building); //trigger custom event

            if (building.IsFree() == true) //if this is a free building
                return; //do not proceed

            building.ToggleModel(false);//hide the building's model initially to show the construction objects:

            Assert.IsTrue(placeOutsideBorder || building.BorderComp || building.CurrentCenter != null,
                $"[BuildingPlacer] Building with code: {building.GetCode()} must be placable outside the faction's border or belong to one of its faction's building centers.");

            if (placeOutsideBorder == false && building.BorderComp == null) //if the building is to be placed inside the faction's border and this is not a center building
                building.CurrentCenter.RegisterBuilding(building); //register building in the territory that it belongs to.

            if (building.FactionID == GameManager.PlayerFactionID && GodMode.Enabled == false && building.PlacedByDefault == false) //if the player owns this building and this is a single player game
            {
                List<Unit> selectedUnits = gameMgr.SelectionMgr.Selected.GetEntitiesList(EntityTypes.unit, true, true).Cast<Unit>().ToList();

                if (selectedUnits.Count > 0)
                {
                    //If God Mode is not enabled, make builders move to the building to construct it if building isn't supposed to be placed by default
                    foreach (Unit u in selectedUnits) //go through the selected units and look for builders
                    {
                        if (u.BuilderComp) //check if this unit has a builder comp (can actually build).
                        {
                            if (building.WorkerMgr.currWorkers < building.WorkerMgr.GetAvailableSlots()) //make sure that the maximum amount of builders has not been reached
                            {
                                u.BuilderComp.SetTarget(building); //Make the units construct the building:
                            }
                            else //max amount is reached
                                break; //leave loop
                        }
                    }
                }
            }

            building.HealthComp.CheckState(true); //Check the building's construction state

            enabled = false; //disable this component when the building is placed.
        }

        private void FixedUpdate()
        {
            //For the player faction only, because we check if the object is in range or not for other factions inside the NPC building manager: 
            if (IsActive && building.FactionID == GameManager.PlayerFactionID && building.Placed == false && NewPos == true) //& only if the building hasn't been placed yet and it has been moved to a new pos
            {
                CheckBuildingPos();
            }
        }

        //method called when placing a building to check if it's current position is valid or not:
        public void CheckBuildingPos(float sizeOffset = 0f)
        {
            NewPos = false;

#if RTSENGINE_FOW
            //FoW Only:
            //uncomment this only if you are using the Fog Of War asset and replace MIN_FOG_STRENGTH with the value you need.
            if (FogOfWarRTSManager.FoWTeam != null && building.FactionID == GameManager.PlayerFactionID 
                && FogOfWarRTSManager.FoWTeam.GetFogValue(transform.position) >= FogOfWarRTSManager.MinBuildingPlacementFogStrength)
            {
                building.PlaneRenderer.material.color = Color.red; //Show the player that the building can't be placed here.
                CanPlace = false; //The player can't place the building at this position.
                return;
            }
#endif


            //if the building is not in range of a building center, not on the map or not near the resource that it's supposed to be near
            if (!IsBuildingInCenterRange() || !IsBuildingOnMap() || !IsBuildingNearResource())
            {
                TogglePlacementStatus(false);
            }
            else 
            {
                BoxCollider collider = GetComponent<BoxCollider>();
                bool isCollision = Physics.CheckBox(transform.position + collider.center, collider.size/2 + new Vector3(sizeOffset, sizeOffset, sizeOffset), Quaternion.identity, LayerMask.GetMask("Default"));
                TogglePlacementStatus(!isCollision); //toggle placement status depending on result
            }
        }

        //toggle the placement status for this building
        private void TogglePlacementStatus (bool enable)
        {
            building.PlaneRenderer.material.color = (enable == true) ? Color.green : Color.red; //Show the player that the building can't/can be placed here.
            CanPlace = enable; //The player can't/can place the building at this position.

            BuildingPlacementStatusUpdated(building); //trigger custom events
        }

        //a method that checks if the building is inside the faction's borders
        public bool IsBuildingInCenterRange()
        {
            if (placeOutsideBorder)
                return true;

            float distance = 0.0f;
            bool inRange = false; //true if the building is inside its faction's territory

            if (building.CurrentCenter != null) //if the building is already linked to a building center
            {
                //check if the building is still inside this building center's territory
                distance = Vector3.Distance(building.CurrentCenter.transform.position, this.transform.position);
                if (distance <= building.CurrentCenter.Size) //still inside the center's territory
                    inRange = true; //building is in range
                else
                {
                    inRange = false; //building is not in range
                    building.CurrentCenter = null; //set the current center to null, so we can find another one
                }
            }

            if (building.CurrentCenter == null) //if at this point, the building doesn't have a building center.
            {
                foreach(Building center in building.FactionMgr.GetBuildingCenters())
                {
                    if (center.BorderComp.IsActive == false) //if the border of this center is not active yet
                        continue;

                    distance = Vector3.Distance(center.transform.position, transform.position); //calculate the distance between the current center and this building
                    if (distance <= center.BorderComp.Size && center.BorderComp.AllowBuildingInBorder(building.GetCode())) //if the building is inside this center's territory and it's allowed to have this building around this center
                    {
                        inRange = true; //building center found
                        building.CurrentCenter = center.BorderComp;
                        break; //leave the loop
                    }
                }
            }
            
            if (building.CurrentCenter != null && inRange == true) //if, at this point, the building has a center assigned
            {
                //Sometimes borders collide with each other but the priority of the borders is determined by the order of the creation of the borders
                //That's why we need to check for other factions' borders and make sure the building isn't inside one of them:

                foreach(Border border in gameMgr.BuildingMgr.GetAllBorders()) //loop through all borders
                {
                    if (border.IsActive == false || border.building.FactionMgr.FactionID != building.FactionID) //if the border is not active or it belongs to the building's faction
                        continue; //off to the next one

                    distance = Vector3.Distance(border.transform.position, transform.position); //calculate the distance between the center and this building
                    if (distance <= border.Size && border.Order > building.CurrentCenter.Order) //if the building is inside this center's territory
                        //and if the border has a priority over the one that the building belongs to:
                        inRange = false; //Cancel placing the building here.
                }

            }

            return inRange; //return whether the building is in range a building center or not
        }

        //a method that checks if the building is still on the map
        public bool IsBuildingOnMap()
        {
            Ray ray = new Ray(); //create a new ray
            RaycastHit[] hits; //this will hold the registerd hits by the above ray

            BoxCollider boxCollider = building.BoundaryCollider.GetComponent<BoxCollider>();

            //Start by checking if the middle point of the building's collider is over the map.
            //Set the ray check source point which is the center of the collider in the game world:
            ray.origin = new Vector3(transform.position.x + boxCollider.center.x, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z);

            ray.direction = Vector3.down; //The direction of the ray is always down because we want check if there's terrain right under the building's object:

            int i = 4; //we will check the four corners and the center
            while (i > 0) //as long as the building is still on the map/terrain
            {
                hits = Physics.RaycastAll(ray, gameMgr.PlacementMgr.GetGroundMaxDistnace()); //apply the raycast and store the hits

                bool hitTerrain = false; //did one the hits hit the terrain?
                foreach(RaycastHit rh in hits) //go through all hits
                    if (gameMgr.TerrainMgr.IsTerrainTile(rh.transform.gameObject)) //is this a terrain object?
                        hitTerrain = true;

                if (hitTerrain == false) //if there was no registerd terrain hit
                    return false; //stop and return false

                i--;

                //If we reached this stage, then applying the last raycast, we successfully detected that there was a terrain under it, so we'll move to the next corner:
                switch (i)
                {
                    case 0:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x + boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z + boxCollider.size.z / 2);
                        break;
                    case 1:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x + boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z - boxCollider.size.z / 2);
                        break;
                    case 2:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x - boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z - boxCollider.size.z / 2);
                        break;
                    case 3:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x - boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z + boxCollider.size.z / 2);
                        break;
                }
            }

            return true; //at this stage, we're sure that the center and all corners of the building are on the map, so return true
        }

        //a method that checks if the building is near a specific resource that it's supposed to built in range of
        public bool IsBuildingNearResource()
        {
            //if we can place the building free from being in range of a resource or this is not the player's faction (as this doesn't apply for NPC factions, they are managed differently).
            if (placeNearResource == false || RTSHelper.IsLocalPlayer(building) == false)
                return true;

            foreach (Resource resource in gameMgr.ResourceMgr.GetAllResources()) //go through all resources
                if (resource.GetResourceType() == resourceType) //make sure this is the resource that the building needs (matching names)
                    if (Vector3.Distance(resource.transform.position, transform.position) < resourceRange) //if the building is inside the correct range
                        return true; //found requested resource

            return false; //failure in case no resource that matched the requested settings is found
        }
    }
}

