using System.Collections.Generic;
using UnityEngine;

/* NPCBuildingCreator script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    /// <summary>
    // Holds all the information needed regarding an active NPCBuildingRegulator instance.
    /// </summary>
    public class ActiveBuildingRegulator
    {
        public NPCBuildingRegulator instance; //the active instance of the building regulator.
        public float spawnTimer; //spawn timer for the active building regulators.
    }

    /// <summary>
    // Holds all the information needed regarding active NPCBuildingRegulator instances that belong to one building center.
    /// </summary>
    public class BuildingCenterRegulator
    {
        public Building buildingCenter; //the building center where the whole building regulation will happen at.

        public Dictionary<string, ActiveBuildingRegulator> activeBuildingRegulators = new Dictionary<string, ActiveBuildingRegulator>(); //holds the active building regulators
    }

    /// <summary>
    /// Responsible for managing the creation of buildings for a NPC faction.
    /// </summary>
    public class NPCBuildingCreator : NPCComponent
    {
        #region Component Properties
        //The independent building regulators list are not managed by any other NPC component.
        [SerializeField, Tooltip("Buildings that this component is able to create other than the center and population buildings.")]
        private List<Building> independentBuildings = new List<Building>(); //buildings that can be created by this NPC component that do not include the center and population buildings.
        //when this NPC component is initialized, it goes through this list and removes buildings that do not have a NPCBuildingRegulatorData asset that matches...
        //...the NPC faction's type and NPC Manager type.

        private List<Building> currBuildings = new List<Building>(); //a list of buildings that can be used by the NPC faction

        private List<BuildingCenterRegulator> buildingCenterRegulators = new List<BuildingCenterRegulator>(); //a list that holds building centers and their corresponding active building regulators

        //each building count is assocoiated with its total count for the whole NPC faction (for all building centers).
        private Dictionary<string, int> totalBuildingsCount = new Dictionary<string, int>();

        //has the first building center (a building with a Border component) been initialized?
        private bool firstBuildingCenterInitialized = false;
        #endregion

        #region Initiliazing/Terminating
        /// <summary>
        /// Initializes the NPCBuildingCreator instance, called from the NPCManager instance responsible for this component.
        /// </summary>
        /// <param name="gameMgr">GameManager instance of the current game.</param>
        /// <param name="npcMgr">NPCManager instance that manages this NPCComponent instance.</param>
        /// <param name="factionMgr">FactionManager instance of the faction that this component manages.</param>
        public override void Init(GameManager gameMgr, NPCManager npcMgr, FactionManager factionMgr)
        {
            base.Init(gameMgr, npcMgr, factionMgr);

            //start listening to the required delegate events:
            CustomEvents.NPCFactionInit += OnNPCFactionInit;

            CustomEvents.BorderDeactivated += OnBorderDeactivated;
            CustomEvents.BorderActivated += OnBorderActivated;

            CustomEvents.BuildingUpgraded += OnBuildingUpgraded;
        }

        /// <summary>
        /// Called when the object holding this component is disabled/destroyed.
        /// </summary>
        void OnDisable()
        {
            //stop listening to the delegate events:
            CustomEvents.NPCFactionInit -= OnNPCFactionInit;

            CustomEvents.BorderDeactivated -= OnBorderDeactivated;
            CustomEvents.BorderActivated -= OnBorderActivated;

            CustomEvents.BuildingUpgraded -= OnBuildingUpgraded;

            DestroyAllActiveRegulators();
        }

        /// <summary>
        /// Called when a NPC faction is done initializing its components.
        /// </summary>
        /// <param name="factionSlot">FactionSlot of the NPC faction.</param>
        private void OnNPCFactionInit (FactionSlot factionSlot)
        {
            if (factionSlot.FactionMgr != factionMgr) //different NPC faction?
                return;

            currBuildings.Clear();

            //clear the active unit regulator list per default:
            buildingCenterRegulators.Clear();

            firstBuildingCenterInitialized = false;

            //go through the spawned building centers and init them:
            foreach(Building buildingCenter in factionMgr.GetBuildingCenters())
                AddBuildingCenterRegulator(buildingCenter);
        }
        #endregion

        #region Active Regulator Manipulation
        /// <summary>
        /// Creates and activates a BuildingCenterRegulator instance for a newly added building center.
        /// </summary>
        /// <param name="buildingCenter">The Building center instance that to create the BuildingCenterRegulator instance for.</param>
        public void AddBuildingCenterRegulator (Building buildingCenter)
        {
            //create new entry for new building center regulator
            BuildingCenterRegulator newCenterRegulator = new BuildingCenterRegulator
            {
                buildingCenter = buildingCenter,
                activeBuildingRegulators = new Dictionary<string, ActiveBuildingRegulator>()
            };
            //add it to the list:
            buildingCenterRegulators.Add(newCenterRegulator);

            //activate the independent building regulators for this new center regulator if it's the first building center
            //else add all of the buildings that have been used by the NPC faction from the 'currBuildings' list.
            foreach (Building building in !firstBuildingCenterInitialized ? independentBuildings : currBuildings)
                ActivateBuildingRegulator(building, newCenterRegulator);

            //if this is the first border component that has been activated => capital building:
            if (!firstBuildingCenterInitialized)
            {
                npcMgr.GetNPCComp<NPCTerritoryManager>().ActivateCenterRegulator(); //activate the center regulator in the territory manager.
                npcMgr.GetNPCComp<NPCPopulationManager>().ActivatePopulationBuildingRegulators(); //activate the population building

                firstBuildingCenterInitialized = true; //component initiated for the first time
            }
        }

        /// <summary>
        /// Disables and destroys a BuildingCenterRegulator instance that manages a building center.
        /// </summary>
        /// <param name="buildingCenter">The Building center instance whose BuildingCenterRegulator will be removed.</param>
        public void DestroyBuildingCenterRegulator (Building buildingCenter)
        {
            //remove building center regulator from list since it has been destroyed:
            int i = 0;
            while(i < buildingCenterRegulators.Count)
            {
                //if this is the center we're looking for:
                if (buildingCenterRegulators[i].buildingCenter == buildingCenter)
                {
                    //go through all active building regulators in this building center and disable them:
                    foreach (ActiveBuildingRegulator abr in buildingCenterRegulators[i].activeBuildingRegulators.Values)
                        abr.instance.Disable();

                    //remove it
                    buildingCenterRegulators.RemoveAt(i);
                    //done:
                    return;
                }
                else
                    i++; //continue looking
            }
        }

        /// <summary>
        /// Gets the BuildingCenterRegulator instance that manages the NPC faction's capital building
        /// </summary>
        /// <returns></returns>
        public BuildingCenterRegulator GetCapitalBuildingRegualtor()
        {
            //first building center regulator in the list refers to the capital:
            return buildingCenterRegulators[0];
        }

        /// <summary>
        /// Creates and activates one NPCBuildingRegulator instance for a building prefab for each building center.
        /// </summary>
        /// <param name="Building">The Building prefab for which a NPCBuildingRegulator instances will be created.</param>
        public void ActivateBuildingRegulator(Building building)
        {
            //go through the building center regulators:
            foreach (BuildingCenterRegulator bcr in buildingCenterRegulators)
                ActivateBuildingRegulator(building, bcr);
        }

        /// <summary>
        /// Creates and activates a NPCBuildingRegulator instance for a building prefab for one building center.
        /// </summary>
        /// <param name="Building">The Building prefab for which a NPCBuildingRegulator instance will be created.</param>
        /// <param name="centerRegulator">The BuildingCenterRegulator instance for which the NPCBuildingRegulator instance will be created.</param>
        public NPCBuildingRegulator ActivateBuildingRegulator(Building building, BuildingCenterRegulator centerRegulator)
        {
            NPCBuildingRegulatorData data = building.GetRegulatorData(factionMgr.Slot.GetTypeInfo(), npcMgr.NPCType.Key); //get the regulator data

            if (data == null) //invalid regulator data?
                return null; //do not proceed

            //see if the building regulator is already active on the center or not
            NPCBuildingRegulator activeInstance = GetActiveBuildingRegulator(building.GetCode(), centerRegulator);
            if(activeInstance != null)
                return activeInstance; //return the already active instance.

            //we will be activating the building regulator for the input center only
            ActiveBuildingRegulator newBuildingRegulator = new ActiveBuildingRegulator()
            {
                //create new instance
                instance = new NPCBuildingRegulator(data, building, gameMgr, npcMgr, this, centerRegulator.buildingCenter),
                //initial spawning timer: regular spawn reload + start creating after value
                spawnTimer = data.GetCreationDelayTime()
            };

            //add it to the active building regulators list of the current building center.
            centerRegulator.activeBuildingRegulators.Add(building.GetCode(), newBuildingRegulator);

            //if the building is not already in the current list of the buildings that can be used by the NPC faction, add it:
            if(!currBuildings.Contains(building))
                currBuildings.Add(building);

            //whenever a new regulator is added to the active regulators list, then move the building creator into the active state
            Activate();

            //return the new created instance:
            return newBuildingRegulator.instance;
        }

        /// <summary>
        /// Returns an the active NPCBuildingRegulator instance (if it exists) that manages a building type of a given code.
        /// </summary>
        /// <param name="code">The code that identifies the building type.</param>
        /// <param name="centerRegulator">The BuildingCenterRegulator instance which holds pointers to a list of NPCBuildingRegulator instances working inside the correspondant building center.</param>
        /// <returns>ActiveBuildingRegulator instance which includes a pointer to the actual NPCBuildingRegulator instance.</returns>
        public NPCBuildingRegulator GetActiveBuildingRegulator (string code, BuildingCenterRegulator centerRegulator)
        {
            //if the active building regulator instance for the sepififed building code exists, return it
            if (centerRegulator.activeBuildingRegulators.TryGetValue(code, out ActiveBuildingRegulator abr))
                return abr.instance;

            return null; //regulator hasn't been found.
        }

        /// <summary>
        /// Disables and removes all active NPCBuildingRegulator instances.
        /// </summary>
        public void DestroyAllActiveRegulators()
        {
            foreach (BuildingCenterRegulator bcr in buildingCenterRegulators) //go through the active regulators
                foreach (ActiveBuildingRegulator abr in bcr.activeBuildingRegulators.Values)
                    abr.instance.Disable();

            //clear the list, no references to the currently active building regulators -> garbage collector will handle deleting them
            buildingCenterRegulators.Clear();
        }

        /// <summary>
        /// Disables and removes the active NPCBuildingRegulator instance that manages a building whose code matches the given code.
        /// </summary>
        /// <param name="buildingCode">Code of the building that is being managed by the regulator to remove.</param>
        public void DestroyActiveRegulator (string buildingCode)
        {
            //remove the prefab associatedd with the building code from the list of buildings that can be used by the NPC faction.
            Building prefab = GetCapitalBuildingRegualtor().activeBuildingRegulators[buildingCode].instance.Prefab;
            currBuildings.Remove(prefab);

            foreach (BuildingCenterRegulator bcr in buildingCenterRegulators) //go through the active regulators
            {
                //destroy the active instance:
                GetActiveBuildingRegulator(buildingCode, bcr)?.Disable();
                //remove from list and garbage collector will handle deleting it
                bcr.activeBuildingRegulators.Remove(buildingCode);
            }
        }
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Called whenever a Border instance is activated.
        /// </summary>
        /// <param name="border">The Border instance that has been activated.</param>
        private void OnBorderActivated (Border border)
        {
            //if the border's building belongs to this faction.
            if (border.building.FactionID == factionMgr.FactionID)
                AddBuildingCenterRegulator(border.building);
        }

        /// <summary>
        /// Called whenever a Border instance is deactivated.
        /// </summary>
        /// <param name="border">The Border instance that has been deactivated.</param>
        void OnBorderDeactivated(Border border)
        {
            //if the border's building belongs to this faction
            if (border.building.FactionID == factionMgr.FactionID)
                DestroyBuildingCenterRegulator(border.building);
        }

        /// <summary>
        /// Called when a building type is upgraded.
        /// </summary>
        /// <param name="upgrade">Upgrade instance that handles the building upgrade.</param>
        /// <param name="targetID">Index of the upgrade target.</param>
        private void OnBuildingUpgraded(Upgrade upgrade, int targetID)
        {
            if (upgrade.Source.FactionID != factionMgr.FactionID) //only for buildings that belong to this NPC faction
                return;

            DestroyActiveRegulator(upgrade.Source.GetCode()); //remove NPCBuildingRegulator instance for upgrade source building.
            ActivateBuildingRegulator(upgrade.GetTarget(targetID) as Building); //add NPCBuildingRegulator instance for the upgrade target building.
        }
        #endregion

        #region Building Creation
        /// <summary>
        /// Update the building creation timer to monitor creating buildings.
        /// </summary>
        protected override void OnActiveUpdate()
        {
            base.OnActiveUpdate();

            Deactivate(); //assume that the unit creator has finished its job with the current active unit regulators.

            //go through the active building regulators
            for(int i = 0; i < buildingCenterRegulators.Count; i++)
                foreach (ActiveBuildingRegulator abr in buildingCenterRegulators[i].activeBuildingRegulators.Values)
                {
                    if (abr.instance.Data.CanAutoCreate() //make sure the building can be auto created in the first place.
                    //if the building didn't reach its max amount yet and still didn't reach its min amount.
                    //buildings are only automatically created if they haven't reached their min amount
                        && !abr.instance.HasReachedMinAmount() && !abr.instance.HasReachedMaxAmount())
                    {
                        //we are active since the min amount of one of the buildings regulated hasn't been reached
                        Activate();

                        //spawn timer:
                        if (abr.spawnTimer > 0.0f)
                            abr.spawnTimer -= Time.deltaTime;
                        else
                        {
                            //reload timer:
                            abr.spawnTimer = abr.instance.Data.GetSpawnReload();
                            //attempt to create as much as it is possible from this building:
                            OnCreateBuildingRequest(abr.instance, true, buildingCenterRegulators[i].buildingCenter);
                        }
                    }
                }
        }

        /// <summary>
        /// Searches for a NPCBuildingRegulator active instance for which a new building can be placed and constructed.
        /// </summary>
        /// <param name="buildingCode">The code that identifies the building type.</param>
        /// <param name="auto">True if the goal is to create a new instance for the building from the NPCBuildingCreator component.</param>
        /// <returns>An active instance of the building's type NPCBuildingRegulator if found, otherwise null.</returns>
        public NPCBuildingRegulator GetValidBuildingRegulator (string buildingCode, bool auto)
        {
            //go through the building center regulators
            foreach (BuildingCenterRegulator bcr in buildingCenterRegulators)
                //see if this building center has an active regulator instance of the requested type
                if (bcr.activeBuildingRegulators.TryGetValue(buildingCode, out ActiveBuildingRegulator abr))
                    //make sure that we're either automatically going to create the new building or the building can be created when requested by other NPC components
                    if ((auto || abr.instance.Data.CanCreateOnDemand())
                        //and make sure that the regulator instance hasn't reached the max allowed amount
                        && !abr.instance.HasReachedMaxAmount())
                        return abr.instance;

            return null; //no active valid instance found..
        }

        /// <summary>
        /// Launches the next building creation task on the NPCBuildingPlacer component if all requirements are met.
        /// </summary>
        /// <param name="code">The code that identifies the building type to create.</param>
        /// <param name="auto">True if this has been called from the NPCBuildingCreator component, false if called from another NPC component.</param>
        /// <param name="buildingCenter">The Building center instance that will have the next building created inside its territory</param>
        public bool OnCreateBuildingRequest(string buildingCode, bool auto, Building buildingCenter = null)
        {
            return OnCreateBuildingRequest(GetValidBuildingRegulator(buildingCode, auto), auto, buildingCenter);
        }

        /// <summary>
        /// Launches the next building creation task on the NPCBuildingPlacer component if all requirements are met.
        /// </summary>
        /// <param name="instance">The NPCBuildingRegulator instance that will be creating the next building.</param>
        /// <param name="auto">True if this has been called from the NPCBuildingCreator component, false if called from another NPC component.</param>
        /// <param name="buildingCenter">The Building center instance that will have the next building created inside its territory</param>
        public bool OnCreateBuildingRequest(NPCBuildingRegulator instance, bool auto, Building buildingCenter = null)
        {
            //active instance is invalid
            if (instance == null 
                //can't create the building by requests from other NPC components
                || (!auto && !instance.Data.CanCreateOnDemand()) 
                //maximum amount has been already reached
                || instance.HasReachedMaxAmount() )
                return false; //do not proceed

            //check if faction doesn't have enough resources to place the chosen prefab above.
            if (!gameMgr.ResourceMgr.HasRequiredResources(instance.Prefab.GetResources(), factionMgr.FactionID))
            {
                //FUTURE FEATURE -> NO RESOURCES FOUND -> ASK FOR SOME.
                return false;
            }
            else if(!RTSHelper.TestFactionEntityRequirements(instance.Prefab.FactionEntityRequirements, factionMgr))
            {
                //FUTURE FEATURE -> NOT ALL FACTION ENTITIES ARE SPAWNED -> ASK TO CREATE THEM
                return false;
            }

            //if the building center hasn't been chosen and no building center can have the next building built inside its territory
            if(buildingCenter == null && (buildingCenter = factionMgr.GetFreeBuildingCenter(instance.Code)) == null)
            {
                //FUTURE FEATURE -> no building center is found -> request to place a building center?
                return false;
            }

            //all requests have been met but the placement options:
            GameObject buildAround = null; //this is the object that the building will be built around.
            float buildAroundRadius = 0.0f; //this is the radius of the build around object if it exists

            //go through all the building placement option cases:
            switch(instance.Data.GetPlacementOption())
            {
                case NPCPlacementOption.aroundResource:
                    //building will be placed around a resource:
                    //get the list of the resources in the building center where the building will be placed with the requested resource name
                    List<Resource> availableResourceList = ResourceManager.FilterResourceList(buildingCenter.BorderComp.GetResourcesInRange(), 
                        instance.Data.GetPlacementOptionInfo());
                    if (availableResourceList.Count > 0) //if there are resources found:
                    {
                        //pick one randomly:
                        int randomResourceIndex = Random.Range(0, availableResourceList.Count);
                        buildAround = availableResourceList[randomResourceIndex].gameObject;
                        buildAroundRadius = availableResourceList[randomResourceIndex].GetRadius();
                    }
                    break;
                case NPCPlacementOption.aroundBuilding:
                    //building will be placed around another building
                    //get the list of the buildings that match the requested code around the building center
                    List<Building> availableBuildingList = BuildingManager.FilterBuildingList(buildingCenter.BorderComp.GetBuildingsInRange(),
                        instance.Data.GetPlacementOptionInfo());

                    if (availableBuildingList.Count > 0) //if there are buildings found:
                    {
                        //pick one randomly:
                        int randomBuildingIndex = Random.Range(0, availableBuildingList.Count);
                        buildAround = availableBuildingList[Random.Range(0, availableBuildingList.Count)].gameObject;
                        buildAroundRadius = availableBuildingList[randomBuildingIndex].GetRadius();
                    }
                    break;
                default:
                    //no option?
                    buildAround = buildingCenter.gameObject; //build around building center.
                    buildAroundRadius = buildingCenter.GetRadius();
                    break;
            }

            //finally make request to place building:
            npcMgr.GetNPCComp<NPCBuildingPlacer>().OnBuildingPlacementRequest(
                instance.Prefab, buildAround, buildAroundRadius, buildingCenter,
                instance.Data.GetBuildAroundDistance(), instance.Data.CanRotate());

            return true;
        }
        #endregion
    }
}
