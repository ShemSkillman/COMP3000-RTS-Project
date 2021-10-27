using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using RTSEngine.EntityComponent;

namespace RTSEngine
{
    public class Building : FactionEntity
    {
        public override EntityTypes Type { get { return EntityTypes.building; } }

        //when using task panel categories, this is the category ID where the task button of this building will appear when selecting builder units.
        [SerializeField]
        private int taskPanelCategory = 0;
        public int GetTaskPanelCategory() { return taskPanelCategory; }

        [SerializeField]
        private FactionEntityRequirement[] factionEntityRequirements = new FactionEntityRequirement[0];
        public IEnumerable<FactionEntityRequirement> FactionEntityRequirements => factionEntityRequirements;

        [SerializeField]
        private ResourceInput[] resources = new ResourceInput[0]; //required resources to place this building
        public ResourceInput[] GetResources() { return resources; }

        [SerializeField, Tooltip("How would the building's icon look in the task panel in case requirements are not met to build/place it?")]
        private MissingTaskRequirementData missingReqData = new MissingTaskRequirementData { color = Color.red, icon = null };
        /// <summary>
        /// Gets the color and icon defined to display that the player doesn't have the requirements to start placing this building type.
        /// </summary>
        /// <returns>MissedTaskRequirementsData struct instance where the icon and color are defined.</returns>
        public MissingTaskRequirementData GetMissingReqData () { return missingReqData; }

        public bool FactionCapital { set; get; } //If true, then the building is the capital of this faction.

        [SerializeField]
        private int addPopulation = 0; //Allows to add/remove population slots for the faction that this belongs to.
        public int GetAddedPopulationSlots () { return addPopulation; }
        public void RemovePopulationSlots() { gameMgr.GetFaction(factionID).UpdateMaxPopulation(-addPopulation); }

        //Resource collection bonus: A building can affect the resources that are inside the same border by increasing the amount of collection per second
        [System.Serializable]
        public class BonusResource
        {
            [SerializeField]
            private ResourceTypeInfo resourceType = null; //The resource type info asset file goes here.
            public string GetResourceTypeName () { return resourceType.Key; }
            [SerializeField]
            private float durationReduction = 1.0f; //self-explantory
            public float GetBonus () { return -durationReduction; }
        }
        [SerializeField]
        private BonusResource[] bonusResources = new BonusResource[0];

        public Border CurrentCenter { set; get; } //the building's current center

        //placement settings (these must be attributes of the main building component as they will be required by other external components):
        [SerializeField]
        private bool placedByDefault = false; //Is the building placed by default on the map.
        public bool PlacedByDefault { set { placedByDefault = value; } get { return placedByDefault; } }

        public bool IsBuilt { set; get; } //Is the building built?
        public bool Placed { set; get; } //Has the building been placed on the map?
        public bool PlacementInstance { private set; get; } //is this instance of the building used for placement only?

        //building components:
        [SerializeField]
        private Transform spawnPosition = null; //If the building allows to create unit, they will spawned in this position.
        public Vector3 GetSpawnPosition (LayerMask navMeshLayerMask) {
            return new Vector3(spawnPosition.position.x, gameMgr.TerrainMgr.SampleHeight(spawnPosition.position, radius, navMeshLayerMask), spawnPosition.position.z); //return the building's assigned spawn position
        }
        [SerializeField]
        private Transform gotoPosition = null; //The position that the new unit goes to from the spawn position.
        public Transform GotoPosition { set { gotoPosition = value; } get { return gotoPosition; } }
        public Transform RallyPoint { set; get; } //this can be set to the above GotoPosition transform or a building or a resource transform

        //NPC Related:
        [SerializeField, Tooltip("Data required to manage the creation of this building in a NPC faction.")]
        private NPCBuildingRegulatorDataSingle regulatorData = new NPCBuildingRegulatorDataSingle();
        /// <summary>
        /// Gets a NPCBuildingRegulatorData instance that suits the input requirements.
        /// </summary>
        /// <param name="factionType">FactionTypeInfo instance that defines the faction type of the regulator data.</param>
        /// <param name="npcManagerCode">The NPCManager instance code that defines the NPC Manager type.</param>
        /// <returns>NPCUnitRegulatorData instance if both requirements are met.</returns>
        public NPCBuildingRegulatorData GetRegulatorData (FactionTypeInfo factionType, string npcManagerCode) {
            return regulatorData.Filter(factionType, npcManagerCode); }


