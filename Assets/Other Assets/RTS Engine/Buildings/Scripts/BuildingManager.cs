using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Building Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class BuildingManager : MonoBehaviour
    {
        [SerializeField]
        private Building[] freeBuildings = new Building[0]; //buildings that don't belong to any faction must be added here
        public IEnumerable<Building> GetFreeBuildings () { return freeBuildings; }
        [SerializeField]
        private Color freeBuildingColor = Color.black; //color used for free buildings
        public Color GetFreeBuildingColor() { return freeBuildingColor; }

        //Borders:
        public int LastBorderSortingOrder { private set; get; }//In order to draw borders and show which order has been set before the other, their objects have different sorting orders.
        private List<Border> allBorders = new List<Border>(); //All the borders in the game are stored in this game.
        private void AddBorder (Border border) {
            allBorders.Add(border);
            LastBorderSortingOrder--;
        }
        private void RemoveBorder (Border border) { allBorders.Remove(border); }
        public IEnumerable<Border> GetAllBorders () { return allBorders; }

        private GameManager gameMgr;

        public void Init(GameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            CustomEvents.BorderActivated += AddBorder;
            CustomEvents.BorderDeactivated += RemoveBorder;
        }

        //called by the game manager after initializing the faction slots
        public void OnFactionSlotsInitialized ()
        {
            foreach (Building b in freeBuildings) //init all free buildings
                b.Init(gameMgr, -1, true, null, false);
        }

        private void OnDisable()
        {
            CustomEvents.BorderActivated -= AddBorder;
            CustomEvents.BorderDeactivated -= RemoveBorder;
        }

        //creates an instance of a building that is instantly placed:
        public void CreatePlacedInstance(Building buildingPrefab, Vector3 placementPosition, float yEulerAngle, Border buildingCenter, int factionID, bool placedByDefault = false, bool factionCapital = false)
        {
            if (GameManager.MultiplayerGame == false)
            { //if it's a single player game.
                CreatePlacedInstanceLocal(buildingPrefab, placementPosition, yEulerAngle, buildingCenter, factionID, placedByDefault, factionCapital); //place the building
            }
            else
            { //in case it's a multiplayer game:

                //ask the server to spawn the building for all clients:
                NetworkInput newInput = new NetworkInput()
                {
                    sourceMode = (byte)InputMode.create,
                    targetMode = (byte)InputMode.building,
                    value = (placedByDefault && factionCapital) ? 3 : (placedByDefault ? 1 : (factionCapital ? 2 : 0)),
                    initialPosition = placementPosition,
                    targetPosition = new Vector3(0.0f,yEulerAngle, 0.0f)
                };
                InputManager.SendInput(newInput, buildingPrefab, buildingCenter?.building); //send input to input manager
            }
        }

        //creates an instance of a building that is instantly placed:
        public Building CreatePlacedInstanceLocal(Building buildingPrefab, Vector3 placementPosition, float yEulerAngle, Border buildingCenter, int factionID, bool placedByDefault = false, bool factionCapital = false)
        {
            Vector3 buildingEulerAngles = buildingPrefab.transform.rotation.eulerAngles;
            buildingEulerAngles.y = yEulerAngle; //set the rotation of the building

            Building buildingInstance = Instantiate(buildingPrefab.gameObject, placementPosition, Quaternion.Euler(buildingEulerAngles)).GetComponent<Building>(); //create instance

            buildingInstance.PlacedByDefault = placedByDefault;

            buildingInstance.Init(gameMgr, factionID, false, buildingCenter, factionCapital); //init the building

            return buildingInstance;
        }

        //filter a building list depending on a certain code
        public static List<Building> FilterBuildingList(IEnumerable<Building> buildingList, string code)
        {
            //result list:
            List<Building> filteredBuildingList = new List<Building>();
            //go through the input building list:
            foreach (Building b in buildingList)
            {
                if (b.GetCode() == code) //if it has the code we need
                    filteredBuildingList.Add(b); //add it
            }

            return filteredBuildingList;
        }

        //get the closest building of a certain type out of a list to a given position
        public static Building GetClosestBuilding (Vector3 pos, IEnumerable<Building> buildings, List<string> codes = null)
        {
            Building resultBuilding = null;
            float lastDistance = 0;

            //go through the buildings to search
            foreach(Building b in buildings)
            {
                //if the building has a valid code (or there's no code to be checked) and is the closest so far.
                if((codes == null || codes.Contains(b.GetCode())) && (resultBuilding == null || Vector3.Distance(b.transform.position, pos) < lastDistance))
                {
                    resultBuilding = b;
                    lastDistance = Vector3.Distance(b.transform.position, pos);
                }
            }

            return resultBuilding;
        }
    }
}
