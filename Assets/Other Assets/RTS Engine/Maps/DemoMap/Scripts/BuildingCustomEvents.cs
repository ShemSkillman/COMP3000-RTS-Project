using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

/* Building Custom Events created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngineDemo
{
    public class BuildingCustomEvents : MonoBehaviour
    {
        //This component uses the custom delegate events to monitor building related custom events and modify the building's behavior in the demo scene

        [SerializeField]
        private GameManager gameMgr = null;

        //listen to custom events:
        private void OnEnable()
        {
            CustomEvents.BuildingPlaced += OnBuildingPlaced;
            CustomEvents.BuildingHealthUpdated += OnBuildingHealthUpdated;
            CustomEvents.BuildingBuilt += OnBuildingBuilt;
        }

        private void OnDisable()
        {
            CustomEvents.BuildingPlaced -= OnBuildingPlaced;
            CustomEvents.BuildingHealthUpdated -= OnBuildingHealthUpdated;
            CustomEvents.BuildingBuilt -= OnBuildingBuilt;
        }

        //called each time a building is placed:
        private void OnBuildingPlaced (Building building)
        {
            if (building.gameObject.GetComponent<BuildingConstructionEffect>())
                building.gameObject.GetComponent<BuildingConstructionEffect>().Toggle(gameMgr, true); //enable it
        }

        //called each time a building's health is updated
        private void OnBuildingHealthUpdated (Building building, int value, FactionEntity source)
        {
            if (building.IsBuilt) //if the building is built then do not proceed
                return;

            if(building.gameObject.GetComponent<BuildingConstructionEffect>()) //if there's a building construction effect
                building.gameObject.GetComponent<BuildingConstructionEffect>().UpdateTargetHeight(); //update the target height of the construction object
        } 

        //called each time a building is built
        private void OnBuildingBuilt (Building building)
        {
            if (building.gameObject.GetComponent<BuildingConstructionEffect>()) //if there's a building construction effect
                building.gameObject.GetComponent<BuildingConstructionEffect>().Toggle(null, false); //disable the construction effect component
        }
    }
}