        //Building components:
        public BuildingPlacer PlacerComp { private set; get; }
        public BuildingHealth HealthComp { private set; get; }
        public NavMeshObstacle NavObstacle { private set; get; } //this is the navigation obstacle component assigned to the building.
        public Collider BoundaryCollider { private set; get; } //this is the collider that define the building's zone on the map where other buildings are not allowed to be placed.
        public Border BorderComp { private set; get; }
        public BuildingDropOff DropOffComp { private set; get; }
        public WorkerManager WorkerMgr { private set; get; }
        public Portal PortalComp { private set; get; }
        public ResourceGenerator GeneratorComp { private set; get; }

        public BuildingAttack AttackComp { private set; get; }
        public override void UpdateAttackComp(AttackEntity attackEntity) { AttackComp = attackEntity as BuildingAttack; }

        public void Init(GameManager gameMgr, int fID, bool free, Border buildingCenter, bool factionCapital)
        {
            base.Init(gameMgr, fID, free);

            //get the building-specifc components:
            PlacerComp = GetComponent<BuildingPlacer>();
            HealthComp = GetComponent<BuildingHealth>();
            NavObstacle = GetComponent<NavMeshObstacle>();
            BoundaryCollider = GetComponent<Collider>();
            BorderComp = GetComponent<Border>();
            DropOffComp = GetComponent<BuildingDropOff>();
            WorkerMgr = GetComponent<WorkerManager>();
            PortalComp = GetComponent<Portal>();
            GeneratorComp = GetComponent<ResourceGenerator>();

            //initialize them:
            PlacerComp.Init(gameMgr, this);
            if (allAttackComp.Length > 0)
                AttackComp = allAttackComp[0] as BuildingAttack;
            WorkerMgr.Init();

            this.CurrentCenter = buildingCenter; //set building cenetr.
            this.FactionCapital = factionCapital; //is this the new faction capital?

            if (this.FactionCapital)
                gameMgr.GetFaction(FactionID).CapitalBuilding = this;

            if (BoundaryCollider == null) //if the building collider is not set.
                Debug.LogError("[Building]: The building parent object must have a collider to represent the building's boundaries.");
            else
                BoundaryCollider.isTrigger = true; //the building's main collider must always have "isTrigger" is true.

            RallyPoint = gotoPosition; //by default the rally point is set to the goto position
            if (gotoPosition != null) //Hide the goto position
                gotoPosition.gameObject.SetActive(false);

            if (Placed == false) //Disable the player selection collider object if the building has not been placed yet.
                selection.gameObject.SetActive(false);

            if (placedByDefault == false) //if the building is not placed by default.
                PlaneRenderer.material.color = Color.green; //start by setting the selection texture color to green which implies that it's allowed to place building at its position.

            //if this is no plaement instance:
            if (!PlacementInstance)
            {
                PlacerComp.PlaceBuilding(); //place the building
                if (GodMode.Enabled && FactionID == GameManager.PlayerFactionID) //if god mode is enabled and this is the local player's building
                    placedByDefault = true;
            }

            if (placedByDefault) //if the building is supposed to be placed by default or we're in god mode and this is the player's building -> meaning that it is already in the scene
                HealthComp.AddHealthLocal(HealthComp.MaxHealth, null);
        }

        //initialize placement instance:
        public void InitPlacementInstance (GameManager gameMgr, int fID, Border buildingCenter = null)
        {
            PlacementInstance = true; //init as placement instance

            Init(gameMgr, fID, false, null, false); //init the building

            CurrentCenter = buildingCenter; //set the building center

            if(FactionID == GameManager.PlayerFactionID) //if this building belongs to the local player
                plane.SetActive(true); //Enable the building's plane.

            if (NavObstacle) //disable the nav mesh obstacle comp, if it exists
                NavObstacle.enabled = false;
        }

        //a method called when the building is fully constructed
        public void OnBuilt()
        {
            CanRunComponents = true; //the building can now run its entity components

            if (PortalComp) //activate portal component if it exists
                PortalComp.Init(gameMgr, this);

            if (free == true) //if this is a free building then stop here
                return;

            if (BorderComp) //if the building includes the border component
            {
                BorderComp.Init(gameMgr, this); //activate the border
                CurrentCenter = BorderComp; //make the building its own center.
            }
            if (DropOffComp) //if the building has a drop off component
                DropOffComp.Init(this); //initiliaze it
            if (GeneratorComp) //if there's a generator component, init it
                GeneratorComp.Init(gameMgr, this);
            
            gameMgr.GetFaction(factionID).UpdateMaxPopulation(addPopulation); //update the faction population slots.

            gameMgr.ResourceMgr.UpdateResource(factionID, initResources); //add the initialization resources to the building's faction.

            ToggleResourceBonus(true); //apply the resource bonus

            if (gotoPosition != null) //If the building has a goto position
                if (selection.IsSelected) //Check if the building is currently selected
                    gotoPosition.gameObject.SetActive(true); //then show the goto pos
                else
                    gotoPosition.gameObject.SetActive(false); //hide the goto pos
        }

        //a method that updates resource bonuses for all resources inside the center where this building instance is
        public void ToggleResourceBonus(bool enable)
        {
            if (bonusResources.Length == 0 || CurrentCenter == null) //if there are no bonus resources or the building doesn't have a center
                return; //do not continue

            foreach (Resource r in CurrentCenter.GetResourcesInRange()) //go through the resources inside the current center
            {
                for (int i = 0; i < bonusResources.Length; i++) //for each bonus resource type
                {
                    if (r != null && r.GetResourceType().Key == bonusResources[i].GetResourceTypeName()) //if the resource is valid and the bonus resource matches this resource type
                    {
                        //add/remove the bonus depending on the value of "enable"
                        r.UpdateCollectOneUnitDuration(enable ? bonusResources[i].GetBonus() : -bonusResources[i].GetBonus());
                    }
                }
            }
        }

        //a method that updates the rally point of the building
        public void UpdateRallyPoint (Vector3 targetPosition, Entity target)
        {
            if (GotoPosition == null || !IsBuilt || target == this) //no rally point or the building is not built yet or the assigned target is the building itself
                return;

            if(target == null) //no target hit -> hit terrain
            {
                RallyPoint = GotoPosition;
                GotoPosition.position = targetPosition;
                GotoPosition.gameObject.SetActive(true);
            }
            //target hit and..
            else if((target.Type == EntityTypes.building && ((Building)target).FactionID == GameManager.PlayerFactionID) //it's a player owned building
                    || target.Type == EntityTypes.resource) //or it's a resource
            {
                RallyPoint = target.transform;
                GotoPosition.position = target.transform.position;
                GotoPosition.gameObject.SetActive(false);

                gameMgr.SelectionMgr.FlashSelection(target, true);
            }
        }

        //a method called to send a unit to the building's rally point
        public void SendUnitToRallyPoint (Unit unit)
        {
            if (unit == null || RallyPoint == null) //if the input unit is invalid or the rally point is invalid
                return; //do not proceed.

            Building buildingRallyPoint = RallyPoint.gameObject.GetComponent<Building>();
            Resource resourceRallyPoint = RallyPoint.gameObject.GetComponent<Resource>();

            //if the rallypoint is a building that needs construction and the unit has a builder component
            if (buildingRallyPoint && unit.BuilderComp && buildingRallyPoint.WorkerMgr.currWorkers < buildingRallyPoint.WorkerMgr.GetAvailableSlots())
                unit.BuilderComp.SetTarget(buildingRallyPoint); //send unit to construct the building
            
            //if the rallypoint is a resource that can still use collectors and the unit has a collector component
            else if (resourceRallyPoint && unit.CollectorComp && resourceRallyPoint.WorkerMgr.currWorkers < resourceRallyPoint.WorkerMgr.GetAvailableSlots())
                unit.CollectorComp.SetTarget(resourceRallyPoint);
            //if the rallypoint is just a position on the map
            else
                gameMgr.MvtMgr.Move(unit, RallyPoint.position, 0.0f, null, InputMode.movement, false); //move the unit there
        }

        public override void OnMouseClick()
        {
            if (PortalComp != null) //if the building has portal component
                PortalComp.OnMouseClick(); //trigger the click on the portal component
            else
                base.OnMouseClick(); //otherwise, normal mouse click
        }
    }
}
